using System;
using System.Drawing;
using System.IO;

namespace Images
{
    public class PCX_Decoder
    {
        public static PCX_Decoded_Object Decode(byte[] File_Data)
        {
            PCX_Header header = PCX_Header.Get_Parameter(File_Data);
            if (header == null)
                return null;

            byte[] decomprossed = null;
            switch (header.Encoding)
            {
                case PCX_Image_Encoding.No_Encoding:
                    decomprossed = PCX_Decompressor.No_Compross(File_Data);
                    break;
                case PCX_Image_Encoding.RLE:
                    decomprossed = PCX_Decompressor.Decompross(File_Data);
                    break;
            }

            PCX_Decoded_Object decoded = new PCX_Decoded_Object(header, decomprossed);
            
            switch (header.Bits_Per_Pixel)
            {
                case PCX_Bits_Per_Pixel.Color_256:
                    if (header.Planes_Per_Pixel == 3 && header.Palette_Mode == PCX_Palette_Mode.Color)
                        Decode_256_Without_Palette(decoded);
                    else if (header.Planes_Per_Pixel == 1)
                        Decode_256_With_Palette(decoded);
                    break;
                case PCX_Bits_Per_Pixel.Color_4:
                    Decode_4_Bit(decoded);
                    break;
                case PCX_Bits_Per_Pixel.Color_16:
                    Decode_16_Color(decoded);
                    break;
            }
            
            return decoded;
        }

        private static void Decode_256_Without_Palette(PCX_Decoded_Object Decoding_Object)
        {
            PCX_Header header = Decoding_Object.Get_Header();
            byte[] content = Decoding_Object.Get_Decomprossed_Data();
            Bitmap Result = new Bitmap(header.Width, header.Height);
            
            int Color_Temp;
            int Double_Width = header.Width << 1;
            int Content_Offset = 0;
            
            for (int h = 0; h < header.Height; h++)
            {
                for (int w = 0; w < header.Width; w++)
                {
                    if (Content_Offset + Double_Width < content.Length)
                    {
                        Color_Temp = -16777216;
                        Color_Temp |= content[Content_Offset] << 16;
                        Color_Temp |= content[Content_Offset + header.Width] << 8;
                        Color_Temp |= content[Content_Offset + Double_Width];
                        Content_Offset++;
                        Result.SetPixel(w, h, Color.FromArgb(Color_Temp));
                    }
                }
                Content_Offset += Double_Width;
            }
            
            Decoding_Object.Set_Result(Result);
        }

        private static void Decode_256_With_Palette(PCX_Decoded_Object Decoding_Object)
        {
            PCX_Header header = Decoding_Object.Get_Header();
            byte[] content = Decoding_Object.Get_Decomprossed_Data();
            Bitmap Result = new Bitmap(header.Width, header.Height);
            
            // Use grayscale or simple palette
            for (int h = 0; h < header.Height; h++)
            {
                int row_offset = h * header.Bytes_Per_Line;
                for (int w = 0; w < header.Width; w++)
                {
                    if (row_offset + w < content.Length)
                    {
                        byte pixel = content[row_offset + w];
                        Result.SetPixel(w, h, Color.FromArgb(pixel, pixel, pixel));
                    }
                }
            }
            
            Decoding_Object.Set_Result(Result);
        }

        private static void Decode_4_Bit(PCX_Decoded_Object Decoding_Object)
        {
            PCX_Header header = Decoding_Object.Get_Header();
            byte[] content = Decoding_Object.Get_Decomprossed_Data();
            Bitmap Result = new Bitmap(header.Width, header.Height);
            
            for (int h = 0; h < header.Height; h++)
            {
                for (int w = 0; w < header.Width; w++)
                {
                    Result.SetPixel(w, h, Color.Black);
                }
            }
            
            Decoding_Object.Set_Result(Result);
        }

        private static void Decode_16_Color(PCX_Decoded_Object Decoding_Object)
        {
            PCX_Header header = Decoding_Object.Get_Header();
            byte[] content = Decoding_Object.Get_Decomprossed_Data();
            Bitmap Result = new Bitmap(header.Width, header.Height);
            
            for (int h = 0; h < header.Height; h++)
            {
                for (int w = 0; w < header.Width; w++)
                {
                    Result.SetPixel(w, h, Color.Black);
                }
            }
            
            Decoding_Object.Set_Result(Result);
        }
    }
}
