using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        [HarmonyPatch(typeof(PinnedResourcesPanel))]
        [HarmonyPatch("SortRows")]
        public class Db_Initialize_Patch
        {
            private static bool Prefix(PinnedResourcesPanel __instance, ref Dictionary<Tag, GameObject> ___rows)
            {
                //Debug.Log("Group Resources!");
                //List<Tag> categories = new List<Tag>();
                //foreach (Tag materialCategory in GameTags.MaterialCategories)
                //    categories.Add(materialCategory);
                //foreach (Tag calorieCategory in GameTags.CalorieCategories)
                //    categories.Add(calorieCategory);
                //foreach (Tag unitCategory in GameTags.UnitCategories)
                //    categories.Add(unitCategory);

                List<KeyValuePair<Tag, Tag>> tagList = new List<KeyValuePair<Tag, Tag>>();
                foreach (KeyValuePair<Tag, GameObject> row in ___rows)
                {
                    var f = row.Value.GetComponent<KPrefabID>();
                    Tag cat = Tag.Invalid;
                    if(GameTags.AllCategories.Contains(row.Key))
                    {

                    }
                    if (f != null)
                        cat = DiscoveredResources.GetCategoryForEntity(f);

                    tagList.Add(new KeyValuePair<Tag, Tag>(row.Key,cat));
                }

                tagList.Sort((Comparison<KeyValuePair<Tag,Tag>>)((a, b) =>
                {
                    int catComp = 0;
                    if (a.Value != null && b.Value != null)
                    {
                        
                        catComp = a.Value.ProperNameStripLink().CompareTo(b.Value.ProperNameStripLink());
                    }
                    if (catComp == 0)
                        return a.Key.ProperNameStripLink().CompareTo(b.Key.ProperNameStripLink());
                    else return catComp;
                    }));


                foreach (KeyValuePair<Tag,Tag> key in tagList)
                    ___rows[key.Key].transform.SetAsLastSibling();
                
                __instance.clearNewButton.transform.SetAsLastSibling();
                __instance.seeAllButton.transform.SetAsLastSibling();

                return false;
            }

            //public static void Postfix()
            //{
            //    //Debug.Log("I execute after Db.Initialize!");
            //}
        }
    }
}
