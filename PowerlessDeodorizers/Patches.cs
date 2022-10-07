using HarmonyLib;

namespace PowerlessDeodorizers
{
    public class Patches
    {
        public class Mod_OnLoad : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                Debug.Log("Powerless Deodorizers Gooooo!");
                harmony.PatchAll();
            }
        }

        [HarmonyPatch(typeof(AirFilterConfig))]
        [HarmonyPatch("CreateBuildingDef")]
        public class AirFilterConfig_CreateBuildingDef_Patch
        {
            public static void Prefix()
            {
                Debug.Log("Preparing to patch Air Filters!");
            }

            public static void Postfix(ref BuildingDef __result)
            {
                __result.RequiresPowerInput = false;
                __result.PowerInputOffset = CellOffset.none;
                __result.EnergyConsumptionWhenActive = 0f;
                __result.ExhaustKilowattsWhenActive = 0f;
                __result.SelfHeatKilowattsWhenActive = 0f;
                Debug.Log("Patched AirFilters!");
            }
        }
    }
}
