using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using UtiliCS;

namespace EphemRL.Models
{
    public class MapTile : ViewModel
    {
        public const int TileSize = 32;
        public int X { get; set; }
        public int Y { get; set; }

        public int PixelX { get { return X*TileSize; }}
        public int PixelY { get { return Y*TileSize; }}

        public string ToolTip { get { return string.Format("{0} at ({1}, {2})", Proto.Name, X, Y); } }
        public CroppedBitmap Sprite { get; set; }

        public TerrainProto Proto { get; set; }

        public ManaSource Mana { get; set; }

        private bool _IsSelectable;
        public bool IsSelectable
        {
            get { return _IsSelectable; }
            set { if (value != _IsSelectable) { _IsSelectable = value; NotifyPropertyChanged(); } }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { if (value != _IsSelected) { _IsSelected = value; NotifyPropertyChanged(); } }
        }

        private Actor _Actor;
        public Actor Actor
        {
            get { return _Actor; } 
            set { if (_Actor != value) { _Actor = value; NotifyPropertyChanged(); } } 
        }

        public MapTile(int x, int y, TerrainProto terrain)
        {
            X = x;
            Y = y;

            Proto = terrain;
            Sprite = Spritesheet.Get(terrain.SpriteKey);
            IsSelectable = false;

            Mana = new ManaSource(Proto.ManaCapacity);
        }
    }
}
