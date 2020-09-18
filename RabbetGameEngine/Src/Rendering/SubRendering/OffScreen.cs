﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using RabbetGameEngine.Models;
using System;

namespace RabbetGameEngine.SubRendering
{
    /*This static class will be used for rendering scenes to an off-screen buffer for super sampling and other effects.*/
    public static class OffScreen
    {
        private static ModelDrawable screenQuad;
        private static Shader screenShader;
        private static readonly string screenShaderName = "Offscreen.shader";
        private static int texColorBuffer;
        private static int depthBuffer;
        private static int frameBuffer;
        private static int width;
        private static int height;
        private static FramebufferErrorCode errorCode;

        public static void init(float superSamplingMultiplyer = 1.0F)
        {
            Application.debug("Rendering is utilizing offscreen buffer!");
            frameBuffer = GL.GenFramebuffer();
            depthBuffer = GL.GenRenderbuffer();
            texColorBuffer = GL.GenTexture();

            width = (int)(GameInstance.realScreenWidth * superSamplingMultiplyer);//*2 on width and height for 4x super sampling
            height = (int)(GameInstance.realScreenHeight * superSamplingMultiplyer);

            GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);


            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent32, width, height);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texColorBuffer, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.ClearColor(1.0F, 0, 0, 1.0F);//setting clear color for this framebuffer to red because it should not be able to be seen

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            if ((errorCode = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer)) != FramebufferErrorCode.FramebufferComplete)
            {
                Application.error("Offscreen could not initialize offscreen frame buffers!");
            }
            else if(errorCode == FramebufferErrorCode.FramebufferComplete)
            {
                Application.debug("Offscreen frame buffer successfully initialized!");
            }

            ShaderUtil.tryGetShader(screenShaderName, out screenShader);
            screenQuad = (ModelDrawable)QuadPrefab.getNewModelDrawable().scaleVertices(new Vector3(2.0F, 2.0F, 1.0F));//scale quad to fit screen
            screenQuad.setShader(screenShader);
        }

        public static void prepareToRenderToOffScreenTexture()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            GL.Viewport(0, 0, width, height);
        }

        public static void renderOffScreenTexture()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, GameInstance.gameWindowWidth, GameInstance.gameWindowHeight);
            GL.ClearColor(1.0F, 0, 0, 1.0F);//setting clear color for this framebuffer to red because it should not be able to be seen
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.DepthTest);
            GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
            screenQuad.draw(false);
        }

        public static int getWidth { get => width; }
        public static int getHeight { get => height; }
    }
}
