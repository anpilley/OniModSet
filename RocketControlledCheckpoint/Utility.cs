using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocketControlledCheckpoint
{
    internal class Utility
    {
        public static void DebugLog(string str)
        {
            Debug.Log("[ImprovedRocketControls]: " + str);
        }

        public static void ErrorLog(string str)
        {
            Debug.LogError("[ImprovedRocketControls]: " + str);
        }
    }
}
