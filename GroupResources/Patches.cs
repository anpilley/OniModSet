using HarmonyLib;
using rail;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using Unity.Profiling.LowLevel.Unsafe;
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
        [HarmonyPatch("OnPrefabInit")]
        public class PinnedResourcesPanel_OnPrefabInit_Patch
        {
            /// <summary>
            /// Runs some quick cleanup routines under the assumption that we might be reloading a save from main menu.
            /// </summary>
            public static void Prefix()
            {
                if (!PinnedResourcesPanel_CreateRow_Patch.resourcesHeader)
                    PinnedResourcesPanel_CreateRow_Patch.resourcesHeader = null;
                if(PinnedResourcesPanel_CreateRow_Patch.materialHeaders != null && PinnedResourcesPanel_CreateRow_Patch.materialHeaders.Count > 0)
                {
                    PinnedResourcesPanel_CreateRow_Patch.materialHeaders.Clear();
                }
            }
        }


        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("SortRows")]
        public class PinnedResourcesPanel_SortRows_Patch
        {
            private static readonly Color lightBlue = new Color(0.013f, 0.78f, 1.0f);

            /// <summary>
            /// Sort the pinned resources list by category, name. Hide/unhide based on category collapse.
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

                WorldInventory worldInventory = ClusterManager.Instance.GetWorld(ClusterManager.Instance.activeWorldId).worldInventory;


                // "pinnedResourceRowList" actually includes items marked "new" as well, btw.
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
                bool categoryCollapsed = false;


                // hide all of the material headers by default, we'll enable them based on what items are pinned/new later.
                foreach (Tag clearCategory in PinnedResourcesPanel_CreateRow_Patch.materialHeaders.Keys)
                {
                    try
                    {
                        PinnedResourcesPanel_CreateRow_Patch.materialHeaders[clearCategory].SetActive(false);
                    }
                    catch(NullReferenceException)
                    {
                        Debug.LogError("[GroupResources]: Null Reference hiding material headers");
                    }
                }

                // Sort all of the material headers and rows.
                foreach (var item in pinnedResourceRowList)
                {
                    Tag itemCategoryTag = PickupableToCategoryCache[item.Tag];


                    if (itemCategoryTag != category)
                    {
                        category = PickupableToCategoryCache[item.Tag];

                        // headers might not have been added yet, since they get deferred.
                        if (PinnedResourcesPanel_CreateRow_Patch.materialHeaders.ContainsKey(itemCategoryTag))
                        {
                            PinnedResourcesPanel_CreateRow_Patch.materialHeaders[itemCategoryTag].transform.SetAsLastSibling();
                        }

                    }
                    ___rows[item.Tag].gameObject.transform.SetAsLastSibling();
                }

                // Set visibility on material headers and rows based on the current world and the 
                // state of the material header (collapsed/expanded)
                category = null;

                foreach (var item in pinnedResourceRowList)
                {
                    Tag itemCategoryTag = PickupableToCategoryCache[item.Tag];

                    bool showRowOnThisWorld = worldInventory.pinnedResources.Contains(item.Tag) ||
                        worldInventory.notifyResources.Contains(item.Tag) ||
                        (DiscoveredResources.Instance.newDiscoveries.ContainsKey(item.Tag) && ((double)worldInventory.GetAmount(item.Tag, false) > 0.0));

                    // If it's in the pinned, notify(?) or new list, and we haven't looked at this category yet.
                    if (showRowOnThisWorld && itemCategoryTag != category)
                    {
                        category = itemCategoryTag;

                        if (categoryColor == lightBlue)
                        {
                            categoryColor = Color.white;
                            categoryCollapsed = false; // since this is a new category, reset the collapsed state
                        }
                        else
                        {
                            categoryColor = lightBlue;
                            categoryCollapsed = false; // since this is a new category, reset the collapsed state
                        }

                        // Since creation of the category headers is deferred, this might not contain a key yet.
                        if (PinnedResourcesPanel_CreateRow_Patch.materialHeaders.ContainsKey(itemCategoryTag))
                        {
                            // show headers for things we might show (showRowOnThisWorld), and work out if we need to collapse items.
                            try
                            {


                                PinnedResourcesPanel_CreateRow_Patch.materialHeaders[category].SetActive(true);
                                var headerButton = PinnedResourcesPanel_CreateRow_Patch.materialHeaders[category].transform.Find("Header").GetComponent<MultiToggle>();
                                if (headerButton.CurrentState == 0)
                                {
                                    categoryCollapsed = true;
                                }
                            }
                            catch(NullReferenceException)
                            {
                                Debug.LogError("[GroupResources]: Null Reference showing material headers");
                            }
                        }
                    }


                    // show rows if it's pinned/new and in an expanded category, set the background color.
                    if (showRowOnThisWorld) 
                    {
                        try
                        {

                            if (categoryCollapsed == true)
                            {
                                ___rows[item.Tag].gameObject.SetActive(false);
                            }
                            else
                            {

                                ___rows[item.Tag].gameObject.SetActive(true);
                            }
                        }
                        catch (NullReferenceException)
                        {
                            Debug.LogError("[GroupResources]: Null Reference showing/hiding row items from header state");

                        }

                        var categoryBG = ___rows[item.Tag].gameObject.transform.Find("BG/CategoryBG").gameObject;
                        var img = categoryBG.GetComponent<Image>();

                        img.color = categoryColor;
                        img.SetAlpha(0.5f);
                    }
                    else
                    {
                        try
                        {
                            ___rows[item.Tag].gameObject.SetActive(false);
                        }
                        catch (NullReferenceException)
                        {
                            Debug.LogError("[GroupResources]: Null Reference hiding row items from world state");
                        }
                    }
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
            /// <summary>
            /// Cache of the original resources header we dupe.
            /// </summary>
            public static GameObject resourcesHeader = null;

            /// <summary>
            /// All of the resource headers we've added, by tag.
            /// </summary>
            public static Dictionary<Tag, GameObject> materialHeaders = new Dictionary<Tag, GameObject>();

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

                // The parent to row we've just created should be the EntryContainer, parent to that is the Resource header.
                if (!resourcesHeader)
                {
                    resourcesHeader = __result.gameObject.transform.parent.parent.Find("HeaderLayout").gameObject;
                }

                if (!materialHeaders.ContainsKey(category))
                {
                    // defer creation until later, since copying the header now causes issues.
                    __instance.StartCoroutine(MakeHeaders(category));
                }

                // Tack on an extra BG to regular resource entries, so we can shade that.
                var bg = __result.gameObject.transform.Find("BG").gameObject;
                var additiveBG = Util.KInstantiate(bg, bg, "CategoryBG");
                additiveBG.rectTransform().localScale = new Vector3(1.0f, 1.0f);
                additiveBG.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.0f, bg.rectTransform().rect.width);
                additiveBG.rectTransform().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0.0f, bg.rectTransform().rect.height);

                // Coroutine to add a material header item, deferred by one frame.
                IEnumerator MakeHeaders(Tag coCategory)
                {
                    // wait a frame or so.
                    yield return null;
                    if (!materialHeaders.ContainsKey(coCategory))
                    {
                        var categoryHeader = Util.KInstantiateUI(resourcesHeader, __instance.rowContainer, true);
                        categoryHeader.name = coCategory.ProperNameStripLink();

                        // For some reason, the rows pivot off of the end, not the middle.
                        // And all ONI UI objects seem to come out with a scaling of 0.9 for no discernable reason.
                        categoryHeader.rectTransform().localScale = new Vector3(1.0f, 1.0f);
                        categoryHeader.GetComponentInChildren<LocText>().SetText(coCategory.ProperNameStripLink()); // TODO translation?
                        categoryHeader.rectTransform().pivot = new Vector2(1.0f, 1.0f);

                        // remove the "clear all" button, it won't do anything useful here.
                        var clbutton = categoryHeader.transform.Find("ClearAllButton");
                        if(clbutton != null)
                        {
                            clbutton.SetParent(null, false);
                        }

                        // Add the click handler, and set the default state to collapsed.
                        var headerButton = categoryHeader.transform.Find("Header");

                        MultiToggle headerToggle = headerButton.GetComponent<MultiToggle>();
                        headerToggle.ChangeState(0); // TODO: save/reload this state.
                        headerToggle.onClick = () =>
                        {
                            headerToggle.ChangeState((headerToggle.CurrentState+1)%2);

                            // Kick off a row sort to hide the collapsed state
                            PinnedResourcesPanel_SortRows_Patch.Prefix(__instance, ___rows, ___clearNewButton, ___seeAllButton);
                        };

                        // TODO adjust internal bits here
                        materialHeaders.Add(coCategory, categoryHeader);
                    }

                    // For some reason, using a reverse patch of SortRows wasn't executing. No idea why, possibly because we patched it already.
                    // We'll just call it directly, since ours completely overwrites theirs.
                    PinnedResourcesPanel_SortRows_Patch.Prefix(__instance, ___rows, ___clearNewButton, ___seeAllButton);

                    // if we don't force an update here, the header shows up in the wrong position for quite some time after the header is drawn
                    ___rowContainerLayout.ForceUpdate();
                    yield return null;
                }
            }
        }
    }
}
