﻿using EphemRL.Models;
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

                if (burningTile.FireFuel < .5)
                {
                    var ignitedTile = m.GetAdjacentTiles(burningTile).Where(t => t.Proto.FireResistance < 1 && !t.IsBurning)
                                                                     .OrderBy(t => t.Proto.FireResistance).FirstOrDefault();

                    if (ignitedTile != null) ignitedTile.IsBurning = true;
                }
            }
        }
    }
}
