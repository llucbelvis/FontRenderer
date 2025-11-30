using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Text
{
    public class Reader
    {
        public BinaryReader BinaryReader;

        public Reader(Stream stream)
        {
            this.BinaryReader = new BinaryReader(stream);
        }


        public void GoTo(uint byteOffsetFromOrigin)
        {
            BinaryReader.BaseStream.Position = byteOffsetFromOrigin;
        }

        public int GetLocation()
        {
            return (int)BinaryReader.BaseStream.Position;
        }

        public uint GetULocation()
        {
            return (uint)BinaryReader.BaseStream.Position;
        }

        public UInt16 ReadUInt16()
        {

            UInt16 value = BinaryReader.ReadUInt16();

            if (BitConverter.IsLittleEndian)
            {
                value = (UInt16)((value >> 8) | (value << 8));
            }

            return value;
        }

        public Int16 ReadInt16()
        {
            Int16 value = BinaryReader.ReadInt16();
            if (BitConverter.IsLittleEndian)
            {
                value = (Int16)((value >> 8) | (value << 8));
            }
            return value;
        }

        public UInt32 ReadUInt32()
        {
            UInt32 value = BinaryReader.ReadUInt32();

            if (BitConverter.IsLittleEndian)
            {
                const byte mask = 0b11111111;
                UInt32 a = (value >> 24) & mask;
                UInt32 b = (value >> 16) & mask;
                UInt32 c = (value >> 8) & mask;
                UInt32 d = (value >> 0) & mask;
                value = a << 0 | b << 8 | c << 16 | d << 24;
            }

            return value;
        }

        public byte ReadByte()
        {
            byte value = BinaryReader.ReadByte();

            return value;
        }

        public sbyte ReadSByte()
        {
            sbyte value = BinaryReader.ReadSByte();

            return value;
        }

        public float ReadFixedPoint2Dot14()
        {
            return ReadInt16() / 16384f;
        }

        public string ReadTag()
        {
            Span<char> tag = stackalloc char[4];

            for (int i = 0; i < tag.Length; i++)
                tag[i] = (char)BinaryReader.ReadByte();

            return tag.ToString();
        }

        public void SkipBytes(int count) { BinaryReader.BaseStream.Position += count; }

    }
}