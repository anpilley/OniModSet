using HarmonyLib;
using System.Diagnostics;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using KMod;
using UnityEngine.UI;

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

        #region // request crew side screen
        

        [HarmonyPatch(typeof(RequestCrewSideScreen))]
        [HarmonyPatch("OnShow")]
        public class HabitatModuleSideScreen_GetPassengerModule_Patch
        {
            public static void Postfix(RequestCrewSideScreen __instance)
            {
                //Debug.Log("OnShow Gameobject Heirarchy:");
                //GameObject go = __instance.gameObject;
                //while(go.transform.parent != null)
                //{
                //    Debug.Log("GameObject: "+ go.ToString() + ": " + go.name + "; " + go.GetProperName());
                //    go = go.transform.parent.gameObject;
                //}

            }
        }

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


        //[HarmonyPatch]
        //public class SuitMarker_SuitMarkerReactable_Run_Patch
        //{
        //    public static MethodBase TargetMethod()
        //    {
        //        var type = AccessTools.TypeByName("SuitMarker+SuitMarkerReactable");
        //        if (type != null)
        //        {
        //            Debug.Log("Got SuitMarker+SuitMarkerReactable");
        //            return AccessTools.FirstMethod(type, method => method.Name.Contains("Run"));
        //        }

        //        Debug.Log("Failed to find SuitMarkerReactable.Run");
        //        return null;
        //    }
        //    public static bool Prefix(Object __instance, GameObject ___reactor, SuitMarker ___suitMarker)
        //    {
        //        if (___reactor == null || ___suitMarker == null)
        //            return true;

        //        var ac = ___suitMarker.GetComponent<AccessControl>();
        //        if (ac == null)
        //        {
        //            Debug.Log("Unable to get access control component");
        //            return true;
        //        }
        //        Navigator navigator = ___reactor.GetComponent<Navigator>();

        //        var permission = ac.GetPermission(navigator);

        //        // TODO work out what direction the dupe is facing, compare it against the permission?

        //        if (permission == AccessControl.Permission.Neither)
        //        {
        //            // refuse to let dupe through
        //            return false;
        //        }

        //        // allow normal tests to occur.
        //        return true;

        //    }
        //}

        //[HarmonyPatch]
        //public class SuitMarker_SuitMarkerReactable_InternalCanBegin_Patch
        //{
        //    public static MethodBase TargetMethod()
        //    {
        //        var type = AccessTools.TypeByName("SuitMarker+SuitMarkerReactable");
        //        if (type != null)
        //        {
        //            return AccessTools.FirstMethod(type, method => method.Name.Contains("InternalCanBegin"));
        //        }

        //        Debug.Log("Failed to find SuitMarkerReactable.InternalCanBegin");
        //        return null;
        //    }

        //    public static bool Prefix(Object __instance, GameObject ___reactor, SuitMarker ___suitMarker, ref bool __result)
        //    {
        //        if (___reactor == null || ___suitMarker == null)
        //            return true;

        //        var ac = ___suitMarker.GetComponent<AccessControl>();
        //        if (ac == null)
        //        {
        //            Debug.Log("Unable to get access control component");
        //            return true;
        //        }
        //        Navigator navigator = ___reactor.GetComponent<Navigator>();

        //        var permission = ac.GetPermission(navigator);

        //        // TODO work out what direction the dupe is facing, compare it against the permission?

        //        if (permission == AccessControl.Permission.Neither)
        //        {
        //            // refuse to let dupe through
        //            __result = false;
        //            return false;
        //        }

        //        // normal code path.
        //        return true;
        //    }
        //}

    }
}
