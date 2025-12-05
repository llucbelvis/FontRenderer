using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Text;
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
        public KerningPair[] Kerning;
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

            Kerning = GetKerningSpacing();

            foreach (char c in CharacterGlyphDict.Keys) // debugging
            {
                Debug.WriteLine($"Char: {c}");
                Debug.WriteLine($"advanceWidth: {CharacterGlyphDict[c].advanceWidth}");
                Debug.WriteLine($"leftSideBearing: {CharacterGlyphDict[c].leftSideBearing}");
            }

        }

        public readonly struct KerningPair(Glyph LeftGlyph, Glyph RightGlyph, short kerning)
        {
            public readonly Glyph LeftGlyph = LeftGlyph;
            public readonly Glyph RightGlyph = RightGlyph;

            public readonly short kerning = kerning; 

        }

        public KerningPair[] GetKerningSpacing()
        {
            List<KerningPair> Kerning = new();

            Reader.GoTo(TableLocation["GPOS"].offset);

            Reader.SkipBytes(6);
            UInt16 featureListOffset = Reader.ReadUInt16();
            UInt16 lookupListOffset = Reader.ReadUInt16();

            Reader.GoTo(TableLocation["GPOS"].offset + featureListOffset);


            ushort featureCount = Reader.ReadUInt16();
            List<ushort> kernLookupIndices = new();

            for (int i = 0; i < featureCount; i++)
            {
                string tag = Encoding.ASCII.GetString(Reader.ReadBytes(4));
                ushort featureTableOffset = Reader.ReadUInt16();

                if (tag == "kern")
                {
                    

                    uint kernOffset = Reader.GetULocation();
                    uint kernFeatureTable = TableLocation["GPOS"].offset + featureListOffset + featureTableOffset;

                    Reader.GoTo(kernFeatureTable);
                    
                    ushort featureParams = Reader.ReadUInt16(); // usually 0
                    ushort lookupIndexCount = Reader.ReadUInt16();

                    for (int k = 0; k < lookupIndexCount; k++){

                        ushort lookupIndex = Reader.ReadUInt16();
                        if (!kernLookupIndices.Contains(lookupIndex))
                            kernLookupIndices.Add(lookupIndex);
                    }
                    
                    
                    Debug.WriteLine($"{tag} {kernOffset}");

                    Reader.GoTo(kernOffset);

                }
            }

            Debug.WriteLine($"{string.Join(",",kernLookupIndices)}");

            Reader.GoTo(TableLocation["GPOS"].offset + lookupListOffset);
            ushort lookupCount = Reader.ReadUInt16();

            Debug.WriteLine($"Total lookups in GPOS{lookupCount}");

            ushort[] lookupOffsets = new ushort[lookupCount];

            for (int i = 0; i < lookupCount; i++) 
            {
                lookupOffsets[i] = Reader.ReadUInt16();

            }

            foreach (ushort lookupIndex in kernLookupIndices)
            {
                uint lookupStart = TableLocation["GPOS"].offset + lookupListOffset + lookupOffsets[lookupIndex];
                Reader.GoTo(lookupStart);

                ushort lookupType = Reader.ReadUInt16();
                ushort lookupFlag = Reader.ReadUInt16();
                ushort subTableCount = Reader.ReadUInt16();

                if (lookupType == 2) 
                {

                    Debug.WriteLine("  ^ This is a Pair Adjustment lookup (kerning)");
                    ushort[] subTableOffsets = new ushort[subTableCount];
                    for (int i = 0; i< subTableCount; i++)
                    {
                        subTableOffsets[i] = Reader.ReadUInt16();
                    }

                    Debug.WriteLine($"  Has {subTableOffsets.Length} subtables");
                    foreach (ushort subTableOffset in subTableOffsets)
                    {
                        uint subTableStart = lookupStart + subTableOffset;
                        Reader.GoTo(subTableStart);

                        ushort posFormat = Reader.ReadUInt16();

                        if (posFormat == 1)
                        {
                            ushort coverageOffset = Reader.ReadUInt16();
                            ushort valueFormat1 = Reader.ReadUInt16();
                            ushort valueFormat2 = Reader.ReadUInt16();
                            ushort pairSetCount = Reader.ReadUInt16();

                            Debug.WriteLine($"    ValueFormat1: 0x{valueFormat1:X4}, ValueFormat2: 0x{valueFormat2:X4}");
                            Debug.WriteLine($"    PairSetCount: {pairSetCount}");

                            ushort[] pairSetOffsets = new ushort[pairSetCount];

                            for (int k = 0; k < pairSetCount; k++)
                            {
                                pairSetOffsets[k] = Reader.ReadUInt16();
                            }

                            uint currentPos = Reader.GetULocation();
                            Reader.GoTo(subTableStart + coverageOffset);
                            ushort coverageFormat = Reader.ReadUInt16();

                            List<uint> firstGlyph = new List<uint>();
                            if (coverageFormat == 1)
                            { 
                                uint glyphCount = Reader.ReadUInt16();
                                for (int k = 0; k < glyphCount; k++)
                                {
                                    firstGlyph.Add(Reader.ReadUInt16());
                                }
                            }
                            else if(coverageFormat == 2)
                            {
                                ushort rangeCount = Reader.ReadUInt16();
                                for (int k = 0; k < glyphCount; k++)
                                {
                                    uint startGlyph = Reader.ReadUInt16();
                                    uint endGlyph = Reader.ReadUInt16();
                                    uint startCoverageIndex = Reader.ReadUInt16();

                                    for (uint e = startGlyph; e <= endGlyph; e++)
                                    {
                                        firstGlyph.Add((ushort)e);
                                    }
                                }
                            }

                            Reader.GoTo(currentPos);

                            int valueSize1 = GetValueRecordSize(valueFormat1);
                            int valueSize2 = GetValueRecordSize(valueFormat2);

                            int totalPairs = 0;
                            for(int k = 0; k < pairSetCount; k++)
                            {
                                Reader.GoTo(subTableStart + pairSetOffsets[k]);
                                ushort pairValueCount = Reader.ReadUInt16();
                                totalPairs += pairValueCount;

                                for (int e = 0; e < pairValueCount; e++)
                                {
                                    ushort secondGlyph = Reader.ReadUInt16();

                                    short xAdvance1 = ReadValueRecord(valueFormat1);
                                    Reader.SkipBytes(valueSize2);

                                    if (xAdvance1 != 0)
                                    {
                                        Debug.WriteLine($"Left {firstGlyph[k]}, Right {secondGlyph}");

                                        Glyph Left = null;
                                        Glyph Right = null;

                                        foreach ((uint c,uint i ) in CharacterIndexMap)
                                        {
                                            if (i == firstGlyph[k])
                                            {
                                                Left = CharacterGlyphDict[(char)c];
                                                Debug.WriteLine($"Left {(char)c}");
                                            }
                                        }

                                        foreach ((uint c, uint i) in CharacterIndexMap)
                                        {
                                            if (i == secondGlyph)
                                            {
                                                Right = CharacterGlyphDict[(char)c];
                                                Debug.WriteLine($"Right {(char)c}");
                                            }
                                        }

                                        Debug.WriteLine($"Advance {xAdvance1}");

                                        Kerning.Add(new KerningPair(Left, Right, xAdvance1));
                                    }
                                }
                            }
                        }
                    }
                }
                else 
                { 
                
                }
            }


            return Kerning.ToArray();
        }

        private int GetValueRecordSize(ushort valueFormat)
        {
            int size = 0;
            if ((valueFormat & 0x0001) != 0) size += 2; // XPlacement
            if ((valueFormat & 0x0002) != 0) size += 2; // YPlacement
            if ((valueFormat & 0x0004) != 0) size += 2; // XAdvance
            if ((valueFormat & 0x0008) != 0) size += 2; // YAdvance
            if ((valueFormat & 0x0010) != 0) size += 2; // XPlaDevice
            if ((valueFormat & 0x0020) != 0) size += 2; // YPlaDevice
            if ((valueFormat & 0x0040) != 0) size += 2; // XAdvDevice
            if ((valueFormat & 0x0080) != 0) size += 2; // YAdvDevice
            return size;
        }

        private short ReadValueRecord(ushort valueFormat)
        {
            short xAdvance = 0;

            if ((valueFormat & 0x0001) != 0) Reader.SkipBytes(2); // XPlacement
            if ((valueFormat & 0x0002) != 0) Reader.SkipBytes(2); // YPlacement
            if ((valueFormat & 0x0004) != 0) xAdvance = Reader.ReadInt16(); // XAdvance
            if ((valueFormat & 0x0008) != 0) Reader.SkipBytes(2); // YAdvance
            if ((valueFormat & 0x0010) != 0) Reader.SkipBytes(2); // XPlaDevice
            if ((valueFormat & 0x0020) != 0) Reader.SkipBytes(2); // YPlaDevice
            if ((valueFormat & 0x0040) != 0) Reader.SkipBytes(2); // XAdvDevice
            if ((valueFormat & 0x0080) != 0) Reader.SkipBytes(2); // YAdvDevice

            return xAdvance;
        }
        public static void ListenForInput()
        {

        }

        Dictionary<char, Glyph> GetCharacterGlyphDict()
        {
            Dictionary<char, Glyph> CharacterGlyphDict = new();

            for (int i = 0; i < CharacterIndexMap.Length; i++) // map each character to its glyph
            {
                uint glyphIndex = CharacterIndexMap[i].index; 

                CharacterGlyphDict.Add((char)CharacterIndexMap[i].unicode, Glyph.ReadSimpleGlyph(Reader, GlyphLocation, GlyphLocation[glyphIndex], Metrics[glyphIndex].advanceWidth, Metrics[glyphIndex].leftSideBearing));
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
