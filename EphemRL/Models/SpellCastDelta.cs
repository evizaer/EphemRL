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
        public Dictionary<MapTile, Dictionary<ManaElement, int>> ManaDelta { get; set; }

        public Actor Caster { get; set; }

        public SpellProto Spell { get; set; }

        public MapTile TargetTile { get; set; }

        public bool IsCastable { get; set; }

        public string Hotkey { get; set; }

        public SpellCastDelta()
        {
            ManaDelta = new Dictionary<MapTile, Dictionary<ManaElement, int>>();
        }

        public void AddToManaDelta(MapTile tile, ManaElement element, int manaCount)
        {
            Dictionary<ManaElement, int> manaDict;
            if (!ManaDelta.TryGetValue(tile, out manaDict))
            {
                manaDict = new Dictionary<ManaElement, int>();
                ManaDelta.Add(tile, manaDict);
            }

            if (manaDict.ContainsKey(element))
            {
                manaDict[element] += manaCount;
            }
            else
            {
                manaDict.Add(element, manaCount);
            }
        }

    }
}
