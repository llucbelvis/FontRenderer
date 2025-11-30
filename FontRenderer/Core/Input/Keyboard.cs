using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

namespace Text
{
    public class ProjectKeyboard
    {
        public KeyboardState CurrentState;
        public KeyboardState PreviousState;

        public void Update()
        {


        }
        public void Current(KeyboardState KeyboardState)
        {
            CurrentState = KeyboardState;
        }

        public void Previous(KeyboardState KeyboardState)
        {
            PreviousState = KeyboardState;
        }
    }
}
