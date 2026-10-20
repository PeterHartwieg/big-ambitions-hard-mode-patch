using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using HarmonyLib;

// Uses the exact local game/target DLLs without starting Unity, touching a save or installing a mod.
// Non-null engine handles below are inert data only: no native method may be called on them.
if (args.Length != 3) { Console.Error.WriteLine("Usage: PerformanceCheck <Managed folder> <HardModePatch.dll> <target DLL folder>"); return 2; }
string managed = Path.GetFullPath(args[0]), patchPath = Path.GetFullPath(args[1]), targets = Path.GetFullPath(args[2]);
AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    if (new[] { "mscorlib", "System", "System.Core", "netstandard" }.Contains(name.Name)) return null;
    foreach (string dir in new[] { managed, targets })
    {
        string path = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(path)) return context.LoadFromAssemblyPath(path);
    }
    return null;
};
var patch = AssemblyLoadContext.Default.LoadFromAssemblyPath(patchPath);
var game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(managed, "BigAmbitions.dll"));
var hdrp = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(managed, "Unity.RenderPipelines.HighDefinition.Runtime.dll"));
var bakery = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(targets, "Bakery.dll"));
var hyperreal = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(targets, "PolyModsHyperreal.dll"));
Type rules = patch.GetType("HardModePatch.Core.Rules", true);
int checks = 0;
void Check(bool ok, string name) { checks++; if (!ok) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); }
object Invoke(Type type, string method, params object[] values) => AccessTools.Method(type, method).Invoke(null, values);
object Rule(string name) => rules.GetField(name).GetValue(null);
void Toggle(string name, bool on) { object r = Rule(name); r.GetType().GetMethod("Set").Invoke(r, new object[] { on }); }
object InertEngineObject(Type type, long handle)
{
    object value = RuntimeHelpers.GetUninitializedObject(type);
    AccessTools.Field(type, "m_CachedPtr").SetValue(value, new IntPtr(handle));
    return value;
}

Type compatibility = patch.GetType("HardModePatch.PerformanceCompatibility", true);
Check((bool)Invoke(compatibility, "Reviewed", game, compatibility.GetField("GameHash").GetRawConstantValue()), "audited game binary accepted");
Check(!(bool)Invoke(compatibility, "Reviewed", game, "0000000000000000"), "changed binary rejected");
Check(!(bool)Invoke(compatibility, "Reviewed", null, "0000000000000000"), "unreadable binary rejected");

