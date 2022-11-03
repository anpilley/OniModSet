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
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static STRINGS.CREATURES.STATUSITEMS;

namespace GroupResources
{
    public class Patches
    {
        public class Mod_OnLoad : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                Debug.Log("Grouped Resources Gooooooo!");

                harmony.PatchAll();

                PUtil.InitLibrary();
                new POptions().RegisterOptions(this, typeof(GroupResourceSettings));
            }
        }

        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("OnPrefabInit")]
        public class PinnedResourcesPanel_OnPrefabInit_Patch
        {
            /// <summary>
            /// Runs some quick cleanup routines under the assumption that we might be reloading a save from main menu.
            /// </summary>
            public static void Prefix(PinnedResourcesPanel __instance)
            {
                var categoryManager = __instance.gameObject.AddOrGet<CategoryManager>();
                var settings = POptions.ReadSettings<GroupResourceSettings>();
                categoryManager.Init(settings);
            }
        }

        [HarmonyPatch(typeof(SaveGame))]
        [HarmonyPatch("OnPrefabInit")]
        public class SaveGame_OnPrefabInit_Patch
        {
            /// <summary>
            /// Attach a component to save/reload collapse/expanded state.
            /// </summary>
            /// <param name="__instance">"this"</param>
            public static void Postfix(SaveGame __instance)
            {
                __instance.gameObject.AddOrGet<ResourceHeaderState>();
            }
        }

        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("SortRows")]
        public class PinnedResourcesPanel_SortRows_Patch
        {

            /// <summary>
            /// Sort the pinned resources list by category, name. Hide/unhide based on category collapse.
            /// </summary>
            /// <param name="__instance">this</param>
            /// <param name="___rows">set of pinned tags and gameobjects</param>
            /// <param name="___clearNewButton">button</param>
            /// <param name="___seeAllButton">button</param>
            /// <returns></returns>
            public static bool Prefix(PinnedResourcesPanel __instance, 
                Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> ___rows)
            {
                return __instance.GetComponent<CategoryManager>().SortRows(___rows);
            }
        }

        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("CreateRow")]
        public class PinnedResourcesPanel_CreateRow_Patch 
        {

            /// <summary>
            /// Create Row adds a background to each resource entry, and adds new category headers as new ones show up.
            /// </summary>
            /// <param name="__instance">"this"</param>
            /// <param name="tag">The tag of a resource being added</param>
            /// <param name="__result">The return result from the original CreateRow</param>
            /// <param name="___rowContainerLayout">The QuickLayout, since we need to force a layout.</param>
            /// <param name="___rows"></param>
            /// <param name="___clearNewButton"></param>
            /// <param name="___seeAllButton"></param>
            public static void Postfix(PinnedResourcesPanel __instance, 
                Tag tag, 
                ref PinnedResourcesPanel.PinnedResourceRow __result, 
                QuickLayout ___rowContainerLayout, 
                Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> ___rows)
            {
                __instance.GetComponent<CategoryManager>().CreateCategoryRow(ref __result, tag, ___rowContainerLayout, ___rows);
            }
        }
    }
}
