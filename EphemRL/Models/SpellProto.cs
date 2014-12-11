using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemRL.Models
{
    public enum SpellTarget { Self, Terrain, Actor }

    /// <summary>
    /// What tiles can be used for mana. If "Mana" Dict is populated, this tile is
    /// required to have at least as many mana as specified in "Mana".
    /// </summary>
    public class TileManaRequirement
    {
        public int DX { get; set; }
        public int DY { get; set; }

        public SpellTarget Target { get; set; }

        public Dictionary<ManaElement, int> Mana { get; set; }
    }

    public class SpellProto
    {
        public List<TileManaRequirement> TileManaRequired { get; set; }
        public Dictionary<ManaElement, int> AggregateManaRequired { get; set; }
        public string Name { get; set; }
        public int Range { get; set; }
        public SpellTarget Target { get; set; }

        public SpellProto()
        {
            AggregateManaRequired = new Dictionary<ManaElement, int>();
        }
    }
}
