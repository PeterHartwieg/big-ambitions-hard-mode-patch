using System;
using System.Collections.Generic;
using System.Reflection;
using Buildings.Retail.Simulation;
using HardModePatch.Core;
using HarmonyLib;

namespace HardModePatch
{
    // Internet Cafe (3796860957). Off screen, RetailBusinessSimulator caps an hour's customers by building
    // capacity and by staffed items. The cafe's PCs need no staff, so a one-PC cafe could serve the whole
    // building at $24 a head with no cost of goods. The postfix caps a cafe at two sessions per usable PC.
    public sealed class InternetCafePatch : ITargetPatch
    {
        static readonly string[] ComputerNames = { "ba:itemname_gamingcomputer", "ba:itemname_desktopcomputer", "ba:itemname_computer", "ba:itemname_laptop" };
        static Type cafeSimulator;
        static AccessTools.FieldRef<object, BuildingRegistration> registration;

        public TargetInfo Target { get { return Targets.InternetCafe; } }

        public IList<MethodBase> ResolveHooks(Assembly target)
        {
            cafeSimulator = target.GetType("InternetCafe.CafeBusinessSimulator", false);
            var capacity = AccessTools.Method(typeof(RetailBusinessSimulator), "GetMaxCustomerCapacityThisHour", Type.EmptyTypes);
            var field = AccessTools.Field(typeof(RetailBusinessSimulator), "buildingRegistration");
            if (capacity == null || capacity.ReturnType != typeof(int) || field == null || field.FieldType != typeof(BuildingRegistration)) capacity = null;
            else registration = AccessTools.FieldRefAccess<object, BuildingRegistration>(field);
            MethodBase cafeService = cafeSimulator == null || !typeof(RetailBusinessSimulator).IsAssignableFrom(cafeSimulator)
                ? null
                : AccessTools.DeclaredMethod(cafeSimulator, "GetCustomerService");
            return new MethodBase[] { capacity, cafeService };
        }

        public void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks)
        {
            harmony.Patch(hooks[0], postfix: new HarmonyMethod(typeof(InternetCafePatch), "CapacityPostfix"));
        }

        static void CapacityPostfix(object __instance, ref int __result)
        {
            if (!Rules.CafePcCapacity.Enabled || __instance == null || __instance.GetType() != cafeSimulator) return;
            try
            {
                var building = registration(__instance);
                if (building == null || building.itemInstances == null) return;
                int pcs = 0;
                foreach (var item in building.itemInstances.Values)
                    if (item != null && Array.IndexOf(ComputerNames, item.itemName) >= 0 && !ItemHelper.HasAnyMissingRequirements(item)) pcs++;
                __result = CafeMath.Cap(__result, pcs);
            }
            catch (Exception ex) { Log.Error("Internet Cafe: capacity left as the game computed it: " + ex); }
        }
    }
}
