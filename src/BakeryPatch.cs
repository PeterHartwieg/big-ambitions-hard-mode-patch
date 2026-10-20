using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BigAmbitions.Items;
using HardModePatch.Core;
using HarmonyLib;
using UnityEngine;

namespace HardModePatch
{
    // Bakery (3810927743) registers five products whose margins are five to ten times vanilla food. Their
    // wholesale and default market prices are rewritten as the items register, and once more for items
    // already registered when this patch arrived. Rival bakeries price from the same items. A business
    // that already set its own selling prices keeps them.
    public sealed class BakeryPatch : ITargetPatch
    {
        static FieldInfo marketPriceField;
        static FieldInfo itemsField;
        static Func<MonoBehaviour, bool, bool> applyHelp;
        static MonoBehaviour buildingHelp;
        static readonly UnityEngine.Object[] oneHelp = new UnityEngine.Object[1];
        static readonly UnityEngine.Object[] noHelp = new UnityEngine.Object[0];
        static bool helpAvailable;
        static int cachedLookups, originalLookups, helpBuilds;
        static double originalSearchMs;
        // Prices as the mod shipped them, per item asset this patch changed, for an unload mid-game.
        static readonly Dictionary<Item, KeyValuePair<float, float>> originals = new Dictionary<Item, KeyValuePair<float, float>>();

        public TargetInfo Target { get { return Targets.Bakery; } }

        public IList<MethodBase> ResolveHooks(Assembly target)
        {
            var bakery = target.GetType("Bakery", false);
            marketPriceField = AccessTools.Field(typeof(Item), "defaultMarketPrice");
            itemsField = AccessTools.Field(typeof(ItemsGetter), "ItemsDictionary");
            MethodBase register = marketPriceField != null && marketPriceField.FieldType == typeof(float) && itemsField != null
                ? AccessTools.Method(typeof(ItemsGetter), "RegisterModItem", new[] { typeof(Item) })
                : null;
            MethodBase bakeryLoad = bakery == null ? null : AccessTools.Method(bakery, "OnLoadAsync");
            var hooks = new List<MethodBase> { register, bakeryLoad };
            helpAvailable = PerformanceCompatibility.Reviewed(target, Targets.Bakery.VerifiedHashes[0])
                && PerformanceCompatibility.Reviewed(typeof(HelpSystem).Assembly, PerformanceCompatibility.GameHash)
                && PerformanceCompatibility.Reviewed(typeof(UnityEngine.Object).Assembly, PerformanceCompatibility.UnityCoreHash)
                && PerformanceCompatibility.Reviewed(typeof(InstanceBehavior<>).Assembly, PerformanceCompatibility.ExtensionsHash);
            // Help is optional even when the existing price patch is compatible with a new mod build.
            // Do not make an unreviewed Help body a new prerequisite for the price rules.
            if (!helpAvailable) return hooks;
            var navigation = target.GetType("BakeryHelpNavigationPatch", false);
            var tryApply = navigation == null ? null : AccessTools.Method(navigation, "TryApply", new[] { typeof(MonoBehaviour), typeof(bool) });
            var refresh = navigation == null ? null : AccessTools.Method(navigation, "RefreshGeneratedNavigation", new[] { typeof(MonoBehaviour), typeof(Type), typeof(MonoBehaviour) });
            var loadCategories = AccessTools.DeclaredMethod(typeof(HelpSystem), "LoadCategories", Type.EmptyTypes);
            helpAvailable = tryApply != null && tryApply.IsStatic && tryApply.ReturnType == typeof(bool)
                && refresh != null && refresh.IsStatic && refresh.ReturnType == typeof(bool)
                && loadCategories != null && loadCategories.ReturnType == typeof(void);
            if (helpAvailable) hooks.AddRange(new MethodBase[] { tryApply, refresh, loadCategories });
            return hooks;
        }

        // OnLoadAsync names the items the price table is keyed by.
        static readonly Contract LoadBody = new Contract("Bakery.OnLoadAsync", "d0fd788dfc9bc258");

