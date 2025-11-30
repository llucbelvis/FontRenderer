using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Text
{
    public class Camera
    {
        public Vector3 position;

        public float zoom = 1f;

        public Viewport Viewport;

        public static Camera Generate(Viewport Viewport)
        {
            Camera Camera = new Camera()
            {
                Viewport = Viewport,
            };

            return Camera;
        }

        public void Zoom(MouseState CurrentState, MouseState PreviousState)
        {
            float scroll = CurrentState.ScrollWheelValue - PreviousState.ScrollWheelValue;

            zoom = Math.Clamp(zoom - scroll / 3000f, 0f, 6f);
        }

        public void Move(ProjectInput.Input W, ProjectInput.Input A, ProjectInput.Input S, ProjectInput.Input D)
        {
            if (W.press)
                position.Y -= 0.01f * zoom;

            if (S.press)
                position.Y += 0.01f * zoom;
            if (A.press)
                position.X += 0.01f * zoom;

            if (D.press)
                position.X -= 0.01f * zoom;
        }
    }
}
