using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using UtiliCS;

namespace EphemRL.Models
{
    public class Actor : ViewModel
    {
        private int _Health;
        public int Health
        {
            get { return _Health; } 
            set { if (_Health != value) { _Health = value; NotifyPropertyChanged(); NotifyPropertyChanged("HealthPct"); } } 
        }

        private int _MaxHealth;
        public int MaxHealth
        {
            get { return _MaxHealth; } 
            set { if (_MaxHealth != value) { _MaxHealth = value; NotifyPropertyChanged(); } } 
        }

        public ActorProto Proto { get; private set; }

        public double HealthPct { get { return (double) Health / Proto.MaxHealth; }}

        public CroppedBitmap Sprite { get; set; }

        public List<SpellProto> Spells { get; set; }

        public Actor(ActorProto proto, IEnumerable<SpellProto> allSpells)
        {
            Proto = proto;

            Health = MaxHealth = proto.MaxHealth;

            Sprite = Spritesheet.Get(proto.SpriteKey);

            Spells = allSpells.Where(s => proto.Spells.Contains(s.Name)).ToList();
        }
    }
}
