﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using RabbetGameEngine.Debugging;
using RabbetGameEngine.GUI;
using RabbetGameEngine.Models;
using RabbetGameEngine.SubRendering;
using System.Drawing;

namespace RabbetGameEngine
{

    /*This class will be responsable for most of the games rendering requests. It will then send the requests to the suitable sub renderers.
      e.g, when the game requests text to be rendered on the screen, the renderer will send a request to the TextRenderer2D.
      e.g, when the game requests entity models to be rendered in the world, the renderer will send a request to the model draw function.
      This class also contains the projection matrix.*/
    public static class Renderer//TODO: Reduce driver overhead closer to zero
    {
        private static int privateTotalDrawCallCount;
        private static Matrix4 projectionMatrix;
        public static readonly bool useOffScreenBuffer = false;

        private static bool prevFullscreenBool;//used to check if the fullscreen setting has changed
        private static Rectangle preFullScreenSize;//used to store the window dimentions before going into full screen

        /*Called before any rendering is done*/
        public static void init()
        {
            ShaderUtil.loadAllFoundShaderFiles();
            TextureUtil.loadAllFoundTextureFiles();
            ModelUtil.loadAllFoundModelFiles();
            Application.debug("Loaded " + ShaderUtil.getShaderCount() + " shaders.");
            Application.debug("Loaded " + TextureUtil.getTextureCount() + " textures.");
            Application.debug("Loaded " + ModelUtil.getModelCount() + " models.");

            GL.Viewport(preFullScreenSize = GameInstance.get.ClientRectangle);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.VertexProgramPointSize);//allows shaders for GL_POINTS to change size of points.
            GL.Enable(EnableCap.PointSprite);           //allows shaders for GL_POINTS to change point fragments (opentk exclusive)
            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Equal, 1);
            GL.LineWidth(3);
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathUtil.radians(GameSettings.fov), GameInstance.aspectRatio, 0.1F, 1000.0F);
            if(useOffScreenBuffer) OffScreen.init();

            prevFullscreenBool = GameSettings.fullscreen;
        }

        /*Called each time the game window is resized*/
        public static void onResize()
        {
            GL.Viewport(GameInstance.get.ClientRectangle);
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView((float)MathUtil.radians(GameSettings.fov), GameInstance.aspectRatio, 0.1F, 1000.0F);
            GUIHandler.onWindowResize();
        }

        /*Called before all draw calls*/
        private static void preRender()
        {
            if (useOffScreenBuffer) OffScreen.prepareToRenderToOffScreenTexture();
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }
        
        public static void renderAll()
        {
            Profiler.beginEndProfile(Profiler.renderingName);
            preRender();
            renderWorld();
            GUIHandler.drawCurrentGUIScreen();
            postRender();
            Profiler.beginEndProfile(Profiler.renderingName);
        }
        private static void renderWorld()
        {
            privateTotalDrawCallCount = 0;
            GameInstance.get.currentPlanet.drawEntities(GameInstance.get.thePlayer.getViewMatrix(), projectionMatrix);
            GameInstance.get.currentPlanet.drawVFX(GameInstance.get.thePlayer.getViewMatrix(), projectionMatrix);
            GameInstance.get.currentPlanet.getGroundModel().draw(GameInstance.get.thePlayer.getViewMatrix(), projectionMatrix, GameInstance.get.currentPlanet.getFogColor());
            GameInstance.get.currentPlanet.getWallsModel().draw(GameInstance.get.thePlayer.getViewMatrix(), projectionMatrix, GameInstance.get.currentPlanet.getFogColor());
            GameInstance.get.currentPlanet.getSkyboxModel().draw(GameInstance.get.thePlayer.getViewMatrix(), projectionMatrix, GameInstance.get.currentPlanet.getSkyColor(), GameInstance.get.currentPlanet.getFogColor());
            if(GameSettings.drawHitboxes)HitboxRenderer.renderAll(GameInstance.get.thePlayer.getViewMatrix(), projectionMatrix);
        }

        /*Called after all draw calls*/
        private static void postRender()
        {
            if (useOffScreenBuffer) OffScreen.renderOffScreenTexture();
            GameInstance.get.SwapBuffers();
        }

        public static int getAndResetTotalDrawCount()
        {
            int result = privateTotalDrawCallCount;
            privateTotalDrawCallCount = 0;
            return result;
        }

        public static void onToggleFullscreen()
        {
            if (prevFullscreenBool != GameSettings.fullscreen)
            {
                prevFullscreenBool = GameSettings.fullscreen;
                if (GameSettings.fullscreen)
                {
                    preFullScreenSize = GameInstance.get.ClientRectangle;
                    GameInstance.get.WindowState = WindowState.Fullscreen;
                }
                else
                {
                    GameInstance.get.WindowState = WindowState.Normal;
                    GameInstance.get.ClientRectangle = preFullScreenSize;
                }
            }
        }

        /*deletes all loaded opengl assets*/
        public static void onClosing()
        {
            ShaderUtil.deleteAll();
            TextureUtil.deleteAll();
            ModelUtil.deleteAll();
        }

        public static Matrix4 projMatrix { get => projectionMatrix; }
        public static int totalDraws { get { return privateTotalDrawCallCount; } set { privateTotalDrawCallCount = value; } }
    }
}
 