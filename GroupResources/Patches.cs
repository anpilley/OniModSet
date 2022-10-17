using HarmonyLib;
using rail;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static PinnedResourcesPanel;

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
        /// 
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

                Color lightBlue = new Color(0.013f, 0.78f, 1.0f);

                Debug.Log("[Grouped Resources]: Category switching");
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
                                Debug.Log("[Grouped Resources]: White: " + category.ProperNameStripLink());
                                categoryColor = Color.white;
                            }
                            else
                            {
                                Debug.Log("[Grouped Resources]: Blue: " + category.ProperNameStripLink());
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

        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("CreateRow")]
        public class PinnedResourcesPanel_CreateRow_Patch
        {
            public static void Postfix(PinnedResourcesPanel __instance, Tag tag, ref PinnedResourcesPanel.PinnedResourceRow __result)
            {
                // Tack on an extra BG, so we can shade that.
                var bg = __result.gameObject.transform.Find("BG").gameObject;
                var additiveBG = Util.KInstantiate(bg, bg, "CategoryBG");
                additiveBG.rectTransform().localScale = new Vector3(1.0f, 1.0f);
                additiveBG.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.0f, bg.rectTransform().rect.width);
                additiveBG.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0.0f, bg.rectTransform().rect.height);

            }
        }
    }
}
