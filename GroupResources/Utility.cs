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
