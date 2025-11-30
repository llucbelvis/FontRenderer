using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Text
{
    public class ProjectMouse
    {
        public MouseState CurrentState;
        public MouseState PreviousState;

        public Vector3 currentPosition;
        public Vector3 interfacePosition;
        public Vector3 previousPosition;

        public int direction;
        public int currentSection = 0;
        public int previousSection = 1;

        public void Update(Camera Camera)
        {


        }

        public void Current(MouseState MouseState, Camera ContentCamera, Camera InterfaceCamera)
        {
            CurrentState = MouseState;

            currentPosition = ToWorld(ContentCamera, new Vector3(MouseState.X, MouseState.Y, 0));
            interfacePosition = ToWorld(InterfaceCamera, new Vector3(MouseState.X, MouseState.Y, 0));
        }

        public void Previous(MouseState MouseState)
        {
            PreviousState = MouseState;
        }

        public static Vector3 ToWorld(Camera Camera, Vector3 position)
        {
            return Camera.Viewport.Unproject(position, Matrix.CreateOrthographic(((float)Camera.Viewport.Width / 1000) * Camera.zoom, ((float)Camera.Viewport.Height / 1000) * Camera.zoom, 0, 1), Matrix.CreateTranslation(Camera.position), Matrix.Identity);
        }
    }
}