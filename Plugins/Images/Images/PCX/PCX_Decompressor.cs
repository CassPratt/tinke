using System;
using System.Collections.Generic;

namespace Images
{
    public class PCX_Decompressor
    {
        public static byte[] No_Compross(byte[] File)
        {
            int Maximum_Index = File.Length;
            byte[] Result = new byte[Maximum_Index - 128];
            for (int i = 128; i < Maximum_Index; i++)
                Result[i - 128] = File[i];
            return Result;
        }

        public static byte[] Decompross(byte[] File)
        {
            List<byte> Result_List = new List<byte>();
            int Maximum_Index = File.Length;
            int Op, Repeat_Time;
            byte Repeat_Value;
            
            for (int i = 128; i < Maximum_Index; i++)
            {
                Op = (File[i] >> 6) & 3;
                if (Op == 3)
                {
                    Repeat_Time = File[i] & 0x3f;
                    Repeat_Value = File[++i];
                    for (int j = 0; j < Repeat_Time; j++)
                        Result_List.Add(Repeat_Value);
                }
                else
                    Result_List.Add(File[i]);
            }
            
            return Result_List.ToArray();
        }
    }
}
