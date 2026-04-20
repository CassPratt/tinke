using System;

namespace Images
{
    public class PCX_Header
    {
        public PCX_Version Version = PCX_Version.Fixed_EGA_Palette;
        public PCX_Image_Encoding Encoding = PCX_Image_Encoding.No_Encoding;
        public PCX_Bits_Per_Pixel Bits_Per_Pixel = PCX_Bits_Per_Pixel.Color_256;
        public ushort Min_X = 0;
        public ushort Min_Y = 0;
        public ushort Max_X = 0;
        public ushort Max_Y = 0;
        public ushort Horizontal_DPI = 0;
        public ushort Vertical_DPI = 0;
        public byte[] EGA_Palette = new byte[48];
        public byte Planes_Per_Pixel = 0; //1, 3, 4
        public ushort Bytes_Per_Line = 0;
        public PCX_Palette_Mode Palette_Mode = PCX_Palette_Mode.Color;
        public ushort Source_Horizontal_DPI = 0;
        public ushort Source_Vertical_DPI = 0;

        public ushort Height = 0;
        public ushort Width = 0;

        public static PCX_Header Get_Parameter(byte[] File)
        {
            if (File.Length < 128)
                return null;

            if (File[0] != 0x0A)
                return null;

            PCX_Header header = new PCX_Header();
            
            try
            {
                header.Version = (PCX_Version)File[1];
                header.Encoding = (PCX_Image_Encoding)File[2];
                header.Bits_Per_Pixel = (PCX_Bits_Per_Pixel)File[3];
                header.Min_X = PCX_Byte_Converter.Get_UShort(File, 4);
                header.Min_Y = PCX_Byte_Converter.Get_UShort(File, 6);
                header.Max_X = PCX_Byte_Converter.Get_UShort(File, 8);
                header.Max_Y = PCX_Byte_Converter.Get_UShort(File, 10);
                header.Horizontal_DPI = PCX_Byte_Converter.Get_UShort(File, 12);
                header.Vertical_DPI = PCX_Byte_Converter.Get_UShort(File, 14);
                header.EGA_Palette = PCX_Byte_Converter.Get_Bytes(File, 16, 48);
                header.Planes_Per_Pixel = File[65]; //1, 3, 4
                header.Bytes_Per_Line = PCX_Byte_Converter.Get_UShort(File, 66);
                header.Palette_Mode = (PCX_Palette_Mode)PCX_Byte_Converter.Get_UShort(File, 68);
                header.Source_Horizontal_DPI = PCX_Byte_Converter.Get_UShort(File, 70);
                header.Source_Vertical_DPI = PCX_Byte_Converter.Get_UShort(File, 72);

                header.Width = (ushort)(header.Max_X - header.Min_X + 1);
                header.Height = (ushort)(header.Max_Y - header.Min_Y + 1);
                
                return header;
            }
            catch
            {
                return null;
            }
        }
    }
}
