using System;

namespace Images
{
    public enum PCX_Version
    {
        Fixed_EGA_Palette = 0,
        Modifiable_EGA_Palette = 2,
        No_Palette = 3,
        Windows = 4,
        Including_24_bit_Images = 5
    }

    public enum PCX_Image_Encoding
    {
        No_Encoding = 0,
        RLE = 1
    }

    public enum PCX_Bits_Per_Pixel
    {
        Color_2 = 1,
        Color_4 = 2,
        Color_16 = 4,
        Color_256 = 8
    }

    public enum PCX_Palette_Mode
    {
        Color = 1,
        Grayscale = 2
    }
}
