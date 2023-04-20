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

namespace NoSoddingSoMeteors
{
    public class Patches
    {
        public class Mod_OnLoad : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                Debug.Log("Fuck you meteors, Gooooo!");
                harmony.PatchAll();
            }
        }

        [HarmonyPatch(typeof(Db))]
        [HarmonyPatch(nameof(Db.Initialize))]
        public class Db_Initialize_Patch
        {
            public static void Postfix(Db __instance)
            {
                Debug.Log("Removing meteors");
                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.SpacedOutStyleStartMeteorShowers);
                __instance.GameplaySeasons.SpacedOutStyleStartMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.SpacedOutStyleRocketMeteorShowers);
                __instance.GameplaySeasons.SpacedOutStyleRocketMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.SpacedOutStyleWarpMeteorShowers);
                __instance.GameplaySeasons.SpacedOutStyleWarpMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.ClassicStyleStartMeteorShowers);
                __instance.GameplaySeasons.ClassicStyleStartMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.ClassicStyleWarpMeteorShowers);
                __instance.GameplaySeasons.ClassicStyleWarpMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.MarshyMoonletMeteorShowers);
                __instance.GameplaySeasons.MarshyMoonletMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.TundraMoonletMeteorShowers);
                __instance.GameplaySeasons.TundraMoonletMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.NiobiumMoonletMeteorShowers);
                __instance.GameplaySeasons.NiobiumMoonletMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.WaterMoonletMeteorShowers);
                __instance.GameplaySeasons.WaterMoonletMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.MiniMetallicSwampyMeteorShowers);
                __instance.GameplaySeasons.MiniMetallicSwampyMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.MiniForestFrozenMeteorShowers);
                __instance.GameplaySeasons.MiniForestFrozenMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.MiniBadlandsMeteorShowers);
                __instance.GameplaySeasons.MiniBadlandsMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.MiniFlippedMeteorShowers);
                __instance.GameplaySeasons.MiniFlippedMeteorShowers = null;

                __instance.GameplaySeasons.Remove(__instance.GameplaySeasons.MiniRadioactiveOceanMeteorShowers);
                __instance.GameplaySeasons.MiniRadioactiveOceanMeteorShowers = null;
            }
        }
    }
}