foreach (var entry in new[] { ("BakeryPatch", bakery), ("HyperrealPatch", hyperreal) })
{
    Type type = patch.GetType("HardModePatch." + entry.Item1, true);
    object instance = Activator.CreateInstance(type);
    var hooks = (IList<MethodBase>)type.GetMethod("ResolveHooks").Invoke(instance, new object[] { entry.Item2 });
    Check(hooks.All(h => h != null), entry.Item1 + " all hooks resolve");
    var harmony = new Harmony("performancecheck." + entry.Item1);
    try
    {
        type.GetMethod("Apply").Invoke(instance, new object[] { harmony, entry.Item2, hooks });
        var patched = entry.Item1 == "BakeryPatch" ? new[] { hooks[0], hooks[2], hooks[3], hooks[4] }
            : hooks.Take(6).Concat(new[] { hooks[7] }).ToArray();
        Check(patched.All(h => Harmony.GetPatchInfo(h)?.Owners.Contains(harmony.Id) == true), entry.Item1 + " every intended hook actually applied");
        MethodBase target = entry.Item1 == "BakeryPatch" ? hooks[2] : hooks[0];
        string transpiler = entry.Item1 == "BakeryPatch" ? "HelpLookupTranspiler" : "ScanTranspiler";
        var original = PatchProcessor.GetOriginalInstructions(target);
        int nativeSearches = original.Count(i => i.operand is MethodInfo m && m.Name.StartsWith("FindObjectsOfType"));
        var modified = ((IEnumerable<CodeInstruction>)Invoke(type, transpiler, original.Select(i => new CodeInstruction(i)).ToList())).ToList();
        Check(nativeSearches == 1 && modified.Count == original.Count
            && modified.Count(i => i.operand is MethodInfo m && m.DeclaringType == type && (m.Name == "FindHelp" || m.Name == "FindLights")) == 1,
            entry.Item1 + " replaces exactly one search call without adding instructions");
        Check(original.Zip(modified).Count(pair => pair.First.opcode != pair.Second.opcode || !Equals(pair.First.operand, pair.Second.operand)) == 1,
            entry.Item1 + " preserves the author's remaining method body");
        bool rejected = false;
        try { Invoke(type, transpiler, new List<CodeInstruction> { new CodeInstruction(OpCodes.Ret) }); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { rejected = true; }
        Check(rejected, entry.Item1 + " rejects an unexpected discovery body");

        if (entry.Item1 == "BakeryPatch")
        {
            Type helpType = game.GetType("HelpSystem", true);
            var scope = AccessTools.Field(type, "buildingHelp");
            // The target's first type-resolution success logs through Unity. Seed that exact audited
            // result so this fixture tests the real delegate/lookup path without an engine log call.
            Type navigation = bakery.GetType("BakeryHelpNavigationPatch", true);
            AccessTools.Field(navigation, "cachedHelpSystemType").SetValue(null, helpType);
            AccessTools.Field(navigation, "helpSystemTypeResolved").SetValue(null, true);
            int lookupsBefore = (int)AccessTools.Field(type, "cachedLookups").GetValue(null);
            var apply = (Delegate)AccessTools.Field(type, "applyHelp").GetValue(null);
            Check(!(bool)apply.DynamicInvoke(null, false)
                && (int)AccessTools.Field(type, "cachedLookups").GetValue(null) == lookupsBefore + 1,
                "delegate created before patching reaches the cached lookup on a missing Help instance");
            object help = InertEngineObject(helpType, 1);
            scope.SetValue(null, help);
            var found = (Array)Invoke(type, "FindHelp", helpType);
            Check(found.Length == 1 && ReferenceEquals(found.GetValue(0), help), "Help build uses its supplied instance without a global search");
            Check(ReferenceEquals(found, Invoke(type, "FindHelp", helpType)), "repeated Help lookups reuse the result array");
            object[] refresh = { true };
            bool run = (bool)AccessTools.Method(type, "RefreshHelpPrefix").Invoke(null, refresh);
            Check(!run && !(bool)refresh[0], "Help build suppresses Bakery's recursive deferred refresh");
            Toggle("BakeryHelpDiscovery", false);
            Check((bool)Invoke(type, "RefreshHelpPrefix", false), "option off permits original Help refresh");
            Toggle("BakeryHelpDiscovery", true);
            scope.SetValue(null, null);
            Check((bool)Invoke(type, "RefreshHelpPrefix", false), "outside a Help build original refresh remains available");
            Type lifecycle = helpType.BaseType.Assembly.GetType("Extensions.SingletonLifecycle", true);
            FieldInfo applyField = AccessTools.Field(type, "applyHelp");
            var probe = Delegate.CreateDelegate(applyField.FieldType, typeof(Probes).GetMethod("PopulateHelp"));
            Toggle("BakeryHelpDiscovery", false);
            Invoke(lifecycle, "RegisterSingleton", helpType, help);
            applyField.SetValue(null, probe);
            try
            {
                Toggle("BakeryHelpDiscovery", true);
                Check(Probes.Populations == 1 && ReferenceEquals(Probes.Host, help) && !Probes.ForceRefresh,
                    "enabling after Help already initialized invokes one normal population/refresh attempt");
                Toggle("BakeryHelpDiscovery", true);
                Check(Probes.Populations == 1, "unchanged on option does not repeatedly rebuild Help");
                Invoke(type, "BuildHelpPrefix", help);
                Check(Probes.Populations == 2 && scope.GetValue(null) == null,
                    "Help build invokes the author's insertion and releases the refresh-suppression scope");
            }
            finally
            {
                Invoke(lifecycle, "UnregisterSingleton", helpType, help);
                applyField.SetValue(null, apply);
            }
        }
        else
        {
            object cache = AccessTools.Field(type, "lights").GetValue(null);
            Type cacheType = cache.GetType();
            Type component = cacheType.GetGenericArguments()[0];
            Type light = hdrp.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalLightData", true);
            var find = AccessTools.Method(type, "FindLights").MakeGenericMethod(light);
            void Seed(params object[] lamps)
            {
                Array data = Array.CreateInstance(component, lamps.Length);
                for (int i = 0; i < lamps.Length; i++) data.SetValue(lamps[i], i);
                cacheType.GetMethod("Seed").Invoke(cache, new object[] { data });
            }
            Array Find() => (Array)find.Invoke(null, new object[] { true });
            Seed();
            Check(Find().Length == 0 && ReferenceEquals(Find(), Find()), "empty scene light search stays cached with the exact HDRP array type");
            object first = InertEngineObject(light, 2), second = InertEngineObject(light, 3);
            Seed(first);
            var one = Find();
            Check(one.GetType() == light.MakeArrayType() && one.Length == 1 && ReferenceEquals(one.GetValue(0), first), "typed light cache returns original component identities");
            cacheType.GetMethod("Track").Invoke(cache, new[] { second });
            Check(Find().Length == 2, "new cached light reaches Hyperreal's next discovery");
            Invoke(type, "LightDestroyed", first);
            Check(Find().Length == 1 && ReferenceEquals(Find().GetValue(0), second), "destroy callback removes exactly the old light");
            Check((int)AccessTools.Field(type, "searches").GetValue(null) == 0, "cached empty, added and removed light paths make zero global searches");
            Toggle("HyperrealLightDiscovery", false);
            Check(!(bool)cacheType.GetProperty("Ready").GetValue(cache), "option off invalidates light cache");
            Seed(first);
            Toggle("HyperrealLightDiscovery", true);
            Check(!(bool)cacheType.GetProperty("Ready").GetValue(cache), "option on requires a fresh light discovery");
            foreach (string callback in new[] { "SceneLoaded", "SceneUnloaded" })
            {
                Seed(first);
                MethodInfo method = AccessTools.Method(type, callback);
                method.Invoke(null, method.GetParameters().Select(p => Activator.CreateInstance(p.ParameterType)).ToArray());
                Check(!(bool)cacheType.GetProperty("Ready").GetValue(cache), callback + " releases scene cache");
            }

            // Exercise the actual Harmony postfix with the author's engine work stubbed out.
            // Only managed test objects and HDRP's reset flag are used; this cannot test GPU recovery.
            var probeHarmony = new Harmony("performancecheck.map-probes");
            try
            {
                probeHarmony.Patch(hooks[3], prefix: new HarmonyMethod(typeof(Probes), nameof(Probes.SkipOriginal)));
                probeHarmony.Patch(hooks[5], prefix: new HarmonyMethod(typeof(Probes), nameof(Probes.SkipOriginal)));
                probeHarmony.Patch(hooks[6], prefix: new HarmonyMethod(typeof(Probes), nameof(Probes.HdCameraPrefix)));
                Type mapType = hyperreal.GetType("PolyModsHyperreal.MapMode", true);
                Type envType = hyperreal.GetType("PolyModsHyperreal.GameEnvironment", true);
                Type cfgType = hyperreal.GetType("PolyModsHyperreal.HRConfig", true);
                object map = RuntimeHelpers.GetUninitializedObject(mapType);
                object env = RuntimeHelpers.GetUninitializedObject(envType);
                object cfg = RuntimeHelpers.GetUninitializedObject(cfgType);
                object volume = RuntimeHelpers.GetUninitializedObject(hooks[3].DeclaringType);
                Type exposureType = hdrp.GetType("UnityEngine.Rendering.HighDefinition.Exposure", true);
                object exposure = InertEngineObject(exposureType, 11);
                FieldInfo mode = AccessTools.Field(exposureType, "mode");
                object modeParameter = RuntimeHelpers.GetUninitializedObject(mode.FieldType);
                FieldInfo value = AccessTools.Field(mode.FieldType, "m_Value");
                mode.SetValue(exposure, modeParameter);
                AccessTools.Field(volume.GetType(), "_exposure").SetValue(volume, exposure);
                object camera = InertEngineObject(hooks[5].GetParameters()[0].ParameterType, 12);
                Probes.HdCamera = RuntimeHelpers.GetUninitializedObject(hooks[6].DeclaringType);
                var reset = AccessTools.Field(hooks[6].DeclaringType, "resetPostProcessingHistory");
                var colorHistory = AccessTools.Field(hooks[6].DeclaringType, "colorPyramidHistoryIsValid");
                colorHistory.SetValue(Probes.HdCamera, true);
                FieldInfo closed = AccessTools.Field(game.GetType("GlobalEvents", true), "onCityMapClosed");
                void CloseCamera() => ((Action)closed.GetValue(null))();
                void Frame(bool open, bool engaged, float blend, bool on = true)
                {
                    AccessTools.Field(mapType, "<Open>k__BackingField").SetValue(map, open);
                    AccessTools.Field(mapType, "<Engaged>k__BackingField").SetValue(map, engaged);
                    AccessTools.Field(mapType, "<Blend>k__BackingField").SetValue(map, blend);
                    hooks[5].Invoke(volume, new object[] { camera, null });
                    hooks[3].Invoke(volume, new object[] { on, env, cfg, map });
                }
                void Mode(int selected) => value.SetValue(modeParameter, Enum.ToObject(value.FieldType, selected));
                bool WasReset() => (bool)reset.GetValue(Probes.HdCamera);
                void Arm() { reset.SetValue(Probes.HdCamera, false); Frame(true, true, 1f, false); }
                Mode(4);
                hooks[7].Invoke(null, null);
                hooks[7].Invoke(null, null);
                Check(((Delegate)closed.GetValue(null)).GetInvocationList().Count(d => d.Method.DeclaringType == type) == 1,
                    "game event initialization retains exactly one map-close handler across reloads");
                Arm();
                Frame(false, false, 0f);
                Check(!WasReset(), "actual postfix waits for camera completion even after zero blend");
                CloseCamera();
                Frame(false, false, 0.1f);
                Check(!WasReset(), "actual postfix waits for fade after camera completion");
                Frame(false, false, 0f);
                Check(WasReset() && ReferenceEquals(Probes.RequestedCamera, camera) && (bool)colorHistory.GetValue(Probes.HdCamera),
                    "automatic return sets only exposure/postprocessing reset flag on Hyperreal's supplied camera");
                reset.SetValue(Probes.HdCamera, false);
                Frame(false, false, 0f);
                Check(!WasReset(), "actual postfix resets only once per return");
                Mode(0); Arm(); CloseCamera(); Frame(false, false, 0f);
                Check(!WasReset(), "fixed exposure is unchanged");
                Mode(4); Frame(false, false, 0f);
                Check(!WasReset(), "ineligible fixed return does not leave a delayed reset armed");
                Arm(); CloseCamera(); AccessTools.Field(envType, "Indoors").SetValue(env, true); Frame(false, false, 0f);
                Check(!WasReset(), "indoor return is unchanged");
                AccessTools.Field(envType, "Indoors").SetValue(env, false);
                Arm(); CloseCamera(); Toggle("HyperrealMapReturnFix", false); Frame(false, false, 0f);
                Check(!WasReset(), "option off cancels pending exposure reset");
                Toggle("HyperrealMapReturnFix", true); Frame(false, false, 0f);
                Check(!WasReset(), "enabling does not reset unrelated gameplay");
                Arm(); CloseCamera(); hooks[4].Invoke(map, null); Frame(false, false, 0f);
                Check(!WasReset(), "author's Map.Reset cancels pending return");
                Arm(); CloseCamera(); hooks[7].Invoke(null, null); Frame(false, false, 0f);
                Check(!WasReset(), "game event initialization cancels old return state");
                foreach (string callback in new[] { "SceneLoaded", "SceneUnloaded" })
                {
                    Arm(); CloseCamera();
                    var method = AccessTools.Method(type, callback);
                    method.Invoke(null, method.GetParameters().Select(p => Activator.CreateInstance(p.ParameterType)).ToArray());
                    Frame(false, false, 0f);
                    Check(!WasReset(), callback + " cancels pending map return");
                }
                Arm(); CloseCamera(); Frame(false, false, 0f);
                Check(WasReset(), "map return still works after game event initialization");
            }
            finally { probeHarmony.UnpatchAll(probeHarmony.Id); }
        }
    }
    finally
    {
        harmony.UnpatchAll(harmony.Id);
        type.GetMethod("Revert").Invoke(instance, null);
        if (entry.Item1 == "HyperrealPatch")
        {
            var closed = AccessTools.Field(game.GetType("GlobalEvents", true), "onCityMapClosed").GetValue(null) as Delegate;
            Check(closed == null || !closed.GetInvocationList().Any(d => d.Method.DeclaringType == type), "revert removes map-close handler");
        }
    }
}
// The same original Bakery bytes without a readable file identity are deliberately unverified.
// Optional performance hooks must disappear from both lists without disabling the old price hooks.
{
    Type type = patch.GetType("HardModePatch.BakeryPatch", true);
    object instance = Activator.CreateInstance(type);
    var unverified = Assembly.Load(File.ReadAllBytes(bakery.Location));
    var hooks = (IList<MethodBase>)type.GetMethod("ResolveHooks").Invoke(instance, new object[] { unverified });
    var contracts = (IList)type.GetMethod("Contracts").Invoke(instance, new object[] { unverified, hooks });
    Check(hooks.Count == 2 && hooks.All(h => h != null) && contracts.Count == 1,
        "unverified Bakery keeps independent price hooks/contracts and omits optional Help hooks");
}
{
    Type type = patch.GetType("HardModePatch.HyperrealPatch", true);
    var unverified = Assembly.Load(File.ReadAllBytes(hyperreal.Location));
    var hooks = (IList<MethodBase>)type.GetMethod("ResolveHooks").Invoke(Activator.CreateInstance(type), new object[] { unverified });
    Check(hooks[0] == null && hooks[3] == null,
        "unreadable Hyperreal identity rejects discovery and map workaround before installation");
}
Console.WriteLine($"ALL {checks} PERFORMANCE CHECKS PASSED (offline; live Help and lighting still require a playtest)");
return 0;

public static class Probes
{
    public static object HdCamera, RequestedCamera;
    public static bool SkipOriginal() => false;
    public static bool HdCameraPrefix(object camera, ref object __result)
    {
        RequestedCamera = camera;
        __result = HdCamera;
        return false;
    }
    public static int Populations;
    public static object Host;
    public static bool ForceRefresh;
    public static bool PopulateHelp(object host, bool forceRefresh)
    {
        Populations++;
        Host = host;
        ForceRefresh = forceRefresh;
        return true;
    }
}
