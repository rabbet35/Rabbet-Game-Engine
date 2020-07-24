﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using FredsMath;
namespace FredrickTechDemo
{
    class GameInstance : GameWindow
    {
        private TicksAndFps tickFps;
        private Input input;
        private Renderer renderer;

        public GameInstance(int width, int height, String title) : base(width, height, GraphicsMode.Default, title)
        {
            tickFps = new TicksAndFps(30.0D);
            input = new Input(this);
            renderer = new Renderer(this);
            tickFps.update();
        }

        #region overriding OpenTk base game methods
        protected override void OnUpdateFrame(FrameEventArgs args)//overriding OpenTk game update function, called every frame.
        {
            base.OnUpdateFrame(args);

            for(int i = 0; i< tickFps.getTicksElapsed(); i++)//for each tick that has elapsed since the start of last update, run the games logic enough times to catch up. 
            {
                onTick();
            }
            tickFps.update();
        }

        protected override void OnRenderFrame(FrameEventArgs args)//overriding OpenTk render update function, called every frame.
        {
            renderer.preRender();
            renderer.renderJaredsQuad();
            renderer.postRender();
            base.OnRenderFrame(args);
        }

        protected override void OnLoad(EventArgs e)
        {
            tickFps.init();
            renderer.init();
            base.OnLoad(e);
        }
        #endregion

        #region Every tick
        private void onTick()//insert game logic here
        {
            input.updateInput();
        }
        #endregion

    }
}
