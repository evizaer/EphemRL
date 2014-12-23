using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtiliCS;

namespace EphemRL.Models
{
    public class SpellCastDelta
    {

        public Actor Caster { get; set; }

        public SpellProto Spell { get; set; }

        public MapTile TargetTile { get; set; }

        public bool IsCastable { get; set; }

        public string Hotkey { get; set; }

        public ManaDelta Mana { get; set; }

        public SpellCastDelta()
        {
        }
    }
}
