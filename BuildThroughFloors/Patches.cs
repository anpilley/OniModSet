using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;


namespace BuildThroughFloors
{
    public class Patches
    {
        public class Mod_OnLoad : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                Debug.Log("Building Through Floors, gooooo!");

                harmony.PatchAll();
            }
        }


        [HarmonyPatch(typeof(Constructable))]
        [HarmonyPatch("OnSpawn")]
        public class Constructable_OnSpawn_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool foundnull = false;
                int startindex = -1;
                int endindex = -1;

                var codes = new List<CodeInstruction>(instructions);
                for(var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldnull)
                    {
                        if(foundnull)
                        {
                            Debug.Log("[Build Through Floors]: Found a second ldnull at " + i);
                            endindex = i;
                            break;
                        }
                        else
                        {
                            foundnull = true;
                            continue;
                        }
                    }

                    if(foundnull && codes[i].opcode == OpCodes.Brfalse_S)
                    {
                        startindex = i;
                        Debug.Log("[Build through Floors]: Found brfalse_s at " + i);
                        codes.Insert(startindex, new CodeInstruction(OpCodes.Pop));
                        codes[i+1].opcode = OpCodes.Br_S;
                        
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }
    }
}
