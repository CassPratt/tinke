using System;
using System.Drawing;
using System.IO;
using Ekona.Images;

namespace Images
{
    public class PCXImage : ImageBase
    {
        private Bitmap bitmap;
        private ushort width;
        private ushort height;

        public PCXImage(string path, uint id, ushort width, ushort height) 
            : base(path, id)
        {
            this.width = width;
            this.height = height;
            this.bitmap = new Bitmap(width, height);
        }

        public PCXImage(Bitmap bmp, uint id) 
            : base("", id)
        {
            this.bitmap = bmp;
            this.width = (ushort)bmp.Width;
            this.height = (ushort)bmp.Height;
        }

        public override void Write(string path)
        {
            if (bitmap != null)
                bitmap.Save(path);
        }

        public override Bitmap GetBitmap()
        {
            return bitmap;
        }

        public override Bitmap GetBitmap(PaletteBase palette)
        {
            return bitmap;
        }

        public override uint GetHeight()
        {
            return height;
        }

        public override uint GetWidth()
        {
            return width;
        }

        public override bool Loaded
        {
            get { return bitmap != null; }
        }
    }
}
