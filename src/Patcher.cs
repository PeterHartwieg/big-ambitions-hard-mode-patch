using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using HardModePatch.Core;
using HarmonyLib;

namespace HardModePatch
{
    // One patch per Workshop mod. The patch never references the mod's DLL at compile time: it finds the
    // loaded assembly by name, so a player without that mod only loses that patch.
    public interface ITargetPatch
    {
        TargetInfo Target { get; }
        // Every method the patch hooks, resolved in the target assembly (or the game). A null entry means
        // the method is missing or its signature changed.
        IList<MethodBase> ResolveHooks(Assembly target);
        void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks);
    }

    public sealed class Patcher
    {
        readonly Harmony harmony;
        readonly ITargetPatch[] patches;
        readonly Dictionary<string, TargetState> states = new Dictionary<string, TargetState>();
        readonly object gate = new object();
        bool listening;

        public Patcher(Harmony harmony, ITargetPatch[] patches)
        {
            this.harmony = harmony;
            this.patches = patches;
            foreach (var patch in patches) states[patch.Target.Key] = TargetState.Missing;
        }

        public TargetState StateOf(TargetInfo target)
        {
            lock (gate) { TargetState state; return states.TryGetValue(target.Key, out state) ? state : TargetState.Missing; }
        }

        // Mods load in an order the player doesn't control, so the targets already loaded are patched now
        // and the rest when their assembly arrives.
        public void Start()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            listening = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) Consider(assembly);
        }

        public void Stop()
        {
            if (listening) AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            listening = false;
            harmony.UnpatchAll(harmony.Id);
        }

        void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            try { Consider(args.LoadedAssembly); }
            catch (Exception ex) { Log.Error("Could not inspect " + args.LoadedAssembly.GetName().Name + ": " + ex); }
        }

        void Consider(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            foreach (var patch in patches)
            {
                if (!string.Equals(patch.Target.AssemblyName, name, StringComparison.Ordinal)) continue;
                lock (gate)
                {
                    if (states[patch.Target.Key] != TargetState.Missing) return;   // already handled this session
                    Install(patch, assembly);
                }
                Options.Refresh();
            }
        }

        void Install(ITargetPatch patch, Assembly assembly)
        {
            var target = patch.Target;
            IList<MethodBase> hooks;
            try { hooks = patch.ResolveHooks(assembly); }
            catch (Exception ex) { Log.Error(target.Title + ": hook lookup failed, left unpatched: " + ex); states[target.Key] = TargetState.Incompatible; return; }
            int found = 0;
            var missing = new List<int>();
            for (int index = 0; index < hooks.Count; index++)
            {
                if (hooks[index] != null) found++;
                else missing.Add(index);
            }
            string hash = HashOf(assembly);
            var state = TargetGate.Classify(true, hash, target.VerifiedHashes, hooks.Count, found);
            states[target.Key] = state;
            string version = target.Title + " (" + (hash ?? "hash unreadable") + ")";
            if (state == TargetState.Incompatible)
            {
                Log.Warn(version + ": " + missing.Count + " of " + hooks.Count + " hooked methods are missing or changed (hook " + string.Join(", ", missing) + "). Left unpatched; it runs as its author published it.");
                return;
            }
            try
            {
                patch.Apply(harmony, assembly, hooks);
            }
            catch (Exception ex)
            {
                // A half-applied target is worse than an unpatched one.
                foreach (var hook in hooks) if (hook != null) harmony.Unpatch(hook, HarmonyPatchType.All, harmony.Id);
                states[target.Key] = TargetState.Incompatible;
                Log.Error(version + ": patching failed, left unpatched: " + ex);
                return;
            }
            if (state == TargetState.Verified) Log.Info(version + ": verified version, patched.");
            else Log.Warn(version + ": a version this patch hasn't reviewed. Its hooked methods are unchanged, so it is patched; report problems with this line from the log.");
        }

        static string HashOf(Assembly assembly)
        {
            try
            {
                string path = assembly.Location;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                using (var sha = SHA256.Create())
                {
                    byte[] digest = sha.ComputeHash(File.ReadAllBytes(path));
                    return TargetGate.HashPrefix(BitConverter.ToString(digest).Replace("-", string.Empty));
                }
            }
            catch (Exception) { return null; }
        }
    }
}
