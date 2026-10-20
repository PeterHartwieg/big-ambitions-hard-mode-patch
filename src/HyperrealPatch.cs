using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HardModePatch.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HardModePatch
{
    public sealed class HyperrealPatch : ITargetPatch
    {
        static readonly DiscoveryCache<Component> lights = new DiscoveryCache<Component>();
        static readonly Predicate<Component> alive = value => value != null;
        static Component[] lastSnapshot;
        static Array typedSnapshot;
        static Type lightType;
        static bool active;
        static int searches, hits, registrations, invalidations, scans;
        static double searchMs, scanMs, worstScanMs, lastScanMs;
        static readonly MapReturn mapReturn = new MapReturn();
        static FieldInfo mapOpen, mapEngaged, mapBlend, indoors, exposure, exposureMode, modeValue, resetHistory;
        static MethodInfo getHdCamera;
        static Camera returnCamera;
        static bool mapReturnFailed;

        public TargetInfo Target { get { return Targets.Hyperreal; } }

        public IList<MethodBase> ResolveHooks(Assembly target)
        {
            var street = target.GetType("PolyModsHyperreal.StreetLights", false);
            lightType = AccessTools.TypeByName("UnityEngine.Rendering.HighDefinition.HDAdditionalLightData");
            var scan = street == null ? null : AccessTools.DeclaredMethod(street, "Scan", Type.EmptyTypes);
            var enable = lightType == null ? null : AccessTools.DeclaredMethod(lightType, "OnEnable", Type.EmptyTypes);
            var destroy = lightType == null ? null : AccessTools.DeclaredMethod(lightType, "OnDestroy", Type.EmptyTypes);
            var volume = target.GetType("PolyModsHyperreal.EnhancementVolume", false);
            var environment = target.GetType("PolyModsHyperreal.GameEnvironment", false);
            var config = target.GetType("PolyModsHyperreal.HRConfig", false);
            var map = target.GetType("PolyModsHyperreal.MapMode", false);
            var hdCamera = AccessTools.TypeByName("UnityEngine.Rendering.HighDefinition.HDCamera");
            var apply = volume == null || environment == null || config == null || map == null ? null
                : AccessTools.DeclaredMethod(volume, "Apply", new[] { typeof(bool), environment, config, map });
            var reset = map == null ? null : AccessTools.DeclaredMethod(map, "Reset", Type.EmptyTypes);
            mapOpen = CheckedField(map, "<Open>k__BackingField", typeof(bool));
            mapEngaged = CheckedField(map, "<Engaged>k__BackingField", typeof(bool));
            mapBlend = CheckedField(map, "<Blend>k__BackingField", typeof(float));
            indoors = CheckedField(environment, "Indoors", typeof(bool));
            exposure = volume == null ? null : AccessTools.DeclaredField(volume, "_exposure");
            exposureMode = exposure == null ? null : AccessTools.Field(exposure.FieldType, "mode");
            modeValue = exposureMode == null ? null : AccessTools.Field(exposureMode.FieldType, "m_Value");
            resetHistory = CheckedField(hdCamera, "resetPostProcessingHistory", typeof(bool));
            getHdCamera = hdCamera == null ? null : AccessTools.DeclaredMethod(hdCamera, "GetOrCreate", new[] { typeof(Camera), typeof(int) });
            bool returnMembers = mapOpen != null && mapEngaged != null && mapBlend != null && indoors != null
                && exposure != null && exposure.FieldType.FullName == "UnityEngine.Rendering.HighDefinition.Exposure"
                && modeValue != null && modeValue.FieldType.FullName == "UnityEngine.Rendering.HighDefinition.ExposureMode"
                && resetHistory != null && getHdCamera != null && getHdCamera.ReturnType == hdCamera;
            bool reviewed = PerformanceCompatibility.Reviewed(target, Targets.Hyperreal.VerifiedHashes[0])
                && PerformanceCompatibility.Reviewed(typeof(HelpSystem).Assembly, PerformanceCompatibility.GameHash)
                && PerformanceCompatibility.Reviewed(typeof(UnityEngine.Object).Assembly, PerformanceCompatibility.UnityCoreHash)
                && lightType != null && typeof(Component).IsAssignableFrom(lightType)
                && PerformanceCompatibility.Reviewed(lightType.Assembly, PerformanceCompatibility.HdrpHash);
            if (!reviewed) Log.Warn("Hyperreal: unreviewed or unreadable game/mod/HDRP binary; original discovery and map return retained.");
            return new MethodBase[] { reviewed && scan != null && scan.ReturnType == typeof(void) ? scan : null,
                enable != null && enable.ReturnType == typeof(void) ? enable : null,
                destroy != null && destroy.ReturnType == typeof(void) ? destroy : null,
                reviewed && returnMembers && apply != null && apply.ReturnType == typeof(void) ? apply : null,
                reset != null && reset.ReturnType == typeof(void) ? reset : null,
                volume == null || hdCamera == null ? null : AccessTools.DeclaredMethod(volume, "Ensure",
                    new[] { typeof(Camera), hdCamera.Assembly.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData", true) }),
                getHdCamera,
                AccessTools.DeclaredMethod(typeof(GlobalEvents), "Init", Type.EmptyTypes) };
        }

        public IList<ContractCheck> Contracts(Assembly target, IList<MethodBase> hooks)
        {
            return new[] {
                new ContractCheck(hooks[0], new Contract("Hyperreal StreetLights.Scan", "47684199604a2bd3")),
                new ContractCheck(hooks[1], new Contract("HDRP light.OnEnable", "0e3c10a2154d333d")),
                new ContractCheck(hooks[2], new Contract("HDRP light.OnDestroy", "0e1d2c8398f885d5")),
                new ContractCheck(hooks[3], new Contract("Hyperreal EnhancementVolume.Apply", "b3c3d17b93a11bc1")),
                new ContractCheck(hooks[4], new Contract("Hyperreal MapMode.Reset", "4ce85b86cc606688")),
                new ContractCheck(hooks[5], new Contract("Hyperreal EnhancementVolume.Ensure", "095bef24db378bf1")),
                new ContractCheck(hooks[6], new Contract("HDRP HDCamera.GetOrCreate", "a13081bd19bedb27")),
                new ContractCheck(hooks[7], new Contract("GlobalEvents.Init", "e8e1603ce5118fed")) };
        }

        public void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks)
        {
            harmony.Patch(hooks[0], transpiler: new HarmonyMethod(typeof(HyperrealPatch), "ScanTranspiler"),
                prefix: new HarmonyMethod(typeof(HyperrealPatch), "ScanPrefix"),
                finalizer: new HarmonyMethod(typeof(HyperrealPatch), "ScanFinalizer"));
            harmony.Patch(hooks[1], postfix: new HarmonyMethod(typeof(HyperrealPatch), "LightEnabled"));
            harmony.Patch(hooks[2], prefix: new HarmonyMethod(typeof(HyperrealPatch), "LightDestroyed"));
            harmony.Patch(hooks[3], postfix: new HarmonyMethod(typeof(HyperrealPatch), "MapReturnPostfix"));
            harmony.Patch(hooks[4], postfix: new HarmonyMethod(typeof(HyperrealPatch), "ResetMapReturn"));
            harmony.Patch(hooks[5], postfix: new HarmonyMethod(typeof(HyperrealPatch), "EnsureCamera"));
            harmony.Patch(hooks[7], postfix: new HarmonyMethod(typeof(HyperrealPatch), "EventsInitialized"));
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
            Rules.HyperrealLightDiscovery.Changed += OptionChanged;
            Rules.HyperrealMapReturnFix.Changed += MapReturnOptionChanged;
            EventsInitialized();
            active = true;
            Log.Info("Hyperreal light discovery: reviewed binaries; scene cache and light lifecycle hooks active. Option=" + Rules.HyperrealLightDiscovery.Enabled + ".");
            Log.Info("Hyperreal map return: exposure-history workaround active. Option=" + Rules.HyperrealMapReturnFix.Enabled + ".");
        }

        public void Revert()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneUnloaded -= SceneUnloaded;
            Rules.HyperrealLightDiscovery.Changed -= OptionChanged;
            Rules.HyperrealMapReturnFix.Changed -= MapReturnOptionChanged;
            GlobalEvents.onCityMapClosed -= CameraReturned;
            ResetMapReturn();
            mapReturnFailed = false;
            if (active) LogStats();
            active = false;
            ResetCache();
            lightType = null;
            searches = hits = registrations = invalidations = scans = 0;
            searchMs = scanMs = worstScanMs = lastScanMs = 0;
        }

        static FieldInfo CheckedField(Type type, string name, Type expected)
        {
            var field = type == null ? null : AccessTools.DeclaredField(type, name);
            return field != null && field.FieldType == expected ? field : null;
        }

        // Runs after the author's Apply has chosen the final exposure mode. IsOpen=false starts the
        // return, but onCityMapClosed follows the camera coroutine; neither alone means the fade ended.
        static void MapReturnPostfix(object __instance, bool on, object env, object map)
        {
            if (mapReturnFailed || map == null) return;
            try
            {
                if (!mapReturn.Advance(Rules.HyperrealMapReturnFix.Enabled, (bool)mapOpen.GetValue(map),
                    (bool)mapEngaged.GetValue(map), (float)mapBlend.GetValue(map), on)) return;
                if ((bool)indoors.GetValue(env)) return;
                var currentExposure = exposure.GetValue(__instance) as UnityEngine.Object;
                if (currentExposure == null || Convert.ToInt32(modeValue.GetValue(exposureMode.GetValue(currentExposure))) != 4) return;
                var camera = returnCamera; // Exactly the camera Hyperreal supplied this frame, not the game's cached Camera.main.
                if (camera == null) return;
                var hd = getHdCamera.Invoke(null, new object[] { camera, 0 });
                resetHistory.SetValue(hd, true);
                Log.Info("Hyperreal map return: reset exposure history after camera return and fade (AutomaticHistogram).");
            }
            catch (Exception ex)
            {
                mapReturnFailed = true;
                ResetMapReturn();
                Log.Warn("Hyperreal map return: workaround disabled for this session: " + ex.Message);
            }
        }

        static void CameraReturned() { mapReturn.CameraReturned(); }
        static void EnsureCamera(Camera cam) { returnCamera = cam; }
        static void ResetMapReturn() { mapReturn.Reset(); returnCamera = null; }
        static void MapReturnOptionChanged(bool enabled) { ResetMapReturn(); }
        static void EventsInitialized()
        {
            ResetMapReturn();
            // The game clears its event delegates on load and on returning to the menu.
            GlobalEvents.onCityMapClosed -= CameraReturned;
            GlobalEvents.onCityMapClosed += CameraReturned;
        }

        static IEnumerable<CodeInstruction> ScanTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var matches = code.Where(i => i.opcode == OpCodes.Call && i.operand is MethodInfo method
                && method.DeclaringType == typeof(UnityEngine.Object) && method.Name == "FindObjectsOfType"
                && method.IsGenericMethod && method.GetGenericArguments()[0] == lightType
                && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(bool)).ToList();
            if (matches.Count != 1) throw new InvalidOperationException("Hyperreal light discovery call changed.");
            matches[0].operand = AccessTools.Method(typeof(HyperrealPatch), "FindLights").MakeGenericMethod(lightType);
            return code;
        }

        static T[] FindLights<T>(bool includeInactive) where T : Component
        {
            if (!Rules.HyperrealLightDiscovery.Enabled || !includeInactive)
                return Search<T>(includeInactive);
            if (!lights.Ready)
            {
                var found = Search<T>(includeInactive);
                lights.Seed(found);
                lastSnapshot = null;
                typedSnapshot = null;
            }
            else hits++;
            var snapshot = lights.Snapshot(alive);
            if (!ReferenceEquals(lastSnapshot, snapshot))
            {
                var typed = new T[snapshot.Length];
                for (int i = 0; i < snapshot.Length; i++) typed[i] = (T)snapshot[i];
                typedSnapshot = typed;
                lastSnapshot = snapshot;
            }
            return (T[])typedSnapshot;
        }

        static T[] Search<T>(bool includeInactive) where T : Component
        {
            long started = Stopwatch.GetTimestamp();
            var found = UnityEngine.Object.FindObjectsOfType<T>(includeInactive);
            searchMs += Milliseconds(started);
            searches++;
            return found;
        }

        static void LightEnabled(Component __instance)
        {
            if (!Rules.HyperrealLightDiscovery.Enabled || !lights.Ready) return;
            // Prefabs/assets are not scene lights. Disabled scene lights remain cached, matching the
            // author's includeInactive:true. A new inactive light joins when it first enables.
            if (__instance != null && __instance.gameObject.scene.IsValid()
                && (__instance.hideFlags & HideFlags.DontSave) == 0 && lights.Track(__instance)) registrations++;
        }

        static void LightDestroyed(Component __instance) { lights.Remove(__instance); }
        static void SceneLoaded(Scene scene, LoadSceneMode mode) { ResetCache(); ResetMapReturn(); }
        static void SceneUnloaded(Scene scene) { ResetCache(); ResetMapReturn(); }
        static void OptionChanged(bool enabled) { ResetCache(); LogStats(); }
        static void ResetCache()
        {
            lights.Reset();
            lastSnapshot = null;
            typedSnapshot = null;
            invalidations++;
        }

        static void ScanPrefix(out long __state) { __state = Stopwatch.GetTimestamp(); }
        static void ScanFinalizer(long __state)
        {
            if (__state == 0) return; // Another Harmony prefix may have skipped our timing prefix.
            double duration = Milliseconds(__state);
            lastScanMs = duration;
            scanMs += duration;
            worstScanMs = Math.Max(worstScanMs, duration);
            scans++;
            if (scans == 1 || scans % 8 == 0) LogStats();
        }
        static double Milliseconds(long started) { return (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency; }
        static string Ms(double value) { return value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture); }
        static void LogStats()
        {
            Log.Info("Hyperreal light discovery: scans=" + scans + ", globalSearches=" + searches + ", cacheHits=" + hits
                + ", cachedLights=" + lights.Count + ", newLights=" + registrations + ", invalidations=" + invalidations
                + ", globalSearchMs=" + Ms(searchMs) + ", scanTotalMs=" + Ms(scanMs) + ", scanMaxMs=" + Ms(worstScanMs)
                + ", lastScanMs=" + Ms(lastScanMs)
                + ", enabled=" + Rules.HyperrealLightDiscovery.Enabled + ".");
        }
    }
}
