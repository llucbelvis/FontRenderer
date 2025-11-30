using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.Linq;

namespace Text
{
    internal class Bezier
    {

        public static Vector3 Quadratic(Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return (float)Math.Pow((1f - t), 2f) * p1 + ((2f * (1f - t)) * t * p2) + ((float)Math.Pow(t, 2f) * p3);
        }

        public static Vector3 Cubic(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
        {
            return (float)Math.Pow((1 - t), 3) * p1 + 3 * (float)Math.Pow((1 - t), 2) * t * p2 + 3 * (1 - t) * (float)Math.Pow(t, 2) * p3 + (float)Math.Pow(t, 3f) * p4;
        }
    }
}
