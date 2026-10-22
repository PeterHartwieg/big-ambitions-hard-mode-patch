using System;
using System.Reflection;
using HardModePatch.Core;
using HarmonyLib;
using UI.Notification;

namespace HardModePatch
{
    // Real Life Overhaul updates through the Workshop on its own schedule, so viewers get each release the day it ships.
    // Everything HMP does to or with RLO (factory diplomas, running costs, owner job pay, the hotel "no home" rule) runs
    // only on an RLO build whose code was reviewed for it (Core/Targets). Any other build gets one log line and, when a
    // save loads, one notice; those features stay off and RLO runs as its author published it. Such an RLO drops HMP's
    // diploma records from a save the next time it saves, so they are copied to HMP's own save line first and given back
    // when a reviewed RLO and HMP's courses are loaded again (Core/DiplomaHold).
    public static class RloGate
    {
        static readonly Harmony harmony = new Harmony(HardModePatchMod.HarmonyId + ".rlogate");
        static readonly object gate = new object();
        static Assembly seen;
        static bool accepted, noticeShown, heldNoticeShown;

        public static string Message { get; private set; }

        // An RLO assembly was seen and it isn't a reviewed build.
        public static bool Refused { get { lock (gate) return seen != null && !accepted; } }

        public static MethodBase ResolveHook()
        {
            var loaded = AccessTools.DeclaredMethod(typeof(GlobalEvents), "InvokeOnGameLoaded", Type.EmptyTypes);
            return loaded != null && loaded.IsStatic && loaded.ReturnType == typeof(void) ? loaded : null;
        }

        public static void Start()
        {
            try
            {
                var hook = ResolveHook();
                if (hook == null) throw new MissingMethodException("GlobalEvents.InvokeOnGameLoaded changed.");
                harmony.Patch(hook, prefix: new HarmonyMethod(typeof(RloGate), "GameLoadedPrefix"));
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll(harmony.Id);
                Log.Error("Real Life Overhaul check on save load unavailable; held diploma records are not given back this session: " + ex.Message);
            }
        }

        public static void Stop()
        {
            harmony.UnpatchAll(harmony.Id);
        }

        // Decided once per RLO assembly, logged once.
        public static bool Accepts(Assembly rlo)
        {
            if (rlo == null) return false;
            lock (gate)
            {
                if (ReferenceEquals(rlo, seen)) return accepted;
                seen = rlo;
                string hash = Patcher.HashOf(rlo);
                accepted = TargetGate.Reviewed(hash, Targets.RealLifeOverhaul.VerifiedHashes);
                var version = rlo.GetName().Version;
                string described = "Real Life Overhaul " + (version == null ? "?" : version.ToString(3)) + " (" + (hash ?? "hash unreadable") + ")";
                if (accepted)
                {
                    Message = null;
                    Log.Info(described + ": a reviewed version; factory diplomas, running costs, owner job pay and the hotel rule can run.");
                }
                else
                {
                    Message = described + " is a version HART. Mode Patch hasn't reviewed. Factory diplomas, running costs, owner job pay"
                        + " and the hotel 'no home' rule are off until HART. Mode Patch is updated; RLO runs as its author published it."
                        + " A save's factory diploma records are kept for that update.";
                    Log.Warn(Message);
                }
                return accepted;
            }
        }

        static void GameLoadedPrefix()
        {
            try { OnGameLoaded(SaveGameManager.Current); }
            catch (Exception ex) { Log.Error("Real Life Overhaul check on save load failed: " + ex); }
        }

        // Runs before game-loaded subscribers, so before RLO's careers director first reads the save (a timer after load).
        internal static bool OnGameLoaded(GameInstance game)
        {
            bool refused;
            lock (gate)
            {
                if (seen == null) return false;
                refused = !accepted;
            }
            // Game-loaded also fires when character creation opens, before the city exists; nothing is read then.
            if (game == null || game.BuildingRegistrations == null || game.BuildingRegistrations.Count == 0) return false;
            var data = game.modData;
            string line = null, held = null;
            if (data != null)
            {
                data.TryGetValue(DiplomaHold.CareersKey, out line);
                data.TryGetValue(DiplomaHold.HoldKey, out held);
            }
            if (refused)
            {
                string kept = DiplomaHold.Keep(line, held);
                if (kept != null && kept != held)
                {
                    data[DiplomaHold.HoldKey] = kept;
                    Log.Info("Factory diplomas: " + DiplomaHold.Count(kept) + " diploma record(s) of this save held for a reviewed Real Life Overhaul.");
                }
                Notice(kept != null);
                return true;
            }
            if (held == null) return false;
            if (!RealLifeOverhaulPatch.CoursesRegistered)
            {
                Log.Warn("Factory diplomas: this save has held diploma records, but HMP's courses aren't registered in Real Life Overhaul; they stay held.");
                return false;
            }
            if (DiplomaHold.Covered(line, held))
            {
                data.Remove(DiplomaHold.HoldKey);
                Log.Info("Factory diplomas: the held diploma records are back in Real Life Overhaul's careers record; the hold is cleared.");
                return true;
            }
            int added;
            string restored = DiplomaHold.Restore(line, held, out added);
            if (restored == null)
            {
                Log.Warn("Factory diplomas: held diploma records can't be given back yet (no careers record in this save, or another format); they stay held.");
                return false;
            }
            data[DiplomaHold.CareersKey] = restored;
            Log.Info("Factory diplomas: " + added + " held diploma record(s) given back to Real Life Overhaul; the hold stays until RLO has saved them.");
            return true;
        }

        static void Notice(bool held)
        {
            lock (gate)
            {
                if (held ? heldNoticeShown : noticeShown) return;
                noticeShown = true;
                if (held) heldNoticeShown = true;
            }
            try { Notifications.Show(NotificationType.Warning, held ? "hardmode_rlo_unreviewed_held" : "hardmode_rlo_unreviewed", null, 15f, "HmpRloUnreviewed"); }
            catch (Exception ex) { Log.Error("Could not show the Real Life Overhaul notice: " + ex.Message); }
        }
    }
}
