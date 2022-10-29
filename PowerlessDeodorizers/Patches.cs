// Copyright © 2022 Andrew Pilley
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
// the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
