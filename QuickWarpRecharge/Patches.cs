using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;


namespace QuickWarpRecharge
{
    public class Patches
    {
        public class Mod_OnLoad : KMod.UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                Debug.Log("Quick Recharge, gooooo!");

                harmony.PatchAll();
            }
        }

        [HarmonyPatch(typeof(WarpPortal.WarpPortalSM))]
        [HarmonyPatch("<InitializeStates>b__8_12")]
        public class WarpPortal_Anonymousb__8_12_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Debug.Log("[QuickRecharge]: Attempting to patch b__8_12 ");
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].OperandIs(3000.0))
                    {
                        Debug.Log("[QuickRecharge]: Found ldc_r4 3000 at " + i);
                        
                        codes[i].operand = 30.0f;
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }

    }
}
