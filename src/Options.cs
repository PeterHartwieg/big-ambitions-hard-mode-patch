using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Mods;
using HardModePatch.Core;

namespace HardModePatch
{
    // The native options page: one header per target mod with its patch state, then that mod's rules.
    // The game keeps each toggle in PlayerPrefs under "m:<modId>:<optionId>"; the rules read those at load
    // so they hold before the page is first opened, and the callbacks only carry later changes.
    static class Options
    {
        static string registeredId;
        static Patcher patcher;

        public static string PrefKey(string modId, string optionId) { return "m:" + modId + ":" + optionId; }

        public static void Load(string modId)
        {
            foreach (var rule in Rules.All)
            {
                string key = PrefKey(modId, rule.Id);
                rule.Enabled = UnityEngine.PlayerPrefs.HasKey(key) ? UnityEngine.PlayerPrefs.GetInt(key) != 0 : rule.Default;
            }
            Log.Info("Rules: " + string.Join(", ", Rules.All.Select(r => r.Id + "=" + (r.Enabled ? "on" : "off")).ToArray()) + ".");
        }

        public static void Register(string modId, Patcher source)
        {
            patcher = source;
            registeredId = modId;
            Build();
        }

        // Called again once a target has been inspected, so the page shows its state.
        public static void Refresh()
        {
            if (registeredId != null) Build();
        }

        public static void Unregister()
        {
            if (registeredId == null) return;
            try { OptionsService.RemoveModOptions(registeredId); }
            catch (Exception) { }
            registeredId = null;
            patcher = null;
        }

        static void Build()
        {
            try { OptionsService.RemoveModOptions(registeredId); }
            catch (Exception) { }
            var options = new ModOptions().AddHeader("hardmode_title");
            foreach (var target in Rules.All.Select(r => r.Target).Distinct())
            {
                options.AddSplitter()
                    .AddHeader("hardmode_target_" + target.Key)
                    .AddHeader(StateKey(patcher == null ? TargetState.Missing : patcher.StateOf(target)));
                foreach (var rule in Rules.All.Where(r => r.Target == target))
                {
                    var bound = rule;
                    options.AddToggle(bound.Id, bound.LabelKey, bound.Default, value =>
                    {
                        if (bound.Enabled == value) return;
                        Log.Info(bound.Id + " turned " + (value ? "on" : "off") + ".");
                        try { bound.Set(value); }
                        catch (Exception ex) { Log.Error(bound.Id + ": " + ex); }
                    });
                }
            }
            OptionsService.Register(registeredId, options);
        }

        static string StateKey(TargetState state)
        {
            switch (state)
            {
                case TargetState.Verified: return "hardmode_state_verified";
                case TargetState.Unverified: return "hardmode_state_unverified";
                case TargetState.Incompatible: return "hardmode_state_incompatible";
                default: return "hardmode_state_missing";
            }
        }
    }
}
