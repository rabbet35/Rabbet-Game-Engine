﻿using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using RabbetGameEngine.Models;

namespace RabbetGameEngine
{
    public class StaticRenderObject
    {
        private VertexArrayObject VAO = null;
        private Texture tex = null;
        private Shader shader = null;
        private Model mod = null;
        private PointParticle[] points = null;
        private PrimitiveType type;
        private bool pointBased = false;

        private StaticRenderObject(string texture, string shader, Model data, PrimitiveType drawType)
        {
            TextureUtil.tryGetTexture(texture, out tex);
            ShaderUtil.tryGetShader(shader, out this.shader);
            this.mod = data;
            type = drawType;
            
            switch(type)
            {
                case PrimitiveType.Triangles:
                    VAO = VertexArrayObject.createStaticTriangles(data.vertices, data.indices);
                    break;
                case PrimitiveType.Lines:
                    VAO = VertexArrayObject.createStaticLines(data.vertices, data.indices);
                    break;
            }
        }
        private StaticRenderObject(PointParticle[] data, bool transparency)
        {
            if (transparency)
            {
                ShaderUtil.tryGetShader(ShaderUtil.iSpheresTransparentName, out this.shader);
            }
            else
            {
                ShaderUtil.tryGetShader(ShaderUtil.iSpheresName, out this.shader);
            }
            this.points = data;
            type = PrimitiveType.TriangleStrip;
            pointBased = true;
            VAO = VertexArrayObject.createStaticPoints(points);
        }

        public static StaticRenderObject createSROTriangles(string texture, string shader, Model data)
        {
            return new StaticRenderObject(texture, shader, data, PrimitiveType.Triangles);
        }

        public static StaticRenderObject createSROLines(string texture, string shader, Model data)
        {
            return new StaticRenderObject(texture, shader, data, PrimitiveType.Lines);
        }

        public static StaticRenderObject createSROPoints(PointParticle[] data, bool transparency)
        {
            return new StaticRenderObject(data, transparency);
        }

        public void draw(Matrix4 viewMatrix, Vector3 fogColor)
        {
            VAO.bindVaoVboIbo();
            shader.use();
            if (tex != null)
            {
                tex.use();
            }
            shader.setUniformMat4F("projectionMatrix", Renderer.projMatrix);
            shader.setUniformMat4F("orthoMatrix", Renderer.orthoMatrix);
            shader.setUniformMat4F("viewMatrix", viewMatrix);
            shader.setUniformVec3F("fogColor", fogColor);
            shader.setUniform1F("fogDensity", GameInstance.get.currentPlanet.getFogDensity());
            shader.setUniform1F("fogGradient", GameInstance.get.currentPlanet.getFogGradient());
            shader.setUniform1F("percentageToNextTick", TicksAndFrames.getPercentageToNextTick());

            if (pointBased)
            {
                VAO.bindInstVBO();
                GL.DrawArraysInstanced(VAO.getPrimType(), 0, 4, points.Length);
                return;
            }
            else
            {
                GL.DrawElements(type, mod.indices.Length, DrawElementsType.UnsignedInt, 0);
                return;
            }
        }

        public void delete()
        {
            VAO.delete();
        }
    }
}
