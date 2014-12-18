using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtiliCS;

namespace EphemRL.Models
{
    public class ManaClock : ViewModel
    {
        public ObservableCollection<ManaElement> Order { get; set; }

        public ManaClock(IEnumerable<ManaElement> seedElements)
        {
            Order = new ObservableCollection<ManaElement>(seedElements.ToList().Shuffle());
        }

        /// <summary>
        /// Return the current element and advance the clock.
        /// </summary>
        public ManaElement Tick()
        {
            var e = Order[0];

            Order.RemoveAt(0);

            Order.Add(e);

            return e;
        }
    }
}
