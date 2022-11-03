﻿// Copyright © 2022 Andrew Pilley
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using PeterHan.PLib.Options;
using static GroupResources.Patches;

namespace GroupResources
{
    public class CategoryManager : KMonoBehaviour
    {
        private GameObject resourcesHeader = null;
        public Dictionary<Tag, GameObject> materialHeaders;

        public CategoryManager()
        {
            PickupableToCategoryCache = new Dictionary<Tag, Tag>();
            materialHeaders = new Dictionary<Tag, GameObject>();
        }

        private ResourceHeaderState headerState;

        /// <summary>
        /// cache of the pickupables to category tags, since the lookup is expensive.
        /// </summary>
        private Dictionary<Tag, Tag> PickupableToCategoryCache;

        /// <summary>
        /// Add category to the pickupable -> tag cache if it's not already in there.
        /// </summary>
        /// <param name="rowTag">The resource to lookup and add.</param>
        public void AddCategoryToCache(Tag rowTag)
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

        /// <summary>
        /// Initialize/cleanup between save/load. Get the SaveGame object to retrieve save data.
        /// </summary>
        public void Init()
        {
            if (!resourcesHeader)
                resourcesHeader = null;

            if (materialHeaders != null && materialHeaders.Count > 0)
            {
                materialHeaders.Clear();
            }

            List<GameObject> rootGOs = new List<GameObject>();
            this.gameObject.scene.GetRootGameObjects(rootGOs);
            foreach(var go in rootGOs)
            {
                if(go.name == "SaveGame")
                {
                    this.headerState = go.GetComponent<ResourceHeaderState>();
                    if(this.headerState == null)
                    {
                        Debug.LogError("[Group Resources]: Unable to find Header State");
                    }
                    break;
                }
            }

            if (this.headerState == null)
            {
                Debug.LogError("[Group Resources]: Unable to find SaveGame object?");
            }
        }

        public bool SortRows(Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> rows)
        {
            var resourcesPanelGO = this.gameObject.GetComponent<PinnedResourcesPanel>();

            foreach (Tag rowTag in rows.Keys)
            {
                // look up in existing tag->category lookup cache
                if (!PickupableToCategoryCache.ContainsKey(rowTag))
                {
                    // Add it if it's not already there.
                    AddCategoryToCache(rowTag);
                }
            }

            int currWorldId = ClusterManager.Instance.activeWorldId;

            WorldInventory worldInventory = ClusterManager.Instance.GetWorld(currWorldId).worldInventory;


            // "pinnedResourceRowList" actually includes items marked "new" as well, btw.
            List<PinnedResourcesPanel.PinnedResourceRow> pinnedResourceRowList = new List<PinnedResourcesPanel.PinnedResourceRow>();
            foreach (KeyValuePair<Tag, PinnedResourcesPanel.PinnedResourceRow> row in rows)
                pinnedResourceRowList.Add(row.Value);

            // sort alphabetically, group by category.
            pinnedResourceRowList.Sort((Comparison<PinnedResourcesPanel.PinnedResourceRow>)((a, b) =>
            {
                if (PickupableToCategoryCache[a.Tag] == PickupableToCategoryCache[b.Tag])
                    return a.SortableNameWithoutLink.CompareTo(b.SortableNameWithoutLink);

                return PickupableToCategoryCache[a.Tag].ProperNameStripLink().CompareTo(PickupableToCategoryCache[b.Tag].ProperNameStripLink());
            }));

            Tag category = null;

            Color categoryColor = GroupResourceSettings.Instance.DarkColor;
            bool categoryCollapsed = false;


            // hide all of the material headers by default, we'll enable them based on what items are pinned/new later.
            foreach (Tag clearCategory in materialHeaders.Keys)
            {
                try
                {
                    materialHeaders[clearCategory].SetActive(false);
                }
                catch (NullReferenceException)
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
                    if (materialHeaders.ContainsKey(itemCategoryTag))
                    {
                        materialHeaders[itemCategoryTag].transform.SetAsLastSibling();
                        if (headerState != null)
                        {
                            var headerTag = new ResourceHeaderState.AsteroidTagKey(currWorldId, itemCategoryTag.ProperNameStripLink());
                            var headerButton = materialHeaders[itemCategoryTag].transform.Find("Header").GetComponent<MultiToggle>();
                            if (headerState.HeaderState.ContainsKey(headerTag))
                            {

                                headerButton.ChangeState(headerState.HeaderState[headerTag]);
                            }
                            else
                            {
                                headerState.HeaderState.Add(headerTag, 0);
                                headerButton.ChangeState(0);
                            }
                        }
                    }

                }
                rows[item.Tag].gameObject.transform.SetAsLastSibling();
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

                    if (categoryColor == GroupResourceSettings.Instance.DarkColor)
                    {
                        categoryColor = GroupResourceSettings.Instance.LightColor;
                        categoryCollapsed = false; // since this is a new category, reset the collapsed state
                    }
                    else
                    {
                        categoryColor = GroupResourceSettings.Instance.DarkColor;
                        categoryCollapsed = false; // since this is a new category, reset the collapsed state
                    }

                    // Since creation of the category headers is deferred, this might not contain a key yet.
                    if (materialHeaders.ContainsKey(itemCategoryTag))
                    {
                        // show headers for things we might show (showRowOnThisWorld), and work out if we need to collapse items.
                        try
                        {
                            materialHeaders[category].SetActive(true);
                            var headerButton = materialHeaders[category].transform.Find("Header").GetComponent<MultiToggle>();
                            if (headerButton.CurrentState == 0)
                            {
                                categoryCollapsed = true;
                            }
                        }
                        catch (NullReferenceException)
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
                            rows[item.Tag].gameObject.SetActive(false);
                        }
                        else
                        {
                            rows[item.Tag].gameObject.SetActive(true);
                        }
                    }
                    catch (NullReferenceException)
                    {
                        Debug.LogError("[GroupResources]: Null Reference showing/hiding row items from header state");
                    }

