using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemRL.Models
{
    public class ManaDelta
    {
        public Dictionary<MapTile, Dictionary<ManaElement, int>> Delta { get; private set; }

        public ManaDelta()
        {
            Delta = new Dictionary<MapTile, Dictionary<ManaElement, int>>();
        }

        public void Add(MapTile tile, ManaElement element, int manaCount)
        {
            Dictionary<ManaElement, int> manaDict;
            if (!Delta.TryGetValue(tile, out manaDict))
            {
                manaDict = new Dictionary<ManaElement, int>();
                Delta.Add(tile, manaDict);
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

        public int GetTileDeltaForElement(MapTile t, ManaElement e)
        {
            if (Delta.ContainsKey(t) && Delta[t].ContainsKey(e))
            {
                return Delta[t][e];
            }
            else
            {
                return 0;
            }
        }

        public ManaDelta Merge(ManaDelta other)
        {
            if (other == null) return null;

            var result = new ManaDelta();

            foreach (var tileGroup in Delta.Concat(other.Delta).GroupBy(kv => kv.Key))
            {
                if (tileGroup.Count() == 1) 
                {
                    result.Delta.Add(tileGroup.Key, tileGroup.Single().Value);
                }
                else 
                {
                    var tile = tileGroup.Key;
                    foreach (var tileMana in tileGroup.Select(kv => kv.Value)) 
                    {
                        foreach (var elementMana in tileMana)
                        {
                            var element = elementMana.Key;
                            var manaCount = elementMana.Value;

                            result.Add(tile, element, manaCount);

                            // If the delta indicates there should be more mana consumed from a tile than exist, 
                            // the delta is invalid.
                            if (result.GetTileDeltaForElement(tile, element) > tile.Mana.GetManaCount(element))
                            {
                                return null;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
