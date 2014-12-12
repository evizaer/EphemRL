using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using UtiliCS;

namespace EphemRL.Models
{
    public class ManaSource
    {
        private int[] _Capacity;
        private int[] _Mana;

        public ObservableCollection<ManaElement> ManaUnits { get; set; }

        private static readonly int ElementCount = Enum.GetNames(typeof(ManaElement)).Length;

        public ManaSource()
        {
            ManaUnits = new ObservableCollection<ManaElement>();
        }

        public ManaSource(Dictionary<ManaElement, int> capacity) : this()
        {
            _Capacity = ManaDictToArray(capacity);
            _Mana = ManaDictToArray(capacity);

            SetManaUnitsFrom(_Mana);
        }

        public ManaSource(int[] mana) : this()
        {
            _Capacity = mana;
            _Mana = mana;

            SetManaUnitsFrom(mana);
        }

        private void SetManaUnitsFrom(Dictionary<ManaElement, int> d)
        {
            ManaUnits.AddRange(d.SelectMany(kv => kv.Value != 0
                                                ? 0.To(kv.Value).Select(_ => kv.Key)
                								: Enumerable.Empty<ManaElement>()));
        }

        private void SetManaUnitsFrom(IEnumerable<int> a)
        {
            ManaUnits.AddRange(a.SelectMany((manaCount, index) => manaCount > 0 
                                                                ? 0.To(manaCount).Select(_ => (ManaElement)index)
                                         						: Enumerable.Empty<ManaElement>()));
        }

        private static int[] ManaDictToArray(Dictionary<ManaElement, int> d)
        {
            var res = new int[ElementCount];

            foreach (var mana in d)
            {
                res[(int) mana.Key] = mana.Value;
            }

            return res;
        }

        public int GetManaCount(ManaElement element)
        {
            return _Mana[(int)element];
        }

        public void Expend(Dictionary<ManaElement, int> mana)
        {
            foreach (var elementToMana in mana)
            {
                var manaPaid = Expend(elementToMana.Key, elementToMana.Value);

                if (manaPaid != elementToMana.Value) throw new Exception("Tried to consume mana that wasn't present!");
            }
        }

        public int Expend(ManaElement element, int count = 1)
        {
            var originalMana = _Mana[(int)element];
            var newValue = originalMana - count;

            if (newValue <= 0)
            {
                while (ManaUnits.Contains(element)) { ManaUnits.Remove(element); }
                _Mana[(int)element] = 0;
                return originalMana;
            }
            else
            {
                _Mana[(int)element] = newValue;
                count.Times().Do(_ => ManaUnits.Remove(element));
                return count;
            }
        }

        public bool Regenerate()
        {
            foreach (int i in 0.To(_Capacity.Length))
            {
                if (_Mana[i] < _Capacity[i])
                {
                    _Mana[i]++;
                    ManaUnits.Add((ManaElement)i);
                    return true;
                }
            }

            return false;
        }

        public bool HasAny(ManaElement e)
        {
            return _Mana[(int)e] > 0;
        }
    }
}
