using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HardModePatch.Core;
using HarmonyLib;

namespace HardModePatch
{
    // Hardcore Ambitions (3809287926) sets up any new game as its own: 23 difficulty values, a $75,454
    // student loan, three investment funds and $551 cash, and higher vehicle and boat prices. This keeps
    // the indebted start and the vehicle prices, and lets the save keep its own difficulty:
    //
    // - EnforceHardcoreGameVariables writes the 23 values and re-prices every vanilla loan at its 2.56
    //   interest, on every load. Skipped. The student loan keeps the mod's fixed $16/$158 a day terms,
    //   which its bootstrap re-applies itself.
    // - EnsureStartingReserve buys the three funds and sets cash to $551.37. Replaced by setting the cash
    //   and the mod's own "done" flag, so it never tries again.
    // - ApplyCatalogPriceTargets edits the vehicle and boat assets, which last until the game quits.
    //   The prices are recorded before its first write and put back when a save it didn't mark loads.
    public sealed class HardcoreAmbitionsPatch : ITargetPatch
    {
        const string ModType = "EconomyPresets.HardcoreAmbitions.HardcoreAmbitionsPrototypeMod";
        const int EnforceHook = 0, ReserveHook = 1, CatalogueHook = 2, GameLoadedHook = 3;

        public TargetInfo Target { get { return Targets.HardcoreAmbitions; } }

        public IList<MethodBase> ResolveHooks(Assembly target)
        {
            var mod = target.GetType(ModType, false);
            var hooks = new MethodBase[4];
            if (mod != null)
            {
                hooks[EnforceHook] = Method(mod, "EnforceHardcoreGameVariables", typeof(GameInstance));
                // The second parameter is the mod's private BankBundle class; only its position is relied on.
                hooks[ReserveHook] = AccessTools.GetDeclaredMethods(mod).FirstOrDefault(m =>
                    m.Name == "EnsureStartingReserve" && m.ReturnType == typeof(bool)
                    && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(GameInstance));
                hooks[CatalogueHook] = Method(mod, "ApplyCatalogPriceTargets", typeof(string), typeof(string), typeof(Dictionary<double, double>));
            }
            hooks[GameLoadedHook] = AccessTools.Method(typeof(GlobalEvents), "InvokeOnGameLoaded", Type.EmptyTypes);
            return hooks;
        }

        static MethodBase Method(Type type, string name, params Type[] parameters)
        {
            var method = AccessTools.Method(type, name, parameters);
            return method != null && method.DeclaringType == type ? method : null;
        }

        public void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks)
        {
            var self = typeof(HardcoreAmbitionsPatch);
            harmony.Patch(hooks[EnforceHook], prefix: new HarmonyMethod(self, "EnforcePrefix"));
            harmony.Patch(hooks[ReserveHook], prefix: new HarmonyMethod(self, "ReservePrefix"));
            harmony.Patch(hooks[CatalogueHook], prefix: new HarmonyMethod(self, "CataloguePrefix"));
            harmony.Patch(hooks[GameLoadedHook], postfix: new HarmonyMethod(self, "GameLoadedPostfix"));
        }

        static bool loggedKeep;

        static bool EnforcePrefix()
        {
            if (!Rules.HardcoreKeepDifficulty.Enabled) return true;
            if (!loggedKeep) Log.Info("Hardcore Ambitions: the save keeps its own difficulty settings; the student loan keeps the mod's $16 interest and $158 payment a day.");
            loggedKeep = true;
            return false;
        }

        static bool ReservePrefix(object[] __args, ref bool __result)
        {
            if (!Rules.HardcoreNoFunds.Enabled) return true;
            var game = __args[0] as GameInstance;
            if (game == null) return true;
            if (game.modData == null) game.modData = new Dictionary<string, string>();
            game.Money = HardcoreStart.StartingCash;
            game.modData[HardcoreStart.ReserveInitializedFlag] = "1";
            game.modData[HardcoreStart.NoFundsFlag] = "1";
            __result = true;
            Log.Info("Hardcore Ambitions: start set up without the investment funds; cash " + HardcoreStart.StartingCash.ToString("0.00") + ".");
            return false;
        }

