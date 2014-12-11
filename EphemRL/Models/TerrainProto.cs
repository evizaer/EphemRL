using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EphemRL.Models
{
    [DataContract]
    public class TerrainProto
    {
        public Dictionary<ManaElement, int> ManaCapacity { get; set; } 
        public string Name { get; set; }
        public string SpriteKey { get; set; }
        public bool IsPassable { get; set; }
    }
}
