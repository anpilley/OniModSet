using HarmonyLib;
using rail;
using System;
using System.Collections;
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
            if (rowTag == null)
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
            public static bool Prefix(PinnedResourcesPanel __instance, Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> ___rows, MultiToggle ___clearNewButton, MultiToggle ___seeAllButton)
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

                pinnedResourceRowList.Sort((Comparison<PinnedResourcesPanel.PinnedResourceRow>)((a, b) =>
                {
                    if (PickupableToCategoryCache[a.Tag] == PickupableToCategoryCache[b.Tag])
                        return a.SortableNameWithoutLink.CompareTo(b.SortableNameWithoutLink);

                    return PickupableToCategoryCache[a.Tag].ProperNameStripLink().CompareTo(PickupableToCategoryCache[b.Tag].ProperNameStripLink());
                }));  // sort alphabetically, group by category.

                Tag category = null;

                Color categoryColor = lightBlue;

                foreach (Tag clearCategory in PinnedResourcesPanel_CreateRow_Patch.materialHeaders.Keys)
                {
                    PinnedResourcesPanel_CreateRow_Patch.materialHeaders[clearCategory].SetActive(false);
                }

                foreach (var item in pinnedResourceRowList)
                {
                    if (___rows[item.Tag].gameObject.activeSelf) // rows don't get removed between spaced out views, so skip changing the color of inactive ones.
                    {
                        if (PickupableToCategoryCache[item.Tag] != category)
                        {
                            if (PickupableToCategoryCache[item.Tag] != category)
                            {
                                category = PickupableToCategoryCache[item.Tag];

                                // since creation of the category headers is deferred, this might not contain a key yet.
                                if (PinnedResourcesPanel_CreateRow_Patch.materialHeaders.ContainsKey(category))
                                {
                                    PinnedResourcesPanel_CreateRow_Patch.materialHeaders[category].SetActive(true);
                                    PinnedResourcesPanel_CreateRow_Patch.materialHeaders[category].transform.SetAsLastSibling();
                                }
                            }

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

        /// <summary>
        /// Create Row adds a background to each resource entry, and adds new categories as new ones show up.
        /// </summary>
        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("CreateRow")]
        public class PinnedResourcesPanel_CreateRow_Patch 
        {
            public static GameObject resourcesHeader = null;
            public static Dictionary<Tag, GameObject> materialHeaders = new Dictionary<Tag, GameObject>();
            public static void Postfix(PinnedResourcesPanel __instance, Tag tag, 
                ref PinnedResourcesPanel.PinnedResourceRow __result, 
                QuickLayout ___rowContainerLayout, 
                Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> ___rows,
                MultiToggle ___clearNewButton, MultiToggle ___seeAllButton)
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
                    Debug.Log("[GroupResources]: Adding category " + category.ProperNameStripLink());
                    // defer creation until later, since copying the header now causes issues.

                    __instance.StartCoroutine(MakeHeaders(category));
                }

                //GameScheduler.Instance.Schedule("DumpHeaderObjects", 5.0f, Utility.DumpDetails, (object)___rows);

                // Tack on an extra BG, so we can shade that.
                var bg = __result.gameObject.transform.Find("BG").gameObject;
                var additiveBG = Util.KInstantiate(bg, bg, "CategoryBG");
                additiveBG.rectTransform().localScale = new Vector3(1.0f, 1.0f);
                additiveBG.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.0f, bg.rectTransform().rect.width);
                additiveBG.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0.0f, bg.rectTransform().rect.height);

                IEnumerator MakeHeaders(Tag coCategory)
                {
                    yield return null;
                    if (!materialHeaders.ContainsKey(coCategory))
                    {
                        var categoryHeader = Util.KInstantiateUI(resourcesHeader, __instance.rowContainer, true);
                        categoryHeader.name = coCategory.ProperNameStripLink();

                        categoryHeader.rectTransform().localScale = new Vector3(1.0f, 1.0f);

                        categoryHeader.GetComponentInChildren<LocText>().SetText(coCategory.ProperNameStripLink()); // TODO translation?

                        categoryHeader.rectTransform().pivot = new Vector2(1.0f, 1.0f);

                        // TODO adjust internal bits here
                        materialHeaders.Add(coCategory, categoryHeader);
                        //Debug.Log("[GroupResources]: Coroutine: added header " + coCategory.ProperNameStripLink() + ", headers total: " + materialHeaders.Count);
                    }

                    // For some reason, using a reverse patch of SortRows wasn't executing. No idea why. We'll just call it directly.
                    PinnedResourcesPanel_SortRows_Patch.Prefix(__instance, ___rows, ___clearNewButton, ___seeAllButton);
                    ___rowContainerLayout.ForceUpdate();
                    yield return null;
                }
            }
        }
    }
}
