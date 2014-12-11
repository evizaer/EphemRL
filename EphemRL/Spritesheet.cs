using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EphemRL
{
    public static class Spritesheet
    {
        private static Dictionary<string, CroppedBitmap> _Bitmaps;
        private static BitmapSource _SheetImage;
        private static int _SpriteSize;

        public static void Build(string sheetImagePath, string sheetMapPath, int spriteSize)
        {
            _SpriteSize = spriteSize;

            _SheetImage = new BitmapImage(new Uri(Path.GetFullPath(sheetImagePath)));
            _SheetImage.Freeze();

            var sheetWidthInSprites = _SheetImage.PixelWidth/spriteSize;

            _Bitmaps = File.ReadAllLines(sheetMapPath)
                           .Select(line =>
                            {
                                var columns = line.Split('|');
                                return new {Index = Convert.ToInt32(columns[0]), Name = columns[1]};
                            }).ToDictionary(lineCols => lineCols.Name,
                                            lineCols => new CroppedBitmap(_SheetImage,
                                                        new Int32Rect((lineCols.Index%sheetWidthInSprites)*_SpriteSize,
                                                         (lineCols.Index/sheetWidthInSprites)*_SpriteSize, _SpriteSize,
                                                         _SpriteSize)));
        }

        public static CroppedBitmap Get(string spriteName)
        {
            if (_Bitmaps == null) throw new Exception("Spritesheet has not been initialized! Call Spritesheet.Build before trying to get sprites from Spritesheet.");

            return _Bitmaps[spriteName];
        }
    }
}
