using EphemRL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtiliCS;

namespace EphemRL.Helpers
{
    public class FireSimulator
    {
        public static void Tick(Map m) 
        {
            foreach (var burningTile in m.Tiles.Where(t => t.IsBurning).ToList())
            {
                burningTile.FireFuel -= burningTile.Proto.BurnRate;

                if (burningTile.FireFuel < 0)
                {
                    burningTile.IsBurning = false;
                    m.ChangeTerrainOf(burningTile, "Ash");
                }

                if (burningTile.Actor != null)
                {
                    burningTile.Actor.Health -= 1;
                }
            }
        }
    }
}
