using System;
using System.Reflection;
using System.Threading.Tasks;
using BAModAPI;
using HarmonyLib;

[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyInformationalVersion("0.1.0")]
[assembly: RegisterModClass(typeof(HardModePatch.HardModePatchMod))]
namespace HardModePatch
{
    // Initialization scope, like the mods it patches: the hooks have to be in place before a save loads,
    // because Hardcore Ambitions sets up a new game on the first load of a session.
    [ModEntryOnInitializationLoad]
    public sealed class HardModePatchMod : IModBigAmbitions
    {
        public const string HarmonyId = "hardmodepatch";
        public const string Version = "0.1.0";
        Patcher patcher;

        public string[] RelativeAssetBundlePaths { get { return new string[0]; } }

        public Task OnLoadAsync(ModContext context)
        {
            try
            {
                Options.Load(context.ModId);
                patcher = new Patcher(new Harmony(HarmonyId), new ITargetPatch[]
                {
                    new HardcoreAmbitionsPatch(), new MeaningfulAmbiencePatch(), new BakeryPatch(),
                    new InternetCafePatch(), new VendingMachinesPatch(), new EmpireBankPatch()
                });
                patcher.Start();
                Options.Register(context.ModId, patcher);
                Log.Info("Version " + Version + " active, Harmony " + typeof(Harmony).Assembly.GetName().Version + ". Mods not loaded yet are patched when they load.");
            }
            catch (Exception ex)
            {
                Log.Error("Not active, every mod runs unchanged: " + ex);
                if (patcher != null) patcher.Stop();
                patcher = null;
            }
            return Task.FromResult(0);
        }

        public Task OnUnloadAsync()
        {
            Options.Unregister();
            if (patcher != null) patcher.Stop();
            patcher = null;
            return Task.FromResult(0);
        }
    }

    static class Log
    {
        public static void Info(string message) { UnityEngine.Debug.Log("[Hard Mode Patch] " + message); }
        public static void Warn(string message) { UnityEngine.Debug.LogWarning("[Hard Mode Patch] " + message); }
        public static void Error(string message) { UnityEngine.Debug.LogError("[Hard Mode Patch] " + message); }
    }
}
