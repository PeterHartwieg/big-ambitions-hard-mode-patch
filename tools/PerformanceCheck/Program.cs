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
        var patched = entry.Item1 == "BakeryPatch" ? new[] { hooks[0], hooks[2], hooks[3], hooks[4] } : hooks.ToArray();
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
        }
    }
    finally
    {
        harmony.UnpatchAll(harmony.Id);
        type.GetMethod("Revert").Invoke(instance, null);
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
Console.WriteLine($"ALL {checks} PERFORMANCE CHECKS PASSED (offline; live Help and lighting still require a playtest)");
return 0;

public static class Probes
{
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
