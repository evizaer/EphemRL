using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EphemRL.Models
{
    public class ActorProto
    {
        public string Name { get; set; }
        public string SpriteKey { get; set; }
        public int MaxHealth { get; set; }
        public List<string> Spells { get; set; }
        public int SightRange { get; set; }

        public ActorProto()
        {
            Spells = new List<string>();
        }
    }
}