        // Prices as they were before Hardcore Ambitions' first write, per catalogue asset.
        sealed class PriceRecord
        {
            public UnityEngine.Object Asset;
            public double Price;
        }
        static readonly Dictionary<string, List<PriceRecord>> snapshot = new Dictionary<string, List<PriceRecord>>();
        static int gamesLoaded;

        static void CataloguePrefix(string typeName)
        {
            try
            {
                if (typeName == null || snapshot.ContainsKey(typeName)) return;
                var type = AccessTools.AllTypes().FirstOrDefault(t => t.Name == typeName && typeof(UnityEngine.Object).IsAssignableFrom(t));
                if (type == null) return;   // not loaded yet; Hardcore Ambitions retries and so do we
                var records = new List<PriceRecord>();
                foreach (var asset in UnityEngine.Resources.FindObjectsOfTypeAll(type))
                {
                    double price;
                    if (PriceMember.TryRead(asset, out price)) records.Add(new PriceRecord { Asset = asset, Price = price });
                }
                if (records.Count == 0) return;
                snapshot[typeName] = records;
                Log.Info("Hardcore Ambitions: recorded " + records.Count + " " + typeName + " prices before the Hardcore changes.");
            }
            catch (Exception ex) { Log.Error("Hardcore Ambitions: could not record " + typeName + " prices: " + ex); }
        }

        static void GameLoadedPostfix()
        {
            try
            {
                var game = SaveGameManager.Current;
                if (game == null) return;
                gamesLoaded++;
                string flag;
                bool hardcore = game.modData != null && game.modData.TryGetValue(HardcoreStart.SaveEnabledFlag, out flag) && flag == "1";
                // Hardcore Ambitions registers its setup callback once per launch, and the game clears it after
                // the first load, so a later Hardcore save gets neither its prices nor its checks.
                if (hardcore && gamesLoaded > 1)
                    Log.Warn("Hardcore Ambitions only sets up the first save loaded in a session. Restart the game before playing this save.");
                if (!Rules.HardcoreVehiclePricesPerSave.Enabled) return;
                if (!HardcoreStart.ShouldRestoreVanillaPrices(snapshot.Count > 0, hardcore)) return;
                int restored = 0;
                foreach (var records in snapshot.Values)
                    foreach (var record in records)
                        if (record.Asset != null && PriceMember.TryWrite(record.Asset, record.Price)) restored++;
                Log.Info("Hardcore Ambitions: this save isn't a Hardcore save; restored " + restored + " vehicle and boat prices.");
            }
            catch (Exception ex) { Log.Error("Hardcore Ambitions: could not restore vehicle prices: " + ex); }
        }
    }

    // Reads and writes a catalogue price the way Hardcore Ambitions does: "Price", then "price", as a field
    // or a property.
    static class PriceMember
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Names = { "Price", "price" };

        public static bool TryRead(object owner, out double value)
        {
            value = 0;
            foreach (var name in Names)
            {
                object raw;
                var field = owner.GetType().GetField(name, Flags);
                if (field != null) raw = field.GetValue(owner);
                else
                {
                    var property = owner.GetType().GetProperty(name, Flags);
                    if (property == null || !property.CanRead || property.GetIndexParameters().Length > 0) continue;
                    raw = property.GetValue(owner, null);
                }
                if (!(raw is float || raw is double || raw is int || raw is long || raw is decimal)) continue;
                value = Convert.ToDouble(raw);
                return true;
            }
            return false;
        }

        public static bool TryWrite(object owner, double value)
        {
            foreach (var name in Names)
            {
                try
                {
                    var field = owner.GetType().GetField(name, Flags);
                    if (field != null) { field.SetValue(owner, Convert.ChangeType(value, field.FieldType)); return true; }
                    var property = owner.GetType().GetProperty(name, Flags);
                    if (property != null && property.CanWrite) { property.SetValue(owner, Convert.ChangeType(value, property.PropertyType), null); return true; }
                }
                catch (Exception) { }
            }
            return false;
        }
    }
}
