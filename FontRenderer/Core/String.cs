using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Text;
using static Microsoft.Xna.Framework.Graphics.SpriteFont;
using static System.Net.Mime.MediaTypeNames;

namespace Text
{
    internal class String : Mesh
    {
        public string text;
        public Font Font;

        public String(GraphicsDevice GraphicsDevice, Vector3 position, Vector3 origin, float depth, Font Font, string text) : base(GraphicsDevice, position, origin, depth, null, null)
        {
            this.text = text;
            this.Font = Font;

            List<VertexPosition> vertex = new();
            List<uint> index = new();

            uint indexOffset = 0;
            float spacing = 0;

            Glyph PreviousGlyph = null;

            foreach (char c in text)
            {
                if (Font.CharacterGlyphDict[c] == null)
                {

                    (spacing, vertex, index, indexOffset) = AddGlyph(Font.CharacterGlyphDict.First().Value, spacing, vertex, index, indexOffset, 0);
                    PreviousGlyph = null;
                    continue;
                }

                if (c == ' ')
                {
                    spacing += (Font.CharacterGlyphDict[' '].advanceWidth / 10000f);
                    PreviousGlyph = null;
                    continue;
                }

                if (PreviousGlyph == null)
                {
                    (spacing, vertex, index, indexOffset) = AddGlyph(Font.CharacterGlyphDict[c], spacing, vertex, index, indexOffset, 0);
                    PreviousGlyph = Font.CharacterGlyphDict[c];
                }
                else
                {
                    
                    foreach (Text.Font.KerningPair Pair in Font?.Kerning)
                    {
                        if (Pair.LeftGlyph == PreviousGlyph && Pair.RightGlyph == Font.CharacterGlyphDict[c])
                        {
                            (spacing, vertex, index, indexOffset) = AddGlyph(Font.CharacterGlyphDict[c], spacing, vertex, index, indexOffset, Pair.kerning);
                            break;
                        }
                        else
                        {
                            (spacing, vertex, index, indexOffset) = AddGlyph(Font.CharacterGlyphDict[c], spacing, vertex, index, indexOffset, 0);
                            break;
                        }
                    }
                    PreviousGlyph = Font.CharacterGlyphDict[c];
                }
            }


            this.index = index.ToArray();
            this.vertex = vertex.ToArray();

            this.Buffer(GraphicsDevice);
        }

        public (float, List<VertexPosition>, List<uint>, uint) AddGlyph(Glyph Glyph, float spacing, List<VertexPosition> vertex, List<uint> index, uint indexOffset, short kerning)
        {
            foreach (uint i in Glyph.index)
            {
                index.Add((uint)(i + indexOffset));
            }

            indexOffset += (uint)Glyph.index.Length;

            foreach (VertexPosition v in Glyph.vertex)
            {
                VertexPosition spaced = v;

                spaced.Position.X += spacing;
                spaced.Position.X += Glyph.leftSideBearing / 10000f;

                vertex.Add(spaced);
            }

            spacing += (kerning == 0) ? Glyph.advanceWidth / 10000f : kerning / 10000f;

            return (spacing, vertex, index, indexOffset);
        }
    }
}