        public IList<ContractCheck> Contracts(Assembly target, IList<MethodBase> hooks)
        {
            if (hooks.Count == 2) return new[] { new ContractCheck(hooks[1], LoadBody) };
            return new[] { new ContractCheck(hooks[1], LoadBody),
                new ContractCheck(hooks[2], new Contract("Bakery Help.TryApply", "48ba0fbafa6f38d5")),
                new ContractCheck(hooks[3], new Contract("Bakery Help.RefreshGeneratedNavigation", "bc9f0fc6f202fb86")),
                new ContractCheck(hooks[4], new Contract("HelpSystem.LoadCategories", "1f6420967bb3b53a")) };
        }

        public void Revert()
        {
            if (helpAvailable) LogHelp();
            Rules.BakeryHelpDiscovery.Changed -= HelpOptionChanged;
            helpAvailable = false;
            applyHelp = null;
            buildingHelp = null;
            oneHelp[0] = null;
            cachedLookups = originalLookups = helpBuilds = 0;
            originalSearchMs = 0;
            foreach (var pair in originals)
            {
                if (pair.Key == null) continue;
                pair.Key.wholesalePrice = pair.Value.Key;
                marketPriceField.SetValue(pair.Key, pair.Value.Value);
            }
            if (originals.Count > 0) Log.Info("Bakery: restored the published prices of " + originals.Count + " items.");
            originals.Clear();
        }

        public void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks)
        {
            harmony.Patch(hooks[0], prefix: new HarmonyMethod(typeof(BakeryPatch), "RegisterPrefix"));
            helpAvailable = hooks.Count == 5;
            if (helpAvailable)
            {
                try
                {
                    applyHelp = (Func<MonoBehaviour, bool, bool>)Delegate.CreateDelegate(typeof(Func<MonoBehaviour, bool, bool>), (MethodInfo)hooks[2]);
                    harmony.Patch(hooks[2], transpiler: new HarmonyMethod(typeof(BakeryPatch), "HelpLookupTranspiler"));
                    harmony.Patch(hooks[3], prefix: new HarmonyMethod(typeof(BakeryPatch), "RefreshHelpPrefix"));
                    harmony.Patch(hooks[4], prefix: new HarmonyMethod(typeof(BakeryPatch), "BuildHelpPrefix"));
                    Rules.BakeryHelpDiscovery.Changed += HelpOptionChanged;
                    Log.Info("Bakery Help discovery: reviewed binaries; singleton lookup and Help-build hooks active. Option=" + Rules.BakeryHelpDiscovery.Enabled + ".");
                    HelpOptionChanged(Rules.BakeryHelpDiscovery.Enabled);
                }
                catch (Exception ex)
                {
                    // Another transpiler may have changed the reviewed body in memory. Roll back just
                    // the optional Help hooks; an incompatible optimization must not remove pricing.
                    for (int i = 2; i < hooks.Count; i++) harmony.Unpatch(hooks[i], HarmonyPatchType.All, harmony.Id);
                    Rules.BakeryHelpDiscovery.Changed -= HelpOptionChanged;
                    helpAvailable = false;
                    applyHelp = null;
                    Log.Warn("Bakery Help discovery could not be installed; original discovery retained, price patch active: " + ex);
                }
            }
            else Log.Warn("Bakery Help discovery: unreviewed or unreadable game/mod binary; original discovery retained. Price patch is independent.");
            if (!Rules.BakeryVanillaMargins.Enabled) return;
            var items = itemsField.GetValue(null) as Dictionary<string, Item>;
            if (items == null) return;
            foreach (var price in BakeryPrices.All)
            {
                Item item;
                if (items.TryGetValue(price.ItemName, out item) && item != null) Reprice(item, price);
            }
        }

