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


using KSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GroupResources
{
    /// <summary>
    /// Resource Header state tracking component, saves/loads header state, and tracks across 
    /// different worlds. Attaches to SaveGame
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public class ResourceHeaderState : KMonoBehaviour, ISaveLoadable
    {
        /// <summary>
        /// Combination of world id and category tag name.
        /// </summary>
        public class AsteroidTagKey : IComparable<AsteroidTagKey>
        {
            public AsteroidTagKey(int worldId, string tagName)
            {
                WorldId = worldId;
                TagName = tagName;
            }

            public string TagName;
            public int WorldId;

            public int CompareTo(AsteroidTagKey other)
            {
                if (other == null)
                    return 1;

                Debug.Log("[Group Resources]: Comparing {" + this.ToString() + "} to {" + other.ToString()+"}");

                if (this.WorldId.CompareTo(other.WorldId) != 0)
                    return this.WorldId.CompareTo(other.WorldId);
                return this.TagName.CompareTo(other.TagName);
            }

            public override string ToString()
            {
                return "Asteroid Tag Key: " + WorldId + ", " + TagName;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is AsteroidTagKey))
                    return false;

                return this.CompareTo(obj as AsteroidTagKey) == 0;
            }


            public override int GetHashCode()
            {
                return ((string)("" + this.WorldId + this.TagName)).GetHashCode();
            }

        }

        /// <summary>
        /// Comparison tool for Dictionary for asteroid tags.
        /// </summary>
        public class AsteroidTagKeyEqualizer : IEqualityComparer<AsteroidTagKey>
        {

            public bool Equals(AsteroidTagKey x, AsteroidTagKey y)
            {
                if (x == null || y == null)
                    return false;

                return x.CompareTo(y) == 0;
            }

            public int GetHashCode(AsteroidTagKey obj)
            {
                return ((string)("" + obj.WorldId + obj.TagName)).GetHashCode();
            }
        }

        /// <summary>
        /// Saves the asteroid world id, and the category tag name, and state.
        /// </summary>
        [Serialize]
        public Dictionary<AsteroidTagKey, int> HeaderState = new Dictionary<AsteroidTagKey, int>(new AsteroidTagKeyEqualizer());
    }
}
