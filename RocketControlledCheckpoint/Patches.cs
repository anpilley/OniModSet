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
using UnityEngine;
using System.Collections.Generic;
using static Components;

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
        /// <summary>
        /// Add Controlled/Uncontrolled button to suit markers when inside rocket capsules.
        /// </summary>
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
                }
                else
                {
                    Utility.ErrorLog("Failed to get Suit Marker for Dock");
                }
            }
        }

        /// <summary>
        /// Add an automation input port for the suit checkpoint.
        /// </summary>
        [HarmonyPatch(typeof(SuitMarkerConfig))]
        [HarmonyPatch("CreateBuildingDef")]
        public class SuitMarkerConfig_CreateBuildingDef_Patch
        {
            public static void Postfix(ref BuildingDef __result)
            {
                __result.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
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
            public static PassengerRocketModule patchRocketModule = null;
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
                    if (patchRocketModule == null)
                    {
                        Utility.ErrorLog("patchRocketModule is null unexpectedly, ignoring Limit button press!");
                        return;
                    }
                    patchRocketModule.RequestCrewBoard((PassengerRocketModule.RequestCrewState)3);
                    RequestCrewSideScreen_RefreshRequestButtons_Patch.RefreshRequestButtons(__instance);
#if DEBUG
                    Utility.DebugLog("Limit button clicked");
#endif
                };

                ___toggleMap.Add(limitedButton, (PassengerRocketModule.RequestCrewState)3);

            }
        }

        /// <summary>
        /// Godawful hack to make sure we can access the passenger module from onClick
        /// </summary>
        [HarmonyPatch(typeof(RequestCrewSideScreen))]
        [HarmonyPatch("SetTarget")]
        public class RequestCrewSideScreen_SetTarget_Patch
        {
            public static void Postfix(PassengerRocketModule ___rocketModule)
            {
                // Pass this to the static tracking variable, so we can use it in limitedButton.onClick
                RequestCrewSideScreen_OnSpawn_Patch.patchRocketModule = ___rocketModule;
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
#if DEBUG
                    Utility.DebugLog("Added button to RCS prefab");
#endif

                }
            }
        }
        #endregion

        #region // PassengerRocketModule

        /// <summary>
        /// Add a "Limit" option to passenger modules that allows crew to come and go (via access permissions),
        /// force non-crew out.
        /// </summary>
        [HarmonyPatch(typeof(PassengerRocketModule))]
        [HarmonyPatch("RefreshOrders")]
        public class PassengerRocketModule_RefreshOrders_Patch
        {
            public static bool Prefix(PassengerRocketModule __instance)
            {
                // only works if landed, otherwise, do nothing, it'll get refreshed later.
                if (!__instance.HasTag(GameTags.RocketOnGround) || !__instance.GetComponent<ClustercraftExteriorDoor>().HasTargetWorld()) 
                    return true;

                // we're overriding this with "3" which is a bonus enum value for an int-backed enum.
                if (__instance.PassengersRequested == (PassengerRocketModule.RequestCrewState)3)
                {
#if DEBUG
                    Utility.DebugLog("Crew state 3 detected." + __instance);

#endif
                    int exteriorCell = __instance.GetComponent<NavTeleporter>().GetCell();
                    int interiorCell = __instance.GetComponent<ClustercraftExteriorDoor>().TargetCell();

                    foreach (MinionIdentity minionIdentity in Components.LiveMinionIdentities.Items)
                    {
                        bool isAssignedMinion = 
                            Game.Instance.assignmentManager.assignment_groups[
                                __instance.GetComponent<AssignmentGroupController>().AssignmentGroupID
                                ].HasMember((IAssignableIdentity)minionIdentity.assignableProxy.Get());
                        
                        bool isMinionOnBoard = minionIdentity.GetMyWorldId() == (int)Grid.WorldIdx[interiorCell];

                        if(!isAssignedMinion && isMinionOnBoard)
                        {
                            // Remove non-crew member if they're inside.
                            minionIdentity.GetSMI<RocketPassengerMonitor.Instance>().SetMoveTarget(exteriorCell);
                        }

                        if(!isAssignedMinion)
                        {
                            // Don't let non-Crew back in. (restrict = true)
                            PassengerRocketModule_RefreshAccessStatus_Patch
                                .RefreshAccessStatus(__instance, minionIdentity, true);
                        }
                        
                        if(isAssignedMinion && !isMinionOnBoard)
                        {
                            // For Crew, clear their orders, in case they're being told to come here by a previous Crew setting.
                            minionIdentity.GetSMI<RocketPassengerMonitor.Instance>().ClearMoveTarget(interiorCell);
                        }

                        if(isAssignedMinion)
                        {
                            // Let crew come and go. (restrict = false)
                            PassengerRocketModule_RefreshAccessStatus_Patch
                                .RefreshAccessStatus(__instance, minionIdentity, false);
                        }
                    }

                    // don't continue with RefreshOrders
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Reverse patch RefreshAccessStatus to access the private method in PassengerRocketModule
        /// </summary>
        [HarmonyPatch]
        public class PassengerRocketModule_RefreshAccessStatus_Patch
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(PassengerRocketModule), "RefreshAccessStatus")]
            public static void RefreshAccessStatus(object instance, MinionIdentity minion, bool restrict)
            {
                Utility.ErrorLog("RefreshAccessStatus stub");
            }
        }
        #endregion

    }
}
