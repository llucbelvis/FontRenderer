using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Text
{
    public class Main : Game
    {
        private GraphicsDeviceManager GraphicsDeviceManager;
        Font Font;
        Effect Default;
        Effect Glyph;
        Camera Camera;


        List<String> strings;

        ProjectInput Input = new();

        ProjectKeyboard ProjectKeyboard = new();
        ProjectMouse ProjectMouse = new();

        SpriteBatch SpriteBatch;
        SpriteFont SpriteFont;

        List<double> previousFramerate = new();
        public Main()
        {
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
            GraphicsDeviceManager.SynchronizeWithVerticalRetrace = false;

            GraphicsDeviceManager.PreferredBackBufferWidth = 1920;
            GraphicsDeviceManager.PreferredBackBufferHeight = 1080;

            GraphicsDeviceManager.IsFullScreen = false;
            GraphicsDeviceManager.PreferMultiSampling = true;
            IsFixedTimeStep = false;
            IsMouseVisible = true;

            GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef;

            GraphicsDeviceManager.PreparingDeviceSettings += (sender, settings) =>
            {
                settings.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 0;
            };

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            Font = new Font(GraphicsDevice, "C:\\Users\\llucb\\source\\repos\\FontRenderer\\FontRenderer\\Font\\Roboto-Regular.ttf");
            Camera = Camera.Generate(GraphicsDevice.Viewport);
            Default = Content.Load<Effect>("Default");

            SpriteBatch = new(GraphicsDevice);
            SpriteFont = Content.Load<SpriteFont>("Arial");

            base.Initialize();
        }

        protected override void LoadContent()
        {
           
        }

        MouseState previous = Mouse.GetState();
        protected override void Update(GameTime gameTime)
        {





            ProjectMouse.Current(Mouse.GetState(), Camera, Camera);
            ProjectKeyboard.Current(Keyboard.GetState());

            Input.Update(ProjectMouse, ProjectKeyboard);

            if (Input.KeyboardKeyMap[Keys.Z].press)
            {
                GraphicsDevice.RasterizerState = new RasterizerState()
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.WireFrame,

                };
            }
            else
            {
                GraphicsDevice.RasterizerState = new RasterizerState()
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid,

                };
            }

            Camera.Move(Input.KeyboardKeyMap[Keys.W], Input.KeyboardKeyMap[Keys.A], Input.KeyboardKeyMap[Keys.S], Input.KeyboardKeyMap[Keys.D]);
            Camera.Zoom(ProjectMouse.CurrentState, ProjectMouse.PreviousState);

            ProjectMouse.Previous(Mouse.GetState());
            ProjectKeyboard.Previous(Keyboard.GetState());

            base.Update(gameTime);

        }

        public int Framerate(GameTime GameTime)
        {
            double currentFPS = 1 / GameTime.ElapsedGameTime.TotalSeconds;

            if (previousFramerate.Count > 10000f)
                previousFramerate.RemoveAt(0);

            previousFramerate.Add(currentFPS);

            double increment = 0;

            foreach (double num in previousFramerate)
                increment += num;

            increment /= previousFramerate.Count;



            return (int)increment;
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);


            new String(GraphicsDevice, new Vector3(0, -0.1f, 0), new Vector3(0, 0, 0), 0, Font, "AV").Draw(GraphicsDevice, Camera, Default);


            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;


            base.Draw(gameTime);
        }
    }
}