        static IEnumerable<CodeInstruction> HelpLookupTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var find = AccessTools.Method(typeof(Resources), "FindObjectsOfTypeAll", new[] { typeof(Type) });
            var matches = code.Where(i => i.Calls(find)).ToList();
            if (matches.Count != 1) throw new InvalidOperationException("Bakery Help discovery call changed.");
            // Mutate the operand only, retaining branch labels and exception-block boundaries.
            matches[0].opcode = OpCodes.Call;
            matches[0].operand = AccessTools.Method(typeof(BakeryPatch), "FindHelp");
            return code;
        }

        static UnityEngine.Object[] FindHelp(Type type)
        {
            if (!Rules.BakeryHelpDiscovery.Enabled || type != typeof(HelpSystem))
            {
                long started = Stopwatch.GetTimestamp();
                var result = Resources.FindObjectsOfTypeAll(type);
                originalSearchMs += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
                originalLookups++;
                if (originalLookups == 1 || originalLookups % 60 == 0) LogHelp();
                return result;
            }
            // IsInitialized explicitly uses searchIfMissing:false. Do not call Instance on a miss:
            // that getter can itself search the scene and cache a missing-type marker.
            var help = buildingHelp;
            if (help == null && InstanceBehavior<HelpSystem>.IsInitialized) help = InstanceBehavior<HelpSystem>.Instance;
            oneHelp[0] = help;
            cachedLookups++;
            if (cachedLookups == 1 || cachedLookups % 60 == 0) LogHelp();
            return help == null ? noHelp : oneHelp;
        }

        static void BuildHelpPrefix(HelpSystem __instance)
        {
            if (!Rules.BakeryHelpDiscovery.Enabled || buildingHelp != null) return;
            buildingHelp = __instance;
            try
            {
                // Bakery still owns the entries. Insert them before the game creates their UI, and
                // avoid Bakery scheduling another LoadCategories while this one is already building.
                if (applyHelp(__instance, false)) helpBuilds++;
                LogHelp();
            }
            catch (Exception ex) { Log.Warn("Bakery Help: page insertion failed; the game's Help still builds: " + ex); }
            finally { buildingHelp = null; }
        }

        static bool RefreshHelpPrefix(ref bool __result)
        {
            if (!Rules.BakeryHelpDiscovery.Enabled || buildingHelp == null) return true;
            __result = false;
            return false;
        }

        static void HelpOptionChanged(bool enabled)
        {
            oneHelp[0] = null;
            // Help is lazy and might already be initialized after Bakery's retry window expired.
            // A one-time normal TryApply adds any missing pages and uses Bakery's own refresh path.
            if (enabled && helpAvailable)
            {
                try
                {
                    if (InstanceBehavior<HelpSystem>.IsInitialized)
                    {
                        var help = InstanceBehavior<HelpSystem>.Instance;
                        if (help != null) applyHelp(help, false);
                    }
                }
                catch (Exception ex) { Log.Warn("Bakery Help: could not populate existing Help on enable: " + ex); }
            }
            LogHelp();
        }

        static void LogHelp()
        {
            Log.Info("Bakery Help discovery: cachedLookups=" + cachedLookups + ", originalSearches=" + originalLookups
                + ", originalSearchMs=" + originalSearchMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                + ", populatedHelpBuilds=" + helpBuilds + ", enabled=" + Rules.BakeryHelpDiscovery.Enabled + ".");
        }

        static void RegisterPrefix(Item item)
        {
            try
            {
                if (!Rules.BakeryVanillaMargins.Enabled || item == null) return;
                var price = BakeryPrices.For(item.itemName);
                if (price != null) Reprice(item, price);
            }
            catch (Exception ex) { Log.Error("Bakery: price left as published: " + ex); }
        }

        static void Reprice(Item item, BakeryPrice price)
        {
            if (!originals.ContainsKey(item)) originals[item] = new KeyValuePair<float, float>(item.wholesalePrice, (float)marketPriceField.GetValue(item));
            item.wholesalePrice = price.Wholesale;
            marketPriceField.SetValue(item, price.Market);
            Log.Info("Bakery: " + price.ItemName + " now " + price.Wholesale.ToString("0.00") + " wholesale, " + price.Market.ToString("0.00") + " market.");
        }
    }
}
