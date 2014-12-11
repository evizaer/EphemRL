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
            set { if (_Health != value) { _Health = value; NotifyPropertyChanged(); } } 
        }

        public int MaxHealth { get; set; }

        public double HealthPct { get { return (double) Health/MaxHealth; }}

        public CroppedBitmap Sprite { get; set; }

        public List<SpellProto> Spells { get; set; }

        public Actor()
        {
            Health = MaxHealth = 10;
            Sprite = Spritesheet.Get("Player");

            Spells = new List<SpellProto>();
        }
    }
}