                    var categoryBG = rows[item.Tag].gameObject.transform.Find("BG/CategoryBG").gameObject;
                    var img = categoryBG.GetComponent<Image>();

                    img.color = categoryColor;
                    img.SetAlpha(GroupResourceSettings.Instance.Alpha);
                }
                else
                {
                    try
                    {
                        rows[item.Tag].gameObject.SetActive(false);
                    }
                    catch (NullReferenceException)
                    {
                        Debug.LogError("[GroupResources]: Null Reference hiding row items from world state");
                    }
                }
            }

            resourcesPanelGO.clearNewButton.transform.SetAsLastSibling();
            resourcesPanelGO.seeAllButton.transform.SetAsLastSibling();

            return false;
        }

        /// <summary>
        /// Create a background for new category rows, and a category header.
        /// </summary>
        /// <param name="result">the newly created row.</param>
        /// <param name="itemTag"></param>
        /// <param name="rowContainerLayout"></param>
        /// <param name="rows"></param>
        public void CreateCategoryRow(
            ref PinnedResourcesPanel.PinnedResourceRow result,
            Tag itemTag,
            QuickLayout rowContainerLayout,
            Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> rows)
        {
            var pinnedResourcesPanel = this.FindComponent<PinnedResourcesPanel>();

            // look up in existing tag->category lookup cache
            if (!PickupableToCategoryCache.ContainsKey(itemTag))
            {
                // Add it if it's not already there.
                AddCategoryToCache(itemTag);
            }

            if (headerState == null)
            {
                Debug.LogError("[Group Resources]: headerState uninitialized?");
            }

            var category = PickupableToCategoryCache[itemTag];

            // The parent to row we've just created should be the EntryContainer, parent to that is the Resource header.
            if (!resourcesHeader)
            {
                resourcesHeader = result.gameObject.transform.parent.parent.Find("HeaderLayout").gameObject;
            }

            if (!materialHeaders.ContainsKey(category) && GroupResourceSettings.Instance.CategoryHeaders)

            {
                // defer creation until later, since copying the header now causes issues.
                if (this.gameObject.activeInHierarchy)
                    this.StartCoroutine(MakeHeaders(category));
            }

            // Tack on an extra BG to regular resource entries, so we can shade that.
            var bg = result.gameObject.transform.Find("BG").gameObject;
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
                    var categoryHeader = Util.KInstantiateUI(resourcesHeader, pinnedResourcesPanel.rowContainer, true);
                    categoryHeader.name = coCategory.ProperNameStripLink();

                    // For some reason, the rows pivot off of the end, not the middle.
                    // And all ONI UI objects seem to come out with a scaling of 0.9 for no discernable reason.
                    categoryHeader.rectTransform().localScale = new Vector3(1.0f, 1.0f);
                    categoryHeader.GetComponentInChildren<LocText>().SetText(coCategory.ProperNameStripLink()); // TODO translation?
                    categoryHeader.rectTransform().pivot = new Vector2(1.0f, 1.0f);

                    // remove the "clear all" button, it won't do anything useful here.
                    var clbutton = categoryHeader.transform.Find("ClearAllButton");
                    if (clbutton != null)
                    {
                        clbutton.SetParent(null, false);
                    }

                    // Add the click handler, and set the default state to collapsed.
                    var headerButton = categoryHeader.transform.Find("Header");

                    MultiToggle headerToggle = headerButton.GetComponent<MultiToggle>();
                    if (headerState != null && headerState.HeaderState != null)
                    {
                        var targetKey = new ResourceHeaderState.AsteroidTagKey(ClusterManager.Instance.activeWorldId, coCategory.ProperNameStripLink());

                        if (headerState.HeaderState.ContainsKey(targetKey))
                        {
                            headerToggle.ChangeState(headerState.HeaderState[targetKey], true);
                        }
                        else
                        {
                            headerToggle.ChangeState(0);
                        }
                    }

                    headerToggle.onClick = () =>
                    {
                        int worldId = ClusterManager.Instance.activeWorldId;
                        int newState = (headerToggle.CurrentState + 1) % 2;
                        headerToggle.ChangeState(newState);

                        if (headerState != null && headerState.HeaderState != null)
                        {
                            var targetKey = new ResourceHeaderState.AsteroidTagKey(worldId, coCategory.ProperNameStripLink());

                            if (headerState.HeaderState.ContainsKey(targetKey))
                            {
                                headerState.HeaderState[targetKey] = newState;
                            }
                            else
                            {
                                headerState.HeaderState.Add(targetKey, newState);
                            }
                        }


                        // Kick off a row sort to hide the collapsed state
                        this.SortRows(rows);
                        pinnedResourcesPanel.Refresh();
                    };

                    // TODO adjust internal bits here
                    materialHeaders.Add(coCategory, categoryHeader);
                }

                // For some reason, using a reverse patch of SortRows wasn't executing. No idea why, possibly because we patched it already.
                // We'll just call it directly, since ours completely overwrites theirs.
                this.SortRows(rows);

                // if we don't force an update here, the header shows up in the wrong position for quite some time after the header is drawn
                rowContainerLayout.ForceUpdate();
                yield return null;
            }
        }
    }
}
