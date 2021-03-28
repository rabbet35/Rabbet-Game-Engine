﻿using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using RabbetGameEngine.Models;
using RabbetGameEngine.Rendering;
namespace RabbetGameEngine.SubRendering
{
    public class BatchLerpLines : Batch
    {
        public BatchLerpLines(int renderLayer = 0) : base(RenderType.guiLines, renderLayer)
        {
        }

        protected override void buildBatch()
        {
            ShaderUtil.tryGetShader(ShaderUtil.lerpLinesName, out batchShader);
            batchShader.use();
            batchShader.setUniformMat4F("projectionMatrix", Renderer.projMatrix);
            maxBufferSizeBytes /= 3;
            vertices = new Vertex[RenderConstants.INIT_BATCH_ARRAY_SIZE];
            indices = new uint[RenderConstants.INIT_BATCH_ARRAY_SIZE];
            modelMatrices = new Matrix4[RenderConstants.INIT_BATCH_ARRAY_SIZE];
            VertexBufferLayout l12 = new VertexBufferLayout();
            Vertex.configureLayout(l12);
            vao.addBufferDynamic(RenderConstants.INIT_BATCH_ARRAY_SIZE * Vertex.SIZE_BYTES, l12);
            VertexBufferLayout matl5 = new VertexBufferLayout();
            matl5.add(VertexAttribPointerType.Float, 4);
            matl5.add(VertexAttribPointerType.Float, 4);
            matl5.add(VertexAttribPointerType.Float, 4);
            matl5.add(VertexAttribPointerType.Float, 4);
            matl5.add(VertexAttribPointerType.Float, 4);
            matl5.add(VertexAttribPointerType.Float, 4);
            matl5.add(VertexAttribPointerType.Float, 4);
            matl5.add(VertexAttribPointerType.Float, 4);
            vao.addBufferDynamic(RenderConstants.INIT_BATCH_ARRAY_SIZE * sizeof(float) * 16, matl5);
            vao.addIndirectBuffer(RenderConstants.INIT_BATCH_ARRAY_SIZE);
            vao.addIndicesBufferDynamic(RenderConstants.INIT_BATCH_ARRAY_SIZE);
            vao.drawType = PrimitiveType.Lines;
        }

        public override bool tryToFitInBatchModel(Model mod)
        {
            return false;
        }

        public override void updateBuffers()
        {
        
        }

        public override void updateUniforms(World thePlanet)
        {
            batchShader.setUniformMat4F("projectionMatrix", Renderer.projMatrix);
            batchShader.setUniformVec3F("fogColor", thePlanet.getFogColor());
            batchShader.setUniform1F("fogStart", thePlanet.getFogStart());
            batchShader.setUniform1F("fogEnd", thePlanet.getFogEnd());
        }

        public override void drawBatch(World thePlanet)
        {
            vao.bind();
            batchShader.use();
            batchShader.setUniform1F("percentageToNextTick", TicksAndFrames.getPercentageToNextTick());
            GL.MultiDrawElementsIndirect(PrimitiveType.Lines, DrawElementsType.UnsignedInt, System.IntPtr.Zero, requestedObjectItterator, 0);
            vao.unBind();
        }
    }
}