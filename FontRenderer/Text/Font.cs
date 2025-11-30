using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using static Microsoft.Xna.Framework.Graphics.SpriteFont;


namespace Text
{
    public class Font
    {
        //REVISE FUNCTIONS TO STANDARTIZE NAMING

        public Stream Stream;
        public Reader Reader;

        Dictionary<string, (uint offset, uint length)> TableLocation;

        uint[] GlyphLocation;
        public Dictionary<char, Glyph> CharacterGlyphDict;
        (uint unicode, uint index)[] CharacterIndexMap;
        (uint advanceWidth, int leftSideBearing)[] Metrics;
        int glyphCount;


        public Font(GraphicsDevice GraphicsDevice, string path)
        {
            Stream = File.Open(path, FileMode.Open);
            Reader = new(Stream);

            Reader.SkipBytes(4); // skipping information which does not need reading
            UInt16 tableCount = Reader.ReadUInt16(); // number of present tables on the font
            Reader.SkipBytes(6);

            TableLocation = GetTables(tableCount);

            glyphCount = GetGlyphCount();

            GlyphLocation = GetGlyphLocations();
            CharacterIndexMap = GetCharacterIndexMap(); ; // get the number of CharacterGlyphDict present in the glyf table
            Metrics = GetIndexSpacing(); // get the spacing for each character

            CharacterGlyphDict = GetCharacterGlyphDict();

            foreach (char c in CharacterGlyphDict.Keys) // debugging
            {
                Debug.WriteLine($"Char: {c}");
                Debug.WriteLine($"advanceWidth: {CharacterGlyphDict[c].advanceWidth}");
                Debug.WriteLine($"leftSideBearing: {CharacterGlyphDict[c].leftSideBearing}");
            }

        }

        public static void ListenForInput()
        {

        }

        Dictionary<char, Glyph> GetCharacterGlyphDict()
        {
            Dictionary<char, Glyph> CharacterGlyphDict = new();

            for (int i = 0; i < CharacterIndexMap.Length; i++) // map each character to its glyph
            {
                CharacterGlyphDict.Add((char)CharacterIndexMap[i].unicode, Glyph.ReadSimpleGlyph(Reader, GlyphLocation, GlyphLocation[CharacterIndexMap[i].index], Metrics[i].advanceWidth, Metrics[i].leftSideBearing));
                CharacterGlyphDict.Last().Value.Load();
            }

            return CharacterGlyphDict;
        }

        Dictionary<string, (uint offset, uint length)> GetTables(UInt16 tableCount)
        {
            Dictionary<string, (uint offset, uint length)> TableLocation = new();

            for (int i = 0; i < tableCount; i++) // reading the data on the information of each table
            {
                string tag = Reader.ReadTag(); // name of the table
                uint checkSum = Reader.ReadUInt32();
                uint offset = Reader.ReadUInt32(); // bytes from origin
                uint length = Reader.ReadUInt32(); // length of the table in bytes

                TableLocation.Add(tag, (offset, length));

                Debug.WriteLine($"Tag: {tag} Location: {offset} "); // debugging
            }

            return TableLocation;
        }

        int GetGlyphCount()
        {
            Reader.GoTo(TableLocation["maxp"].offset + 4);

            return Reader.ReadUInt16();
        }

        (uint advanceWidth, int leftSideBearing)[] GetIndexSpacing()
        {
            Reader.GoTo(TableLocation["hhea"].offset);
            Reader.SkipBytes(34);
            ushort entries = Reader.ReadUInt16();


            Reader.GoTo(TableLocation["hmtx"].offset);

            (uint advanceWidth, int leftSideBearing)[] metrics = new (uint advanceWidth, int leftSideBearing)[glyphCount];

            for (int i = 0; i < entries; i++)
            {
                metrics[i].advanceWidth = Reader.ReadUInt16();
                metrics[i].leftSideBearing = Reader.ReadInt16();
            }

            uint lastAdvanceWidth = metrics[entries - 1].advanceWidth;

            if (entries < glyphCount)
            {
                for (int i = entries; i < glyphCount; i++)
                {
                    metrics[i].advanceWidth = lastAdvanceWidth;
                    metrics[i].leftSideBearing = Reader.ReadInt16();
                }
            }

            return metrics;
        }

        uint[] GetGlyphLocations()
        {
            Reader.GoTo(TableLocation["head"].offset);
            Reader.SkipBytes(50);

            bool isTwoByteEntry = (short)Reader.ReadUInt16() == 0;

            uint locationTableStart = TableLocation["loca"].offset;
            uint glyphTableStart = TableLocation["glyf"].offset;
            uint[] glyphLocations = new uint[glyphCount];

            for (int i = 0; i < glyphCount; i++)
            {
                Reader.GoTo(locationTableStart + (uint)(i * (isTwoByteEntry ? 2 : 4)));
                uint glyphDataOffset = isTwoByteEntry ? Reader.ReadUInt16() * 2u : Reader.ReadUInt32();
                glyphLocations[i] = glyphTableStart + glyphDataOffset;
            }

            return glyphLocations;
        }

