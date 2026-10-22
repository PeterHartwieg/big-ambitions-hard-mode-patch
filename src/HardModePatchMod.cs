using System;
using System.Reflection;
using System.Threading.Tasks;
using BAModAPI;
using HarmonyLib;

[assembly: AssemblyVersion("0.4.2.0")]
[assembly: AssemblyInformationalVersion("0.4.2")]
[assembly: RegisterModClass(typeof(HardModePatch.HardModePatchMod))]
namespace HardModePatch
{
    // Initialization scope, like the mods it patches: the hooks have to be in place before a save loads,
    // because Hardcore Ambitions sets up a new game on the first load of a session.
    [ModEntryOnInitializationLoad]
    public sealed class HardModePatchMod : IModBigAmbitions
    {
        public const string HarmonyId = "hardmodepatch";
        public const string Version = "0.4.2";
        Patcher patcher;

        public string[] RelativeAssetBundlePaths { get { return new string[0]; } }

        public Task OnLoadAsync(ModContext context)
        {
            try
            {
                Options.Load(context.ModId);
                IndebtedScenario.Start();
                RentRebalance.Start();
                HappinessRebalance.Start();
                OwnerJobPay.Start();
                RunningCostsPatch.Start();
                patcher = new Patcher(new Harmony(HarmonyId), new ITargetPatch[]
                {
                    new HardcoreAmbitionsPatch(), new MeaningfulAmbiencePatch(), new BakeryPatch(),
                    new InternetCafePatch(), new VendingMachinesPatch(), new EmpireBankPatch(), new HyperrealPatch(), new RealLifeOverhaulPatch()
                });
                patcher.Start();
                RealLifeOverhaulPatch.ReportAvailability();
                Options.Register(context.ModId, patcher);
                Log.Info("Version " + Version + " active, Harmony " + typeof(Harmony).Assembly.GetName().Version + ". Mods not loaded yet are patched when they load.");
            }
            catch (Exception ex)
            {
                Log.Error("Not active, every mod runs unchanged: " + ex);
                if (patcher != null) patcher.Stop();
                IndebtedScenario.Stop();
                RentRebalance.Stop();
                HappinessRebalance.Stop();
                OwnerJobPay.Stop();
                RunningCostsPatch.Stop();
                patcher = null;
            }
            return Task.FromResult(0);
        }

        public Task OnUnloadAsync()
        {
            Options.Unregister();
            if (patcher != null) patcher.Stop();
            IndebtedScenario.Stop();
            RentRebalance.Stop();
            HappinessRebalance.Stop();
            OwnerJobPay.Stop();
            RunningCostsPatch.Stop();
            patcher = null;
            return Task.FromResult(0);
        }
    }

    static class Log
    {
        // Outside Unity (the offline checks) the engine's log can't be called; the message is dropped there.
        public static void Info(string message) { try { UnityEngine.Debug.Log("[HART. Mode Patch] " + message); } catch (System.Security.SecurityException) { } }
        public static void Warn(string message) { try { UnityEngine.Debug.LogWarning("[HART. Mode Patch] " + message); } catch (System.Security.SecurityException) { } }
        public static void Error(string message) { try { UnityEngine.Debug.LogError("[HART. Mode Patch] " + message); } catch (System.Security.SecurityException) { } }
    }
}
