using HarmonyLib;
using System.Diagnostics;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using KMod;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RocketControlledCheckpoint
{
    public class Patches
    {
        public class Mod_OnLoad : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                Utility.DebugLog(" Controllable Suit Checkpoints!");
                harmony.PatchAll();
            }
        }

        #region // Suit Marker

        [HarmonyPatch(typeof(SuitMarkerConfig))]
        [HarmonyPatch("DoPostConfigureComplete")]
        public class SuitMarkerConfig_DoPostConfigureComplete_Patch
        {
            public static void Postfix(GameObject go)
            {
                SuitMarker suitMarker = go.AddOrGet<SuitMarker>();
                if (suitMarker)
                {
                    go.AddOrGetDef<RocketUsageRestriction.Def>();

                    go.AddOrGet<AccessControl>().controlEnabled = true;
                }
                else
                {
                    Utility.ErrorLog("Failed to get Suit Marker for Dock");
                }
            }
        }

        #endregion

        #region // request crew side screen

        /// <summary>
        /// Reverse patch private method RefreshRequestButtons so i can call it from other patches.
        /// </summary>
        [HarmonyPatch]
        public class RequestCrewSideScreen_RefreshRequestButtons_Patch
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(RequestCrewSideScreen), "RefreshRequestButtons")]
            public static void RefreshRequestButtons(object instance)
            {
                Utility.ErrorLog("RefreshRequestButtons stub");
            }
        }

        /// <summary>
        /// Append extra behavior for the OnSpawn, hooking up the onClick behavior.
        /// </summary>
        [HarmonyPatch(typeof(RequestCrewSideScreen))]
        [HarmonyPatch("OnSpawn")]
        public class RequestCrewSideScreen_OnSpawn_Patch
        {
            public static void Prefix(
                RequestCrewSideScreen __instance,
                PassengerRocketModule ___rocketModule,
                Dictionary<KToggle, PassengerRocketModule.RequestCrewState> ___toggleMap)
            {
                Utility.DebugLog("Current PassengerRocketModuleState = " + (int)___rocketModule.PassengersRequested);

                var limitedButtonGO = __instance.transform.Find("ButtonContainer/Buttons/Limited").gameObject;
                var limitedButton = limitedButtonGO.GetComponent<KToggle>();
                limitedButton.onClick += () =>
                {
                    ___rocketModule.RequestCrewBoard((PassengerRocketModule.RequestCrewState)3);
                    RequestCrewSideScreen_RefreshRequestButtons_Patch.RefreshRequestButtons(__instance);
                };

                ___toggleMap.Add(limitedButton, (PassengerRocketModule.RequestCrewState)3);

            }
        }
        #endregion


        #region // KScreen
        /// <summary>
        /// Patch KScreen to get access to OnPrefabInit for RequestCrewSidescreen, add our extra button
        /// </summary>
        [HarmonyPatch(typeof(KScreen))]
        [HarmonyPatch("OnPrefabInit")]
        public class KScreen_OnPrefabInit_Patch
        {
            public static void Postfix(KScreen __instance)
            {
                if(__instance is RequestCrewSideScreen rcss)
                {
                    Utility.DebugLog("Got RequestCrewSideScreen::OnPrefabInit");

                    // Buttons found using DNI, alt-u, inspecting button to discover RCSS has
                    // ButtonContainer -> Buttons -> Release|Request.
                    var allButton = __instance.transform.Find("ButtonContainer/Buttons/Release").gameObject;
                    var crewButton = __instance.transform.Find("ButtonContainer/Buttons/Request").gameObject;
                    var buttonsGroup = __instance.transform.Find("ButtonContainer/Buttons").gameObject;

                    if (!allButton || !crewButton)
                    {
                        Utility.ErrorLog("Couldn't find All or Crew button");
                        return;
                    }
                    
                    // Resize the existing buttons
                    allButton.rectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 80f);
                    crewButton.rectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 80f);
                    crewButton.rectTransform().SetInsetAndSizeFromParentEdge(
                        RectTransform.Edge.Right, 25f, crewButton.rectTransform().sizeDelta.x);

                    // add another in.
                    var limitedButton = Util.KInstantiate(allButton, buttonsGroup, "Limited");
                    limitedButton.rectTransform().localScale = new Vector3(1.0f, 1.0f);
                    var locText = limitedButton.GetComponentInChildren<LocText>();
                    locText.text = "Limit";

                    limitedButton.rectTransform().SetInsetAndSizeFromParentEdge(
                        RectTransform.Edge.Left, 25f, limitedButton.rectTransform().sizeDelta.x);
                    
                    limitedButton.GetComponent<ToolTip>().toolTip = "Limit entry to Crew Only";

                }
            }
        }
        #endregion

    }
}