        (uint unicode, uint index)[] GetCharacterIndexMap()
        {
            List<(uint unicode, uint index)> map = new();
            uint cmapOffset = TableLocation["cmap"].offset;

            Reader.GoTo(cmapOffset);

            uint version = Reader.ReadUInt16();
            uint numSubtables = Reader.ReadUInt16();

            uint cmapSubtableOffset = uint.MaxValue;

            bool hasReadMissingCharGlyph = false;

            for (int i = 0; i < numSubtables; i++)
            {
                uint platformID = Reader.ReadUInt16();
                uint platfromSpecificID = Reader.ReadUInt16();
                uint offset = Reader.ReadUInt32();


                if (platformID == 0)
                {
                    uint unicodeVersion = platfromSpecificID;

                    if (unicodeVersion == 4)
                    {
                        cmapSubtableOffset = offset;
                    }

                    if (unicodeVersion == 3 && cmapSubtableOffset == uint.MaxValue)
                    {
                        cmapSubtableOffset = offset;
                    }

                }
            }

            if (cmapSubtableOffset == 0)
            {
                throw new Exception("Font does not contain supported character map type");
            }

            Reader.GoTo(cmapOffset + cmapSubtableOffset);
            uint format = Reader.ReadUInt16();

            if (format != 12 && format != 4)
            {
                throw new Exception("Font cmap format not supported");
            }

            else if (format == 12)
            {
                //Debug.WriteLine("12");

                int reserved = Reader.ReadUInt16();
                uint byteLength = Reader.ReadUInt32();
                uint language = Reader.ReadUInt32();
                uint numGroups = Reader.ReadUInt32();

                for (int i = 0; i < numGroups; i++)
                {
                    uint startCharCode = Reader.ReadUInt32();
                    uint endCharCode = Reader.ReadUInt32();
                    uint startGlyphIndex = Reader.ReadUInt32();

                    uint numChars = endCharCode - startCharCode + 1;

                    for (uint charOffset = 0; charOffset < numChars; charOffset++)
                    {
                        uint charCode = (uint)(startCharCode + charOffset);
                        uint glyphIndex = (uint)(startGlyphIndex + charOffset);
                        map.Add((charCode, glyphIndex));
                    }
                }
            }
            else if (format == 4)
            {
                int length = Reader.ReadUInt16();
                int languageCode = Reader.ReadUInt16();
                int segCount2X = Reader.ReadUInt16();
                int segCount = segCount2X / 2;
                Reader.SkipBytes(6);

                int[] endCodes = new int[segCount];
                for (int i = 0; i < segCount; i++)
                {
                    endCodes[i] = Reader.ReadUInt16();
                }

                Reader.SkipBytes(2);

                int[] startCodes = new int[segCount];
                for (int i = 0; i < segCount; i++)
                {
                    startCodes[i] = Reader.ReadUInt16();
                }

                int[] idDeltas = new int[segCount];
                for (int i = 0; i < segCount; i++)
                {
                    idDeltas[i] = Reader.ReadUInt16();
                }

                (int offset, int readLoc)[] idRangeOffsets = new (int, int)[segCount];
                for (int i = 0; i < segCount; i++)
                {
                    int readLoc = (int)Reader.GetLocation();
                    int offset = Reader.ReadUInt16();
                    idRangeOffsets[i] = (offset, readLoc);
                }

                for (int i = 0; i < startCodes.Length; i++)
                {
                    int endCode = endCodes[i];
                    int currCode = startCodes[i];

                    if (currCode == 65535) break;

                    while (currCode <= endCode)
                    {
                        int glyphIndex;
                        if (idRangeOffsets[i].offset == 0)
                        {
                            glyphIndex = (currCode + idDeltas[i]) % 65536;
                        }
                        else
                        {
                            uint ReaderLocationOld = (uint)Reader.GetLocation();
                            int rangeOffsetLocation = idRangeOffsets[i].readLoc + idRangeOffsets[i].offset;
                            int glyphIndexArrayLocation = 2 * (currCode - startCodes[i]) + rangeOffsetLocation;

                            Reader.GoTo((uint)glyphIndexArrayLocation);
                            glyphIndex = Reader.ReadUInt16();

                            if (glyphIndex != 0)
                            {
                                glyphIndex = (glyphIndex + idDeltas[i]) % 65536;
                            }

                            Reader.GoTo(ReaderLocationOld);
                        }

                        map.Add(new((uint)currCode, (uint)glyphIndex));
                        hasReadMissingCharGlyph |= glyphIndex == 0;
                        currCode++;
                    }
                }

                if (!hasReadMissingCharGlyph)
                {
                    map.Add(new(65535, 0));
                }
            }

            return map.ToArray();
        }
    }
}
