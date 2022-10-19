using HarmonyLib;
using rail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PinnedResourcesPanel;
using static STRINGS.DUPLICANTS.PERSONALITIES;

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
            }
        }

        /// <summary>
        /// cache of the pickupables to category tags, since the lookup is expensive.
        /// </summary>
        public static Dictionary<Tag, Tag> PickupableToCategoryCache = new Dictionary<Tag, Tag>();

        /// <summary>
        /// Add category to the pickupable -> tag cache if it's not already in there.
        /// </summary>
        /// <param name="rowTag">The resource to lookup and add.</param>
        public static void AddCategoryToCache(Tag rowTag)
        {
            if(rowTag == null)
            {
                Debug.LogError("[GroupResources]: rowTag unexpectedly null");
            }
            HashSet<Tag> resourceTags = null;
            
            foreach (Tag materialCategory in GameTags.MaterialCategories)
            {
                if (DiscoveredResources.Instance.TryGetDiscoveredResourcesFromTag(materialCategory, out resourceTags))
                {
                    if (resourceTags.Contains(rowTag))
                    {
                        PickupableToCategoryCache.Add(rowTag, materialCategory);
                        return;
                    }
                }
            }
            foreach (Tag calorieCategory in GameTags.CalorieCategories)
            {
                if (DiscoveredResources.Instance.TryGetDiscoveredResourcesFromTag(calorieCategory, out resourceTags))
                {
                    if (resourceTags.Contains(rowTag))
                    {
                        PickupableToCategoryCache.Add(rowTag, calorieCategory);
                        return;
                    }
                }
            }

            foreach (Tag unitCategory in GameTags.UnitCategories)
            {
                if (DiscoveredResources.Instance.TryGetDiscoveredResourcesFromTag(unitCategory, out resourceTags))
                {
                    if (resourceTags.Contains(rowTag))
                    {
                        PickupableToCategoryCache.Add(rowTag, unitCategory);
                        return;
                    }
                }
            }

            // Miscellaneous
            if (DiscoveredResources.Instance.TryGetDiscoveredResourcesFromTag(GameTags.Miscellaneous, out resourceTags))
            {
                if (resourceTags.Contains(rowTag))
                {
                    PickupableToCategoryCache.Add(rowTag, GameTags.Miscellaneous);
                    return;
                }
            }

            // MiscPickupable
            if (DiscoveredResources.Instance.TryGetDiscoveredResourcesFromTag(GameTags.MiscPickupable, out resourceTags))
            {
                if (resourceTags.Contains(rowTag))
                {
                    PickupableToCategoryCache.Add(rowTag, GameTags.MiscPickupable);
                    return;
                }
            }

            Debug.LogError("[GroupResources]: Couldn't find a category for Tag: " + rowTag.ProperName());
        }

        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("SortRows")]
        public class PinnedResourcesPanel_SortRows_Patch
        {
            private static Color lightBlue = new Color(0.013f, 0.78f, 1.0f);

            /// <summary>
            /// Sort the pinned resources list by category, name.
            /// </summary>
            /// <param name="__instance">this</param>
            /// <param name="___rows">set of pinned tags and gameobjects</param>
            /// <param name="___clearNewButton">button</param>
            /// <param name="___seeAllButton">button</param>
            /// <returns></returns>
            private static bool Prefix(PinnedResourcesPanel __instance, ref Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> ___rows, MultiToggle ___clearNewButton, MultiToggle ___seeAllButton)
            {
                foreach (Tag rowTag in ___rows.Keys)
                {
                    // look up in existing tag->category lookup cache
                    if (!PickupableToCategoryCache.ContainsKey(rowTag))
                    {
                        // Add it if it's not already there.
                        AddCategoryToCache(rowTag);
                    }
                }
                
                List<PinnedResourcesPanel.PinnedResourceRow> pinnedResourceRowList = new List<PinnedResourcesPanel.PinnedResourceRow>();
                foreach (KeyValuePair<Tag, PinnedResourcesPanel.PinnedResourceRow> row in ___rows)
                    pinnedResourceRowList.Add(row.Value);

                pinnedResourceRowList.Sort((Comparison<PinnedResourcesPanel.PinnedResourceRow>)((a, b) => {
                    if (PickupableToCategoryCache[a.Tag] == PickupableToCategoryCache[b.Tag])
                        return a.SortableNameWithoutLink.CompareTo(b.SortableNameWithoutLink);

                    return PickupableToCategoryCache[a.Tag].ProperNameStripLink().CompareTo(PickupableToCategoryCache[b.Tag].ProperNameStripLink());
                }));  // sort alphabetically, group by category.

                Tag category = null;
                
                Color categoryColor = lightBlue;

                foreach (var item in pinnedResourceRowList)
                {
                    if (___rows[item.Tag].gameObject.activeSelf) // rows don't get removed between spaced out views, so skip changing the color of inactive ones.
                    {
                        if (PickupableToCategoryCache[item.Tag] != category)
                        {
                            category = PickupableToCategoryCache[item.Tag];

                            if (categoryColor == lightBlue)
                            {
                                categoryColor = Color.white;
                            }
                            else
                            {
                                categoryColor = lightBlue;
                            }
                        }
                    }
                    ___rows[item.Tag].gameObject.transform.SetAsLastSibling();
                    
                    var categoryBG = ___rows[item.Tag].gameObject.transform.Find("BG/CategoryBG").gameObject;
                    var img = categoryBG.GetComponent<Image>();
                    
                    img.color = categoryColor;
                    img.SetAlpha(0.5f);

                }
                ___clearNewButton.transform.SetAsLastSibling();
                ___seeAllButton.transform.SetAsLastSibling();

                return false;
            }
        }

        //[HarmonyPatch(typeof(QuickLayout))]
        //[HarmonyPatch("Layout")]
        //public class QuickLayout_Layout_Patch
        //{
        //    public static void Prefix(QuickLayout __instance, Vector2 ____offset)
        //    {
        //        Debug.Log("QuickLayout: " + __instance.transform.gameObject.name);
        //        if (__instance.transform.gameObject.name == "EntryContainer")
        //        {
        //            Debug.Log("QuickLayout: Got EntryContainer, offset: "+____offset);
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(PinnedResourcesPanel))]
        //[HarmonyPatch("Render1000ms")]
        //public class PinnedResourcesPanel_Render1000ms_Patch
        //{
        //    public static bool test = false;
        //    public static void Postfix(PinnedResourcesPanel __instance, ref Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> ___rows, QuickLayout ___rowContainerLayout)
        //    {
        //        if (test == true)
        //            return;

        //        Debug.Log("[Group resources]: Render1000ms");

        //        if(___rows.Count > 0)
        //        {
        //            test = true;
        //            Tag tag = ___rows.Keys.First<Tag>();
        //            var headerRef = ___rows[tag].gameObject.transform.parent.parent.Find("HeaderLayout").gameObject;
        //            var categoryHeader = Util.KInstantiateUI(headerRef, __instance.rowContainer, true);
        //            categoryHeader.name = tag.ProperNameStripLink(); // TODO translation?

        //            categoryHeader.rectTransform().localScale = new Vector3(1.0f, 1.0f);

        //            categoryHeader.GetComponentInChildren<LocText>().SetText(tag.ProperNameStripLink());

        //            categoryHeader.rectTransform().anchoredPosition = new Vector2(0.0f, 0.0f);
        //            categoryHeader.rectTransform().pivot = new Vector2(1.0f, 0.0f);
        //            categoryHeader.rectTransform().anchorMax = new Vector2(0.0f, 1.0f);
        //            categoryHeader.rectTransform().anchorMin = new Vector2(0.0f, 1.0f);

        //            categoryHeader.rectTransform().SetLocalPosition((Vector3)new Vector2(0.0f, 0.0f));

        //            PinnedResourcesPane_SortRows_ReversePatch.SortRows(__instance);
        //            ___rowContainerLayout.ForceUpdate();
                    

        //            Debug.Log("[GroupResources]: Render1000ms Adding category " + tag.ProperNameStripLink());
        //        }
        //    }
        //}

        [HarmonyPatch]
        public class PinnedResourcesPane_SortRows_ReversePatch
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(PinnedResourcesPanel), "SortRows")]
            public static void SortRows(object instance)
            {
                Debug.LogError("SortRows stub");
            }
        }

        public class CategoryHeader
        {
            public GameObject go;
            public string name;
        }

        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("CreateRow")]
        public class PinnedResourcesPanel_CreateRow_Patch
        {
            public static GameObject resourcesHeader = null;
            public static Dictionary<Tag, CategoryHeader> materialHeaders = new Dictionary<Tag, CategoryHeader>();
            public static bool scheduled = false;
            public static void Postfix(PinnedResourcesPanel __instance, Tag tag, ref PinnedResourcesPanel.PinnedResourceRow __result, QuickLayout ___rowContainerLayout)
            {
                // look up in existing tag->category lookup cache
                if (!PickupableToCategoryCache.ContainsKey(tag))
                {
                    // Add it if it's not already there.
                    AddCategoryToCache(tag);
                }

                var category = PickupableToCategoryCache[tag];

                // The parent to this should be the EntryContainer, parent to that is the Resource header.
                if (!resourcesHeader)
                {
                    resourcesHeader = __result.gameObject.transform.parent.parent.Find("HeaderLayout").gameObject;
                }

                if (!materialHeaders.ContainsKey(category))
                {
                    //var categoryHeader = Util.KInstantiateUI(resourcesHeader, __instance.rowContainer, true);
                    //categoryHeader.name = category.ProperNameStripLink(); // TODO translation?

                    //categoryHeader.rectTransform().localScale = new Vector3(1.0f, 1.0f);

                    //categoryHeader.GetComponentInChildren<LocText>().SetText(category.ProperNameStripLink());

                    //categoryHeader.rectTransform().anchoredPosition = new Vector2(0.0f, 0.0f);
                    //categoryHeader.rectTransform().pivot = new Vector2(0.0f, 0.0f);
                    //categoryHeader.rectTransform().anchorMax = new Vector2(0.0f, 1.0f);
                    //categoryHeader.rectTransform().anchorMin = new Vector2(0.0f, 1.0f);


                    //categoryHeader.rectTransform().SetLocalPosition((Vector3)new Vector2(0.0f, 0.0f));

                    Debug.Log("[GroupResources]: Adding category " + category.ProperNameStripLink());
                    // defer creation until later.
                    materialHeaders.Add(category, new CategoryHeader() { name= category.ProperNameStripLink(), go = null});

                    if (!scheduled)
                    {
                        GameScheduler.Instance.Schedule("GroupResourcesCreateCategoryHeaders", 0f, MakeHeaders);
                        scheduled = true;
                    }

                    //Debug.Log("[Group Resources]: anchor: (" + categoryHeader.rectTransform().anchoredPosition.x + "," + categoryHeader.rectTransform().anchoredPosition.y + ") " + categoryHeader.ToString());
                    //Debug.Log("[Group Resources]: pivot: (" + categoryHeader.rectTransform().pivot.x + "," + categoryHeader.rectTransform().pivot.y + ")");
                    //Debug.Log("[Group Resources]: anchormax: (" + categoryHeader.rectTransform().anchorMax.x + "," + categoryHeader.rectTransform().anchorMax.y + ")");
                    //Debug.Log("[Group Resources]: anchormin: (" + categoryHeader.rectTransform().anchorMin.x + "," + categoryHeader.rectTransform().anchorMin.y + ")");
                    //Debug.Log("[Group Resources]: row anchor: (" + __result.gameObject.rectTransform().anchoredPosition.x + "," + __result.gameObject.rectTransform().anchoredPosition.y + ")");
                    //Debug.Log("[Group Resources]: row pivot: (" + __result.gameObject.rectTransform().pivot.x + "," + __result.gameObject.rectTransform().pivot.y + ")");
                    //Debug.Log("[Group Resources]: anchormax: (" + __result.gameObject.rectTransform().anchorMax.x + "," + __result.gameObject.rectTransform().anchorMax.y + ")");
                    //Debug.Log("[Group Resources]: anchormin: (" + __result.gameObject.rectTransform().anchorMin.x + "," + __result.gameObject.rectTransform().anchorMin.y + ")");

                }

                // Tack on an extra BG, so we can shade that.
                var bg = __result.gameObject.transform.Find("BG").gameObject;
                var additiveBG = Util.KInstantiate(bg, bg, "CategoryBG");
                additiveBG.rectTransform().localScale = new Vector3(1.0f, 1.0f);
                additiveBG.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.0f, bg.rectTransform().rect.width);
                additiveBG.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0.0f, bg.rectTransform().rect.height);

                void MakeHeaders(object go)
                {
                    foreach(Tag headerTag in materialHeaders.Keys)
                    {
                        if(materialHeaders[headerTag].go == null)
                        {
                            var categoryHeader = Util.KInstantiateUI(resourcesHeader, __instance.rowContainer, true);
                            categoryHeader.name = materialHeaders[headerTag].name;

                            categoryHeader.rectTransform().localScale = new Vector3(1.0f, 1.0f);

                            categoryHeader.GetComponentInChildren<LocText>().SetText(headerTag.ProperNameStripLink()); // TODO translation?

                            //categoryHeader.rectTransform().anchoredPosition = new Vector2(0.0f, 0.0f);
                            categoryHeader.rectTransform().pivot = new Vector2(1.0f, 1.0f);
                            //categoryHeader.rectTransform().anchorMax = new Vector2(0.0f, 1.0f);
                            //categoryHeader.rectTransform().anchorMin = new Vector2(0.0f, 1.0f);
                            materialHeaders[headerTag].go = categoryHeader;
                        }
                    }

                    PinnedResourcesPane_SortRows_ReversePatch.SortRows(__instance);
                    ___rowContainerLayout.ForceUpdate();
                    scheduled = false;
                }
            }
        }
        
        //[HarmonyPatch(typeof(PinnedResourcesPanel))]
        //[HarmonyPatch("Populate")]
        //public class PinnedResourcesPanel_Populate_Patch
        //{
        //    public static void Postfix(ref Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> ___rows)
        //    {
        //        Debug.Log("ForcedUpdate finished.");
        //        if(PinnedResourcesPanel_CreateRow_Patch.materialHeaders != null)
        //        {
        //            foreach (var categoryHeader in PinnedResourcesPanel_CreateRow_Patch.materialHeaders.Values)
        //            {
        //                PrintDetails(categoryHeader, 0);
        //            }


        //            foreach (var rowBits in ___rows.Values)
        //            {
        //                Debug.Log("[Group Resources]: position: (" + rowBits.gameObject.rectTransform().position.x + "," + rowBits.gameObject.rectTransform().position.y + ") " + rowBits.gameObject.ToString());
        //                Debug.Log("[Group Resources]: anchor: (" + rowBits.gameObject.rectTransform().anchoredPosition.x + "," + rowBits.gameObject.rectTransform().anchoredPosition.y + ")");
        //                Debug.Log("[Group Resources]: pivot: (" + rowBits.gameObject.rectTransform().pivot.x + "," + rowBits.gameObject.rectTransform().pivot.y + ")");
        //                Debug.Log("[Group Resources]: anchormax: (" + rowBits.gameObject.rectTransform().anchorMax.x + "," + rowBits.gameObject.rectTransform().anchorMax.y + ")");
        //                Debug.Log("[Group Resources]: anchormin: (" + rowBits.gameObject.rectTransform().anchorMin.x + "," + rowBits.gameObject.rectTransform().anchorMin.y + ")");
        //            }
        //        }
        //    }

        //    private static void PrintDetails(GameObject target, int level)
        //    {
        //        Debug.Log("Level: " + level + ", " + target.name);
        //        Debug.Log("[Group Resources]: position: (" + target.rectTransform().position.x + "," + target.rectTransform().position.y + ")" );
        //        Debug.Log("[Group Resources]: anchor: (" + target.rectTransform().anchoredPosition.x + "," + target.rectTransform().anchoredPosition.y + ")");
        //        Debug.Log("[Group Resources]: pivot: (" + target.rectTransform().pivot.x + "," + target.rectTransform().pivot.y + ")");
        //        Debug.Log("[Group Resources]: anchormax: (" + target.rectTransform().anchorMax.x + "," + target.rectTransform().anchorMax.y + ")");
        //        Debug.Log("[Group Resources]: anchormin: (" + target.rectTransform().anchorMin.x + "," + target.rectTransform().anchorMin.y + ")");

        //        for (int index = 0;  index < target.transform.childCount; index++)
        //        {
        //            PrintDetails(target.transform.GetChild(index).gameObject, level+1);
        //        }
        //    }
        //}
    }
}
