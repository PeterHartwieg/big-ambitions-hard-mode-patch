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

        public TargetInfo Target { get { return Targets.Hyperreal; } }

        public IList<MethodBase> ResolveHooks(Assembly target)
        {
            var street = target.GetType("PolyModsHyperreal.StreetLights", false);
            lightType = AccessTools.TypeByName("UnityEngine.Rendering.HighDefinition.HDAdditionalLightData");
            var scan = street == null ? null : AccessTools.DeclaredMethod(street, "Scan", Type.EmptyTypes);
            var enable = lightType == null ? null : AccessTools.DeclaredMethod(lightType, "OnEnable", Type.EmptyTypes);
            var destroy = lightType == null ? null : AccessTools.DeclaredMethod(lightType, "OnDestroy", Type.EmptyTypes);
            bool reviewed = PerformanceCompatibility.Reviewed(target, Targets.Hyperreal.VerifiedHashes[0])
                && PerformanceCompatibility.Reviewed(typeof(HelpSystem).Assembly, PerformanceCompatibility.GameHash)
                && PerformanceCompatibility.Reviewed(typeof(UnityEngine.Object).Assembly, PerformanceCompatibility.UnityCoreHash)
                && lightType != null && typeof(Component).IsAssignableFrom(lightType)
                && PerformanceCompatibility.Reviewed(lightType.Assembly, PerformanceCompatibility.HdrpHash);
            if (!reviewed) Log.Warn("Hyperreal light discovery: unreviewed or unreadable game/mod/HDRP binary; original discovery retained.");
            return new MethodBase[] { reviewed && scan != null && scan.ReturnType == typeof(void) ? scan : null,
                enable != null && enable.ReturnType == typeof(void) ? enable : null,
                destroy != null && destroy.ReturnType == typeof(void) ? destroy : null };
        }

        public IList<ContractCheck> Contracts(Assembly target, IList<MethodBase> hooks)
        {
            return new[] {
                new ContractCheck(hooks[0], new Contract("Hyperreal StreetLights.Scan", "47684199604a2bd3")),
                new ContractCheck(hooks[1], new Contract("HDRP light.OnEnable", "0e3c10a2154d333d")),
                new ContractCheck(hooks[2], new Contract("HDRP light.OnDestroy", "0e1d2c8398f885d5")) };
        }

        public void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks)
        {
            harmony.Patch(hooks[0], transpiler: new HarmonyMethod(typeof(HyperrealPatch), "ScanTranspiler"),
                prefix: new HarmonyMethod(typeof(HyperrealPatch), "ScanPrefix"),
                finalizer: new HarmonyMethod(typeof(HyperrealPatch), "ScanFinalizer"));
            harmony.Patch(hooks[1], postfix: new HarmonyMethod(typeof(HyperrealPatch), "LightEnabled"));
            harmony.Patch(hooks[2], prefix: new HarmonyMethod(typeof(HyperrealPatch), "LightDestroyed"));
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
            Rules.HyperrealLightDiscovery.Changed += OptionChanged;
            active = true;
            Log.Info("Hyperreal light discovery: reviewed binaries; scene cache and light lifecycle hooks active. Option=" + Rules.HyperrealLightDiscovery.Enabled + ".");
        }

        public void Revert()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneUnloaded -= SceneUnloaded;
            Rules.HyperrealLightDiscovery.Changed -= OptionChanged;
            if (active) LogStats();
            active = false;
            ResetCache();
            lightType = null;
            searches = hits = registrations = invalidations = scans = 0;
            searchMs = scanMs = worstScanMs = lastScanMs = 0;
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
        static void SceneLoaded(Scene scene, LoadSceneMode mode) { ResetCache(); }
        static void SceneUnloaded(Scene scene) { ResetCache(); }
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
