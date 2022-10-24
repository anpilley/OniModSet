using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static GroupResources.Patches;

namespace GroupResources
{
    internal class Utility
    {
        public static void DumpDetails(object rows)
        {
            Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow> ___rows = rows as Dictionary<Tag, PinnedResourcesPanel.PinnedResourceRow>;

            Debug.Log("[Group Resources]: Dumping materialHeader details.");
            if (PinnedResourcesPanel_CreateRow_Patch.materialHeaders != null)
            {
                foreach (var categoryHeader in PinnedResourcesPanel_CreateRow_Patch.materialHeaders.Values)
                {
                    GroupResources.Utility.PrintDetails(categoryHeader, 0);
                }

                if (___rows != null)
                {
                    Debug.Log("[Group Resources]: Dumping regular row details.");
                    foreach (var rowBits in ___rows.Values)
                    {
                        GroupResources.Utility.PrintDetails(rowBits.gameObject, 0);
                    }



                }
            }
        }

        public static void DumpQuickLayout(object layout)
        {
            Debug.Log("[Group Resources]: Dumping Quicklayout Child list in index order.");
            QuickLayout _layout = layout as QuickLayout;
            if(_layout != null)
            {
                for(int index = 0; index < _layout.transform.childCount; ++index)
                {
                    var child = _layout.transform.GetChild(index);
                    Debug.Log("Group Resources]: (" + index + ") Child: " + child.gameObject.name);
                }
            }
        }


        public static void PrintDetails(GameObject target, int level)
        {
            Debug.Log("Level: " + level + ", " + target.name);
            Debug.Log("[Group Resources]: position: (" + target.rectTransform().position.x + "," + target.rectTransform().position.y + ")");
            Debug.Log("[Group Resources]: anchor: (" + target.rectTransform().anchoredPosition.x + "," + target.rectTransform().anchoredPosition.y + ")");
            Debug.Log("[Group Resources]: pivot: (" + target.rectTransform().pivot.x + "," + target.rectTransform().pivot.y + ")");
            Debug.Log("[Group Resources]: anchormax: (" + target.rectTransform().anchorMax.x + "," + target.rectTransform().anchorMax.y + ")");
            Debug.Log("[Group Resources]: anchormin: (" + target.rectTransform().anchorMin.x + "," + target.rectTransform().anchorMin.y + ")");

            foreach (object component in target.GetComponents(typeof(object)))
            {
                Debug.Log("[Group Resources]: Components: " + component.GetType().Name);
            }


            for (int index = 0; index < target.transform.childCount; index++)
            {
                PrintDetails(target.transform.GetChild(index).gameObject, level + 1);
            }
        }
    }
}
