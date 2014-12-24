using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemRL.Models
{
    public class SpellTargetCriteria
    {
        public TerrainTargetCriteria TerrainCriteria { get; set; }
        public SpellTarget Target { get; set; }

        public SpellTargetCriteria()
        {
            TerrainCriteria = new TerrainTargetCriteria();
        }

        public bool AreSatisfiedBy(MapTile t, Actor a)
        {
            return Target == SpellTarget.Self || TerrainCriteria.AreSatisfiedBy(t, a);
        }
    }

    public class TerrainTargetCriteria
    {
        public bool? IsPassable { get; set; }
        public bool? IsVisible { get; set; }
        public bool? HasActor { get; set; }

        public TerrainTargetCriteria()
        {
            IsPassable = IsVisible = HasActor = null;
        }

        public bool AreSatisfiedBy(MapTile t, Actor a)
        {
            return (!IsPassable.HasValue || IsPassable.Value == t.Proto.IsPassable)
                && (!IsVisible.HasValue || IsVisible.Value == a.VisibleTiles.Contains(t))
                && (!HasActor.HasValue || HasActor.Value == (t.Actor != null));
        }
    }
}
