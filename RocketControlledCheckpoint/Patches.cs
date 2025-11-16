// Copyright © 2022-2025 Andrew Pilley
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
using System.Runtime.CompilerServices;

namespace RocketControlledCheckpoint
{
    public class Patches
    {
        public class Mod_OnLoad : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                Utility.DebugLog("Controllable Suit Checkpoints!");
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

        #endregion

        #region // summon crew side screen

        [HarmonyPatch(typeof(SummonCrewSideScreen))]
        [HarmonyPatch("ToggleCrewRequestState")]
        public class SummonCrewSideScreen_ToggleCrewRequestState_Patch
        {
            public static PassengerRocketModule patchRocketModule = null;
            public static bool Prefix(SummonCrewSideScreen __instance)
            {
                if(patchRocketModule == null)
                {
                    Utility.DebugLog("Didn't have a ref to the PassengerRocketModule in ToggleCrewRequestState");
                    return true;
                }
                switch (patchRocketModule.PassengersRequested)
                {
                    case PassengerRocketModule.RequestCrewState.Request:
                        patchRocketModule.RequestCrewBoard(PassengerRocketModule.RequestCrewState.Release);
                        break;
                    case PassengerRocketModule.RequestCrewState.Release:
                        patchRocketModule.RequestCrewBoard((PassengerRocketModule.RequestCrewState)3);
                        break;
                    case ((PassengerRocketModule.RequestCrewState)3):
                        patchRocketModule.RequestCrewBoard(PassengerRocketModule.RequestCrewState.Request);
                        break;
                    default:
                        patchRocketModule.RequestCrewBoard(PassengerRocketModule.RequestCrewState.Release);
                        break;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(SummonCrewSideScreen))]
        [HarmonyPatch("Refresh")]
        public class SummonCrewSideScreen_Refresh_Patch
        {
            private static Color defaultColor = new Color(0.5568628f, 0.5568628f, 0.5568628f, 1f);
            public static bool Prefix(SummonCrewSideScreen __instance)
            {
                if (SummonCrewSideScreen_ToggleCrewRequestState_Patch.patchRocketModule is null)
                    return true;

                var passengerRocketModule = SummonCrewSideScreen_ToggleCrewRequestState_Patch.patchRocketModule;
                __instance.button.isInteractable = passengerRocketModule.GetCrewCount() > 0;

                if (passengerRocketModule.PassengersRequested == PassengerRocketModule.RequestCrewState.Release)
                {
                    __instance.infoLabel.SetText(STRINGS.UI.UISIDESCREENS.SUMMON_CREW_SIDESCREEN.INFO_LABEL_PUBLIC_ACCESS);
                    __instance.infoLabelTooltip.SetSimpleTooltip(STRINGS.UI.UISIDESCREENS.SUMMON_CREW_SIDESCREEN.INFO_LABEL_TOOLTIP_PUBLIC_ACCESS);

                    __instance.buttonLabel.SetText("Limit to Crew Only");
                    __instance.buttonTooltip.SetSimpleTooltip("Only duplicants on the crew list will be allowed to enter or exit.");
                    __instance.image.sprite = Assets.GetSprite((HashedString)"status_item_change_door_control_state");
                    __instance.image.color = defaultColor;
                    return false;
                }
                else if (passengerRocketModule.PassengersRequested == 
                    (PassengerRocketModule.RequestCrewState)3)
                {
                    //Utility.DebugLog("Showing the Crew Limited button (state 3).");
                    __instance.infoLabel.SetText("Crew Limited");
                    __instance.infoLabelTooltip.SetSimpleTooltip("The rocket module will only allow crew members to enter or exit.");
                    __instance.buttonLabel.SetText(STRINGS.UI.UISIDESCREENS.SUMMON_CREW_SIDESCREEN.SUMMON_CREW_BUTTON_LABEL);
                    __instance.buttonTooltip.SetSimpleTooltip(STRINGS.UI.UISIDESCREENS.SUMMON_CREW_SIDESCREEN.SUMMON_CREW_BUTTON_TOOLTIP);
                    __instance.image.sprite = Assets.GetSprite((HashedString)"status_item_change_door_control_state");
                    __instance.image.color = Color.blue;
                    return false;
                }

                //Utility.DebugLog("Showing some other state.");
                return true;
            }
        }

        /// <summary>
        /// Godawful hack to make sure we can access the passenger module from onClick
        /// </summary>
        [HarmonyPatch(typeof(SummonCrewSideScreen))]
        [HarmonyPatch("SetTarget")]
        public class SummonCrewSideScreen_SetTarget_Patch
        {
            public static void Prefix(GameObject target)
            {
                // Pass this to the static tracking variable, so we can use it in limitedButton.onClick
                if (target != null)
                {
                    CraftModuleInterface craftModuleInterface = null;
                    RocketModuleCluster component = target.GetComponent<RocketModuleCluster>();
                    if (component != null)
                    {
                        //Utility.DebugLog("Using the selected rocket CraftInterface");
                        craftModuleInterface = component.CraftInterface;
                    }
                    else if (target.GetComponent<RocketControlStation>() != null)
                    {
                        //Utility.DebugLog("We seem to be inside a rocket, using this world's ModuleInterface");
                        craftModuleInterface = target.GetMyWorld().GetComponent<Clustercraft>().ModuleInterface;
                    }

                    if (craftModuleInterface is null)
                    {
                        //Utility.DebugLog("Couldn't find CraftModuleInterface");
                        return;
                    }

                    //Utility.DebugLog("Updating the sidescreen target to " + craftModuleInterface.GetPassengerModule());
                    SummonCrewSideScreen_ToggleCrewRequestState_Patch.patchRocketModule = craftModuleInterface.GetPassengerModule();
                }
            }
        }

        [HarmonyPatch(typeof(SummonCrewSideScreen))]
        [HarmonyPatch("ClearTarget")]
        public class SummonCrewSideScreen_ClearTarget_Patch
        {
            public static void Postfix()
            {
                //Utility.DebugLog("Clearing the sidescreen target.");
                SummonCrewSideScreen_ToggleCrewRequestState_Patch.patchRocketModule = null;
                
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
                    Utility.DebugLog("Crew state 3 detected. " + __instance);

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
