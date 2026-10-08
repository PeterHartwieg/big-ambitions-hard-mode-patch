using System;
using System.Collections.Generic;
using System.Reflection;
using HardModePatch.Core;
using HarmonyLib;

namespace HardModePatch
{
    // Vending Machines (3812054455) turns on the game's developer flag BuildingManager
    // .isBuildingTemporarilyEditable while the player holds a machine in a rival's building, so the machine
    // can be placed there. The same flag lets the player pick up and move any item in that building. While
    // the mod has the flag up (RivalPlacement._raised), only its own items can be picked up.
    public sealed class VendingMachinesPatch : ITargetPatch
    {
        const string OwnItemPrefix = "vendingmachines:";
        static FieldInfo raised;

        public TargetInfo Target { get { return Targets.VendingMachines; } }

        public IList<MethodBase> ResolveHooks(Assembly target)
        {
            var placement = target.GetType("VendingMachines.Game.RivalPlacement", false);
            raised = placement == null ? null : AccessTools.Field(placement, "_raised");
            bool ready = raised != null && raised.IsStatic && raised.FieldType == typeof(bool);
            return new MethodBase[]
            {
                ready ? AccessTools.Method(typeof(ItemController), "SecondaryInteract", Type.EmptyTypes) : null,
                ready ? AccessTools.Method(typeof(ItemController), "TryToGrabItem", Type.EmptyTypes) : null
            };
        }

        public void Apply(Harmony harmony, Assembly target, IList<MethodBase> hooks)
        {
            harmony.Patch(hooks[0], prefix: new HarmonyMethod(typeof(VendingMachinesPatch), "InteractPrefix"));
            harmony.Patch(hooks[1], prefix: new HarmonyMethod(typeof(VendingMachinesPatch), "GrabPrefix"));
        }

        static bool InteractPrefix(ItemController __instance)
        {
            return !Locked(__instance);
        }

        static bool GrabPrefix(ItemController __instance, ref bool __result)
        {
            if (!Locked(__instance)) return true;
            __result = false;
            return false;
        }

        static bool Locked(ItemController item)
        {
            try
            {
                if (!Rules.VendingProtectRivalFurniture.Enabled || item == null) return false;
                if (!BuildingManager.isBuildingTemporarilyEditable || !(bool)raised.GetValue(null)) return false;
                if (!BuildingManager.IsInsideBuilding) return false;
                var manager = InstanceBehavior<BuildingManager>.Instance;
                var building = manager == null ? null : manager.buildingRegistration;
                if (building == null || building.RentedByPlayer) return false;
                return item.itemName == null || !item.itemName.StartsWith(OwnItemPrefix, StringComparison.Ordinal);
            }
            catch (Exception) { return false; }
        }
    }
}
