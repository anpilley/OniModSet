using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;


namespace QuickWarpRecharge
{
    public class Patches
    {
        public class Mod_OnLoad : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                Debug.Log("Quick Recharge, gooooo!");

                harmony.PatchAll();
            }
        }

        [HarmonyPatch(typeof(WarpPortal.WarpPortalSM))]
        [HarmonyPatch("<InitializeStates>b__8_12")]
        public class WarpPortal_Anonymousb__8_12_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Debug.Log("[QuickRecharge]: Attempting to patch b__8_12 ");
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].OperandIs(3000.0))
                    {
                        Debug.Log("[QuickRecharge]: Found ldc_r4 3000 at " + i);
                        
                        codes[i].operand = 30.0f;
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }


        [HarmonyPatch(typeof(RailGun.StatesInstance))]
        [HarmonyPatch("HasEnergy")]
        public class RailGun_HasEnergy_Patch
        {
            public static bool Prefix(RailGun.StatesInstance __instance, ref bool __result)
            {
                __result = __instance.master.CurrentEnergy >= __instance.EnergyCost();
                return false;
            }
        }


        [HarmonyPatch(typeof(Bee))]
        [HarmonyPatch("OnPrefabInit")]
        public class Bee_OnPrefabInit_Patch
        {
            public static void Postfix(Bee __instance)
            {
                Debug.Log("[Bee Patch]: Lowering the death temperature for bees.");
                var monitor = __instance.GetDef<CritterTemperatureMonitor.Def>();
                monitor.temperatureColdDeadly = 73.15f;
                monitor.temperatureColdUncomfortable = 123.15f;
                monitor.temperatureHotDeadly = 333.15f;
                monitor.temperatureHotUncomfortable = 323.15f;

            }
        }


        [HarmonyPatch(typeof(HabitatModuleMediumConfig))]
        [HarmonyPatch("ConfigureBuildingTemplate")]
        public class HabitatModuleMediumConfig_ConfigureBuildingTemplate_Patch
        {
            public static void Postfix(HabitatModuleMediumConfig __instance, GameObject go)
            {
                Debug.Log("[Rocket habitat patch]: Insulating the liquid storage.");
                // The habitat modules don't seem to set the insulated property on the 10kg of liquid the input port
                // can store. Add the insulated property. This prevents a vanilla bug showing up in Rocketry Expanded.
                go.GetComponents<RocketConduitSender>()
                    .Do(p => p.conduitStorage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage));
            }
        }

    }
}
