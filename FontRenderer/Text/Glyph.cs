using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Text
{
    public class Glyph
    {
        public int[] contourEndIndices;
        public Point[] Points;

        public VertexPosition[] vertex;
        public int[] index;

        public uint advanceWidth;
        public int leftSideBearing;

        public struct Point(Vector3 position, bool onCurve)
        {
            public Vector3 position = position;
            public bool onCurve = onCurve;
        }

        public Glyph(int[] contourEndIndices, Point[] Points, uint advanceWidth, int leftSideBearing)
        {
            this.contourEndIndices = contourEndIndices;
            this.Points = Points;
            this.advanceWidth = advanceWidth;
            this.leftSideBearing = leftSideBearing;
        }

        public static Glyph ReadSimpleGlyph(Reader Reader, uint[] GlyphLocation, uint glyphLocation, uint advanceWidth, int leftSideBearing)
        {
            Reader.GoTo(glyphLocation);
            short numberOfContours = (short)Reader.ReadUInt16();

            if (numberOfContours == 0)
            {
                return new Glyph(new int[0], new Point[0], 0, 0);
            }
            if (numberOfContours < 0)
            {
                return ReadCompoundGlyph(Reader, GlyphLocation, glyphLocation, advanceWidth, leftSideBearing);
            }

            int[] contourEndIndices = new int[numberOfContours];
            Reader.SkipBytes(8);

            for (int i = 0; i < contourEndIndices.Length; i++)
                contourEndIndices[i] = Reader.ReadUInt16();

            int numPoints = contourEndIndices[^1] + 1;
            byte[] flags = new byte[numPoints];
            Reader.SkipBytes(Reader.ReadUInt16());

            for (int i = 0; i < numPoints; i++)
            {
                byte flag = Reader.ReadByte();
                flags[i] = flag;

                if (FlagBitIsSet(flag, 3))
                {
                    byte repeatCount = Reader.ReadByte();
                    for (int j = 0; j < repeatCount && i < numPoints; j++)
                    {
                        i++;
                        flags[i] = flag;
                    }
                }

            }

            CoordsOnCurve xCoords = ReadCoordinates(Reader, flags, readingX: true);
            CoordsOnCurve yCoords = ReadCoordinates(Reader, flags, readingX: false);

            Point[] Points = new Point[xCoords.coords.Length];

            for (int i = 0; i < xCoords.coords.Length; i++)
            {
                Points[i] = new Point
                {
                    position = new Vector3(xCoords.coords[i] / 10000f, yCoords.coords[i] / 10000f, 0),
                    onCurve = xCoords.onCurve[i],
                };
            }

            return new Glyph(contourEndIndices, Points, advanceWidth, leftSideBearing);
        }

        public static List<Vector3> PointsToBezier(List<Vector3> finalContour, Vector3 p0, Vector3 p1, Vector3 p2, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                float t = (float)i / iterations;

                finalContour.Add(Bezier.Quadratic(p0, p1, p2, t));
            }

            return finalContour;
        }

        public static List<Vector3> ContourToBezier(Span<Point> Points)
        {
            List<Point> midContour = new();
            List<Vector3> finalContour = new();

            for (int i = 0; i < Points.Length; i++)
            {

                Point p0 = Points[i];
                Point p1 = Points[(i + 1) % Points.Length];

                midContour.Add(p0);

                if (!p0.onCurve && !p1.onCurve)
                    midContour.Add(new Point(Vector3.Lerp(p0.position, p1.position, 0.5f), true));
            }

            for (int i = 0; i < midContour.Count; i++)
            {
                Point p0 = midContour[i];
                Point p1 = midContour[(i + 1) % midContour.Count];
                Point p2 = midContour[(i + 2) % midContour.Count];

                if (p0.onCurve && !p1.onCurve && p2.onCurve)
                {
                    finalContour = PointsToBezier(finalContour, p0.position, p1.position, p2.position, 2);
                    i++;
                }
                else if (p0.onCurve)
                {
                    finalContour.Add(p0.position);
                }
            }

            return finalContour;
        }

        bool Convex(Vector3 v1, Vector3 v2, bool isClockwise)
        {
            float cross = Cross(v1, v2);
            return isClockwise ? cross > 0 : cross < 0;
        }

        int Ray(Vector3 p, Vector3 a, Vector3 b, int intersections)
        {

            if (Math.Abs(a.Y - b.Y) < 1e-6f)
                return intersections;
            if (!((a.Y <= p.Y && b.Y > p.Y) || (a.Y > p.Y && b.Y <= p.Y)))
                return intersections;

            float t = (p.Y - a.Y) / (b.Y - a.Y);
            if (t < -1e-6f || t > 1 + 1e-6f)
                return intersections;

            float X = MathHelper.Lerp(a.X, b.X, t);

            if (X >= p.X - 1e-6f)
                intersections++;

            return intersections;
        }


        (bool intersected, float t, Vector3 intersection) Ray(Vector3 p, Vector3 a, Vector3 b)
        {
            if (!((a.Y <= p.Y && b.Y >= p.Y) || (a.Y >= p.Y && b.Y <= p.Y)))
                return (false, 0, Vector3.Zero);

            float A = -a.Y + b.Y;
            float B = p.Y - a.Y;
            float t = B / A;

            float X = MathHelper.Lerp(a.X, b.X, t);

            if (t >= 0 && t <= 1 && X >= p.X)
            {
                return (true, X - p.X, new Vector3(X, p.Y, 0));
            }

            return (false, 0, Vector3.Zero);
        }

        int Ray(Vector3 p, Vector3 a, Vector3 b, Vector3 c, int intersections)
        {
            float A = a.Y - 2 * b.Y + c.Y;
            float B = -2 * a.Y + 2 * b.Y;
            float C = a.Y - p.Y;

            float discriminant = B * B - 4 * A * C;

            if (discriminant >= 0 && Math.Abs(A) > 1e-6)
            {
                float d = (float)Math.Sqrt(discriminant);
                float invA2 = 1 / (2 * A);

                float t1 = (-B + d) * invA2;
                float t2 = (-B - d) * invA2;

                float x1 = MathHelper.Lerp(MathHelper.Lerp(a.X, b.X, t1), MathHelper.Lerp(b.X, c.X, t1), t1);
                float x2 = MathHelper.Lerp(MathHelper.Lerp(a.X, b.X, t2), MathHelper.Lerp(b.X, c.X, t2), t2);

                intersections += (t1 > 0 && t1 < 1 && x1 > p.X) ? 1 : 0;
                intersections += (t2 > 0 && t2 < 1 && x2 > p.X) ? 1 : 0;
            }

            return intersections;
        }

        (bool intersected, float t) Ray(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float A = a.Y - 2 * b.Y + c.Y;
            float B = -2 * a.Y + 2 * b.Y;
            float C = a.Y - p.Y;

            float discriminant = B * B - 4 * A * C;

            if (discriminant >= 0 && Math.Abs(A) > 1e-6)
            {
                float d = (float)Math.Sqrt(discriminant);
                float invA2 = 1 / (2 * A);

                float t1 = (-B + d) * invA2;
                float t2 = (-B - d) * invA2;

                float x1 = MathHelper.Lerp(MathHelper.Lerp(a.X, b.X, t1), MathHelper.Lerp(b.X, c.X, t1), t1);
                float x2 = MathHelper.Lerp(MathHelper.Lerp(a.X, b.X, t2), MathHelper.Lerp(b.X, c.X, t2), t2);

                if (t1 > 0 && t1 < 1 && x1 > p.X) return (true, t1);
                else if (t2 > 0 && t2 < 1 && x2 > p.X) return (true, t2);
            }

            return (false, 0);
        }

        int Intersection(Vector3 point, List<Vector3> contour, int intersections)
        {
            int i = 0;

            if (contour.Count == 0)
                return intersections;

            while (i < contour.Count)
            {
                Vector3 p0 = contour[i];

                Vector3 p1 = contour[(i + 1) % contour.Count];


                intersections = Ray(point, p0, p1, intersections);
                i += 1;

            }

            return intersections;
        }

        public struct RayResult()
        {
            public Vector3 p0, p1, intersection;
            public float t;
        }

        (bool intersected, List<RayResult>) Intersection(Vector3 vertex, Vector3[] contour)
        {
            int i = 0;
            List<RayResult> results = new();

            if (contour.Length == 0)
                return (false, results);

            while (i < contour.Length)
            {
                Vector3 p0 = contour[i];

                Vector3 p1 = contour[(i + 1) % contour.Length];


                (bool intersected, float t, Vector3 intersection) = Ray(vertex, p0, p1);

                if (intersected)
                    results.Add(new RayResult() { p0 = p0, p1 = p1, intersection = intersection, t = t });
                i += 1;
            }

            return (results.Any(), results);
        }

        bool PointOnTriangle(Vector3 p, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            float cross1 = Cross(v2 - v1, p - v1);
            float cross2 = Cross(v3 - v2, p - v2);
            float cross3 = Cross(v1 - v3, p - v3);

            bool hasNeg = (cross1 < 0) || (cross2 < 0) || (cross3 < 0);
            bool hasPos = (cross1 > 0) || (cross2 > 0) || (cross3 > 0);

            return !(hasNeg && hasPos);
        }
        bool IsClockwise(List<Vector3> contour)
        {
            float sum = 0;
            for (int i = 0; i < contour.Count; i++)
            {
                Vector3 curr = contour[i];
                Vector3 next = contour[(i + 1) % contour.Count];
                sum += (next.X - curr.X) * (next.Y + curr.Y);
            }
            return sum > 0;
        }

        private static float Cross(Vector3 p1, Vector3 p2)
        {
            return p1.X * p2.Y - p1.Y * p2.X;
        }

        Vector3[] ContourToTriangle(List<Vector3> contour)
        {
            contour = new List<Vector3>(contour);
            int offset = 0;
            List<Vector3> triangles = new();
            int maxAttempts = contour.Count * 1000;
            int attempts = 0;

            while (contour.Count > 3 && attempts < maxAttempts)
            {
                attempts++;
                Vector3 prev = contour[(offset - 1 + contour.Count) % contour.Count];
                Vector3 curr = contour[offset];
                Vector3 next = contour[(offset + 1) % contour.Count];

                Vector3 prevC = prev - curr;
                Vector3 nextC = next - curr;

                bool pointOnTriangle = false;

                foreach (Vector3 p in contour)
                {
                    if (p == prev || p == curr || p == next)
                        continue;

                    if (PointOnTriangle(p, prev, curr, next))
                        pointOnTriangle = true;
                }

                if (Convex(prevC, nextC, IsClockwise(contour)) && !pointOnTriangle)
                {
                    triangles.Add(prev); triangles.Add(curr); triangles.Add(next);

                    contour.RemoveAt(offset);

                    offset = 0;

                }
                else
                    offset = (offset + 1) % contour.Count;
            }

            if (contour.Count == 3)
            {
                triangles.Add(contour[0]);
                triangles.Add(contour[1]);
                triangles.Add(contour[2]);
            }

            return triangles.ToArray();
        }

        static Glyph ReadCompoundGlyph(Reader Reader, uint[] GlyphLocation, uint glyphLocation, uint advanceWidth, int leftSideBearing)
        {
            Reader.GoTo(glyphLocation);
            Reader.SkipBytes(2 * 5);

            List<Point> allPoints = new();
            List<int> allContourEndIndices = new();

            while (true)
            {
                (Glyph componentGlyph, bool isLast) = ReadNextComponentGlyph(Reader, GlyphLocation);

                int indexOffset = allPoints.Count;
                allPoints.AddRange(componentGlyph.Points);

                foreach (int endIndex in componentGlyph.contourEndIndices)
                {
                    allContourEndIndices.Add(endIndex + indexOffset);
                }

                if (isLast) break;
            }

            return new Glyph(allContourEndIndices.ToArray(), allPoints.ToArray(), advanceWidth, leftSideBearing);
        }

        static (Glyph simpleGlyph, bool isLast) ReadNextComponentGlyph(Reader Reader, uint[] GlyphLocation)
        {
            uint flags = Reader.ReadUInt16();
            uint glyphIndex = Reader.ReadUInt16();

            uint previousLocation = Reader.GetULocation();

            Glyph Glyph = ReadSimpleGlyph(Reader, GlyphLocation, GlyphLocation[glyphIndex], 0, 0);
            Reader.GoTo(previousLocation);

            if (!FlagBitIsSet(flags, 1)) ; // args are point indices, not offsets
            double offsetX = FlagBitIsSet(flags, 0) ? Reader.ReadInt16() : Reader.ReadSByte();
            double offsetY = FlagBitIsSet(flags, 0) ? Reader.ReadInt16() : Reader.ReadSByte();
            double scaleX = 1; double scaleY = 1;

            if (FlagBitIsSet(flags, 3))
                scaleX = scaleY = Reader.ReadFixedPoint2Dot14();
            else if (FlagBitIsSet(flags, 6))
            {
                scaleX = Reader.ReadFixedPoint2Dot14();
                scaleY = Reader.ReadFixedPoint2Dot14();
            }
            else if (FlagBitIsSet(flags, 7)) ; // TODO 2x2 Matrix

            for (int i = 0; i < Glyph.Points.Length; i++)
            {
                Glyph.Points[i].position.X = (float)(Glyph.Points[i].position.X * scaleX + (offsetX / 10000));
                Glyph.Points[i].position.Y = (float)(Glyph.Points[i].position.Y * scaleY + (offsetY / 10000));
            }

            return (Glyph, !FlagBitIsSet(flags, 5));
        }

        public void Load()
        {
            int contourStartIndex = 0;
            List<List<Vector3>> contours = new();

            foreach (int contourEndIndex in this.contourEndIndices)
            {
                int numPointsInContour = contourEndIndex - contourStartIndex + 1;
                Span<Point> Points = this.Points.AsSpan(contourStartIndex, numPointsInContour);

                contours.Add(ContourToBezier(Points));

                contourStartIndex = contourEndIndex + 1;
            }

            List<List<Vector3>> outerContours = new();
            List<List<Vector3>> innerContours = new();
            foreach (List<Vector3> contourA in contours)
            {
                int intersections = 0;

                foreach (List<Vector3> contourB in contours)
                {
                    if (contourA == contourB)
                        continue;

                    intersections = Intersection(contourA.MaxBy(c => c.X), contourB, intersections);

                }

                if (intersections % 2 == 0)
                {
                    if (IsClockwise(contourA)) contourA.Reverse();
                    outerContours.Add(contourA);
                }
                else
                {
                    if (!IsClockwise(contourA)) contourA.Reverse();
                    innerContours.Add(contourA);
                }

            }

            contours.Clear();

            for (int c = 0; c < outerContours.Count; c++)
            {

                List<Vector3> contour = outerContours[c];
                List<List<Vector3>> contourHoles = new();

                foreach (List<Vector3> innerContour in innerContours)
                {
                    int intersections = Intersection(innerContour.MaxBy(point => point.X), contour, 0);

                    if (intersections % 2 == 1)
                    {
                        contourHoles.Add(innerContour);
                    }
                }

                if (!contourHoles.Any()) contours.Add(contour);

                var sortedHoles = contourHoles.OrderByDescending(h => h.Max(v => v.X)).ToList();

                foreach (List<Vector3> hole in sortedHoles)
                {

                    Vector3 vertex = hole.MaxBy(vertex => vertex.X);


                    (bool intersected, List<RayResult> results) = Intersection(vertex, contour.ToArray());
                    Vector3 bridgePoint = Vector3.Zero;


                    if (!intersected) continue;

                    List<Vector3> reflex = new();

                    RayResult edge = results.MinBy(e => e.t);

                    if (Vector3.Distance(edge.p0, edge.intersection) < 1e-6f)
                        bridgePoint = edge.p0;

                    else if (Vector3.Distance(edge.p1, edge.intersection) < 1e-6f)
                        bridgePoint = edge.p1;
                    else
                    {
                        bridgePoint = (edge.p0.X > edge.p1.X) ? edge.p0 : edge.p1;

                        for (int i = 0; i < contour.Count; i++)
                        {

                            Vector3 prev = contour[(i - 1 + contour.Count) % contour.Count];
                            Vector3 curr = contour[i];
                            Vector3 next = contour[(i + 1) % contour.Count];

                            Vector3 edgeA = curr - prev;
                            Vector3 edgeB = next - curr;

                            float cross = edgeA.X * edgeB.Y - edgeA.Y * edgeB.X;

                            if (IsClockwise(contour) && cross > 1e-6f || !IsClockwise(contour) && cross < 1e-6f)
                                reflex.Add(curr);
                        }

                        List<Vector3> inPoints = new();

                        foreach (Vector3 point in reflex)
                        {
                            if (PointOnTriangle(point, vertex, edge.intersection, bridgePoint))
                            {
                                inPoints.Add(point);
                            }
                        }

                        if (inPoints.Any())
                        {
                            float minAngle = float.MaxValue;
                            Vector3 visible = Vector3.Zero;

                            foreach (Vector3 point in inPoints)
                            {
                                Vector3 vp = vertex - point;
                                vp.Normalize();

                                float dotProduct = Vector3.Dot(new Vector3(1, 0, 0), vp);
                                float angle = (float)Math.Acos(dotProduct);

                                if (angle < minAngle)
                                {
                                    minAngle = angle;
                                    visible = point;
                                }
                            }

                            bridgePoint = visible;
                        }
                    }

                    List<Vector3> cutContour = new();

                    foreach (Vector3 point in contour)
                    {
                        cutContour.Add(point);

                        if (point == bridgePoint)
                        {
                            int startIndex = hole.IndexOf(vertex);
                            for (int i = 0; i < hole.Count; i++)
                            {
                                int idx = (startIndex + i) % hole.Count;
                                cutContour.Add(hole[idx]);
                            }

                            cutContour.Add(vertex);
                            cutContour.Add(bridgePoint);

                        }
                    }

                    contour = cutContour;
                }

                contours.Add(contour);
            }

            List<VertexPosition> allVertices = new();
            List<int> allIndices = new();
            int vertexOffset = 0;

            foreach (List<Vector3> contour in contours)
            {
                Vector3[] triangles = ContourToTriangle(contour);

                for (int i = 0; i < triangles.Length / 3; i++)
                {
                    allVertices.Add(new VertexPosition(triangles[i * 3 + 0]));
                    allVertices.Add(new VertexPosition(triangles[i * 3 + 1]));
                    allVertices.Add(new VertexPosition(triangles[i * 3 + 2]));

                    allIndices.Add((int)(vertexOffset + i * 3 + 0));
                    allIndices.Add((int)(vertexOffset + i * 3 + 1));
                    allIndices.Add((int)(vertexOffset + i * 3 + 2));
                }

                vertexOffset += triangles.Length;
            }

            index = allIndices.ToArray();
            vertex = allVertices.ToArray();
        }

        public struct CoordsOnCurve
        {
            public int[] coords;
            public bool[] onCurve;
        }



        static CoordsOnCurve ReadCoordinates(Reader Reader, byte[] flags, bool readingX)
        {
            CoordsOnCurve CoordsOnCurve = new CoordsOnCurve()
            {
                coords = new int[flags.Length],
                onCurve = new bool[flags.Length],
            };

            int offsetSizeFlagBit = readingX ? 1 : 2;
            int offsetSignOrSkipBit = readingX ? 4 : 5;


            for (int i = 0; i < CoordsOnCurve.coords.Length; i++)
            {
                CoordsOnCurve.coords[i] = CoordsOnCurve.coords[Math.Max(0, i - 1)];
                byte flag = flags[i];


                bool onCurve = FlagBitIsSet(flag, 0);
                CoordsOnCurve.onCurve[i] = onCurve;

                if (FlagBitIsSet(flag, offsetSizeFlagBit))
                {
                    byte offset = Reader.ReadByte();
                    int sign = FlagBitIsSet(flag, offsetSignOrSkipBit) ? 1 : -1;
                    CoordsOnCurve.coords[i] += (offset * sign);
                }
                else if (!FlagBitIsSet(flag, offsetSignOrSkipBit))
                {
                    short signedOffset = (short)Reader.ReadUInt16();
                    CoordsOnCurve.coords[i] += signedOffset;
                }

            }

            return CoordsOnCurve;
        }

        static bool FlagBitIsSet(byte flags, int bitIndex)
        {
            return ((flags >> bitIndex) & 1) == 1;
        }

        static bool FlagBitIsSet(uint flags, int bitIndex)
        {
            return ((flags >> bitIndex) & 1) == 1;
        }
    }
}
