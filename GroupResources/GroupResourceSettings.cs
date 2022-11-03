using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PeterHan.PLib.Options;
using UnityEngine;

namespace GroupResources
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("https://github.com/anpilley/OniModSet")]
    public class GroupResourceSettings
    {
        [Option("Category Headers", "Enable/Disable creation of collapsible Category Headers. Pinned items will still be sorted by Category and have background shading.", "Category Headers")]
        [JsonProperty]
        public bool CategoryHeaders { get; set; }

        [Option("Dark Color", "The color for dark-shaded pinned/new items.", "Coloring")]
        [JsonProperty]
        public Color32 DarkColor { get; set; }


        [Option("Light Color", "The color for light-shaded pinned/new items.", "Coloring")]
        [JsonProperty]
        public Color32 LightColor { get; set; }

        [Option("Alpha", "Alpha transparency of the background shading of Resource entries, 0.0 = fully transparent, 1.0 = fully opaque.", "Coloring")]
        [Limit(0.0, 1.0)]
        [JsonProperty]
        public float Alpha { get; set; }

        public GroupResourceSettings()
        {
            CategoryHeaders = true;

            LightColor = new Color32(240, 134, 80, 128);
            DarkColor = new Color32(61, 147, 240, 128);

            Alpha = 0.5f;
        }
    }
}
