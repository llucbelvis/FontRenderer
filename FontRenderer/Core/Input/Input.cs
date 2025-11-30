using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;

namespace Text
{
    public class ProjectInput
    {
        public struct Input
        {
            public bool click = false, cancel = false, press = false;

            public Input(bool click, bool cancel, bool press)
            {
                this.click = click;
                this.cancel = cancel;
                this.press = press;
            }


        }

        public enum MouseButton
        {
            Left,
            Right,
            Middle,
            XButton1,
            XButton2,
        }

        public Dictionary<Keys, Input> KeyboardKeyMap = new();
        public Dictionary<MouseButton, Input> MouseButtonMap = new();

        public ProjectInput()
        {

            foreach (Keys key in Enum.GetValues<Keys>())
            {
                KeyboardKeyMap.Add(key, default);
            }

            foreach (MouseButton button in Enum.GetValues<MouseButton>())
            {
                MouseButtonMap.Add(button, default);
            }

        }
        public void Update(ProjectMouse ProjectMouse, ProjectKeyboard ProjectKeyboard)
        {
            foreach (Keys key in KeyboardKeyMap.Keys)
            {
                KeyboardKeyMap[key] = KeyboardKeyState(ProjectKeyboard.CurrentState, ProjectKeyboard.PreviousState, key);
            }

            foreach (MouseButton button in MouseButtonMap.Keys)
            {
                MouseButtonMap[button] = MouseButtonState(ProjectMouse.CurrentState, ProjectMouse.PreviousState, button);
            }
        }
        public Input MouseButtonState(MouseState CurrentMouseState, MouseState PreviousMouseState, MouseButton button)
        {
            return new Input(
                IdentifyButton(CurrentMouseState, button) == ButtonState.Pressed && IdentifyButton(PreviousMouseState, button) != ButtonState.Pressed ? true : false,
                IdentifyButton(CurrentMouseState, button) != ButtonState.Pressed && IdentifyButton(PreviousMouseState, button) == ButtonState.Pressed ? true : false,
                IdentifyButton(CurrentMouseState, button) == ButtonState.Pressed ? true : false
            );
        }


        public Input KeyboardKeyState(KeyboardState CurrentKeyboardState, KeyboardState PreviousKeyboardState, Keys key)
        {
            return new Input(
                CurrentKeyboardState.IsKeyDown(key) && !PreviousKeyboardState.IsKeyDown(key) ? true : false,
                !CurrentKeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyDown(key) ? true : false,
                CurrentKeyboardState.IsKeyDown(key) ? true : false
            );
        }

        private ButtonState IdentifyButton(MouseState MouseState, MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => MouseState.LeftButton,
                MouseButton.Right => MouseState.RightButton,
                MouseButton.Middle => MouseState.MiddleButton,
                MouseButton.XButton1 => MouseState.XButton1,
                MouseButton.XButton2 => MouseState.XButton2,
                _ => ButtonState.Released

            };

        }
    }
}