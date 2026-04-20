using System;

namespace Images
{
    public class PCX_Byte_Converter
    {
        public static byte[] Get_Bytes(byte[] Source, int Offset, int Length)
        {
            byte[] result = new byte[Length];
            for (int i = 0; i < Length; i++)
                result[i] = Source[Offset + i];
            return result;
        }

        public static ushort Get_UShort(byte[] Source, int Offset)
        {
            return (ushort)((ushort)(Source[Offset + 1] << 8) | Source[Offset]);
        }
    }
}
