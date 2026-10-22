using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HardModePatch.Core;
using HarmonyLib;

namespace HardModePatch
{
    // "Prefill the custom game with the series settings" (series_prefill_custom_game, on by default). The game's custom
    // game screen calls its public SetPreset(GameVariables) from Start with the first built-in preset unless
    // preventAutoSetOnStart is set. A postfix on Start calls SetPreset again with that preset plus the series values
    // (Core/SeriesSettings.NewGame), so the sliders show them and the player can still change anything before starting.
    // Everything is found by name: if the screen, the method or any field is missing or of another type, nothing is
    // patched and the screen keeps the game's own preset.
    public static class CustomGamePrefill
    {
        static readonly Harmony harmony = new Harmony(HardModePatchMod.HarmonyId + ".prefill");
        static MethodInfo setPreset, getPreset;
        static FieldInfo prevent;
        static Type variables;
        static readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();

        public static bool Available { get; private set; }

        // Start, then SetPreset; null where one is missing or changed. Also used by the offline check.
        public static IList<MethodBase> ResolveHooks()
        {
            fields.Clear();
            var handler = AccessTools.TypeByName("UI.MainMenu.CustomGameOptionsHandler");
            var start = handler == null ? null : AccessTools.DeclaredMethod(handler, "Start", Type.EmptyTypes);
            setPreset = handler == null ? null : AccessTools.GetDeclaredMethods(handler)
                .FirstOrDefault(m => m.Name == "SetPreset" && !m.IsStatic && m.ReturnType == typeof(void) && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.Name == "GameVariables");
            variables = setPreset == null ? null : setPreset.GetParameters()[0].ParameterType;
            getPreset = handler == null || variables == null ? null : AccessTools.DeclaredMethod(handler, "GetPreset", Type.EmptyTypes);
            if (getPreset != null && (!getPreset.IsStatic || getPreset.ReturnType != variables)) getPreset = null;
            prevent = handler == null ? null : AccessTools.DeclaredField(handler, "preventAutoSetOnStart");
            if (prevent != null && (prevent.IsStatic || prevent.FieldType != typeof(bool))) prevent = null;
            bool shape = variables != null && getPreset != null && prevent != null && variables.GetConstructor(Type.EmptyTypes) != null;
            foreach (var value in SeriesSettings.NewGame)
            {
                var field = variables == null ? null : variables.GetField(value.Field, BindingFlags.Public | BindingFlags.Instance);
                if (field == null || field.FieldType != value.Value.GetType()) { shape = false; continue; }
                fields[value.Field] = field;
            }
            return new MethodBase[]
            {
                shape && start != null && !start.IsStatic && start.ReturnType == typeof(void) ? start : null,
                shape ? setPreset : null
            };
        }

        public static void Start()
        {
            try
            {
                var hooks = ResolveHooks();
                if (hooks.Any(h => h == null))
                {
                    Log.Warn("Custom game prefill unavailable: the game's custom game screen changed; it keeps the game's own preset.");
                    return;
                }
                harmony.Patch(hooks[0], postfix: new HarmonyMethod(typeof(CustomGamePrefill), "StartPostfix"));
                Available = true;
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll(harmony.Id);
                Available = false;
                Log.Error("Custom game prefill unavailable: " + ex.Message);
            }
        }

        public static void Stop()
        {
            harmony.UnpatchAll(harmony.Id);
            Available = false;
        }

        // The game's preset with the series values on top: a new GameVariables, every public field copied, then ours.
        internal static object SeriesPreset(object current)
        {
            var preset = Activator.CreateInstance(variables);
            if (current != null)
                foreach (var field in variables.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    field.SetValue(preset, field.GetValue(current));
            foreach (var value in SeriesSettings.NewGame) fields[value.Field].SetValue(preset, value.Value);
            return preset;
        }

        static void StartPostfix(object __instance)
        {
            try
            {
                if (!Rules.SeriesPrefill.Enabled || __instance == null || (bool)prevent.GetValue(__instance)) return;
                setPreset.Invoke(__instance, new[] { SeriesPreset(getPreset.Invoke(null, null)) });
                Log.Info("Custom game prefilled with the HART series settings; change any of them before starting.");
            }
            catch (Exception ex)
            {
                Log.Error("Custom game prefill failed; check the custom game values before starting: " + ex.Message);
            }
        }
    }
}
