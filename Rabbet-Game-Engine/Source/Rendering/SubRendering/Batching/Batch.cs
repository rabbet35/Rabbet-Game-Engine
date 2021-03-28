﻿using OpenTK.Mathematics;
using RabbetGameEngine.Models;
using RabbetGameEngine.Rendering;
using System;

namespace RabbetGameEngine.SubRendering
{
    //TODO: replace look at camera function in iSphere shaders with new faster one. (found in mathutil and in moons.shader)
    public class Batch
    { 
        protected int maxBufferSizeBytes = RenderConstants.MAX_BATCH_BUFFER_SIZE_BYTES;
        protected int maxIndiciesCount;
        protected int maxVertexCount;
        protected int maxPositionCount;
        protected int maxPointCount;
        protected int maxSprite3DCount;
        protected int maxMatrixCount;
        protected int maxDrawCommandCount;

        /// <summary>
        /// true if this batch requires transparency sorting
        /// </summary>
        protected bool requiresSorting = false;

        protected bool hasBeenUsed = false;

        protected VertexArrayObject vao;
        protected Vertex[] vertices;
        protected uint[] indices;

        protected PointParticle[] batchedPoints = null;

        protected Matrix4[] modelMatrices = null;

        protected Vector3[] positions = null;

        protected Vector3[] scales = null;

        protected Sprite3D[] sprites3D = null;

        protected DrawCommand[] drawCommands = null;

        protected Texture[] batchTextures = null;

        //TODO: Move all itterators and arrays and shit to their own batch class where they belong

        /// <summary>
        /// number of individual objects requested. This must be used as an identifier for each vertex of 
        /// the individual objects so the shader can determine which model matrices to use to transform it.
        /// </summary>
        protected int requestedObjectItterator = 0;

        /// <summary>
        /// Used for properly interlacing and including new requests for lerp triangle types which require 2 matrices per object
        /// </summary>
        protected int matricesItterator = 0;

        /// <summary>
        /// Used for properly interlacing and including new requests for lerp points which require 2 points per point.
        /// </summary>
        protected int pointsItterator = 0;

        /// <summary>
        /// Used for properly interlacing and including new requests for lerp 3d text or any other type which uses 2 positions per object.
        /// </summary>
        protected int positionItterator = 0;

        protected int spriteItterator = 0;

        /// <summary>
        /// number of vertices requested to be added to this batch since the last update.
        /// </summary>
        protected int requestedVerticesCount = 0;

        /// <summary>
        /// number of indices requested to be added to this batch since the last update
        /// </summary>
        protected int requestedIndicesCount = 0;

        /// <summary>
        /// Number of textures added to this batch
        /// </summary>
        protected int requestedTextures = 0;

        /// <summary>
        /// Will be true if this batch can not fit any more textures
        /// </summary>
        protected bool texturesFull = false;

        protected RenderType batchType;
        protected Shader batchShader;

        protected int renderLayer = 0;

        public Batch(RenderType type, int renderLayer = 0)
        {
            this.renderLayer= renderLayer;
            batchType = type;
            batchTextures = new Texture[RenderConstants.MAX_BATCH_TEXTURES];
            vao = new VertexArrayObject();
            vao.beginBuilding();
            buildBatch();
            vao.finishBuilding();
            calculateBatchLimitations();
            hasBeenUsed = true;
        }

        protected virtual void buildBatch()
        {

        }

        /// <summary>
        /// returns True if this batch accepts and successfully fit the point cloud
        /// </summary>
        /// <param name="mod">the point cloud</param>
        public virtual bool tryToFitInBatchPoints(PointCloudModel mod)
        {
            return false;
        }

        /// <summary>
        /// Returns true if this batch accepts and successfully fits the provided model
        /// </summary>
        /// <param name="mod">The model</param>
        public virtual bool tryToFitInBatchModel(Model mod)
        {
            return false;
        }

        /// <summary>
        /// Returns true if this batch accepts and successfully fits the provided point particle
        /// </summary>
        /// <param name="p">The Point particle</param>
        public virtual bool tryToFitInBatchSinglePoint(PointParticle p)
        {
            return false;
        }

        /// <summary>
        /// Returns true if this batch accepts and successfully fits the provided lerped point particle
        /// </summary>
        /// <param name="p">the point particle</param>
        /// <param name="prevP">the point particle's previous update data</param>
        public virtual bool tryToFitInBatchLerpPoint(PointParticle p, PointParticle prevP)
        {
            return false;
        }
        /// <summary>
        /// Returns true if this batch accepts and successfully fits the provided Sprite3D
        /// </summary>
        /// <param name="s">the 3d sprite</param>
        public virtual bool tryToFitInBatchSprite3D(Sprite3D s)
        {
            return false;
        }

        /// <summary>
        /// Updates this batches vao buffers with all requested render data, should be done after each render update.
        /// </summary>
        public virtual void updateBuffers()
        {

        }

        /// <summary>
        /// Updates the uniforms for this batches shader. Should be called after each render update before rendering.
        /// This is for uniforms which only need to be updated once per update, not per frame.
        /// </summary>
        /// /// <param name="thePlanet">The current planet being rendered</param>
        public virtual void updateUniforms(World thePlanet)
        {

        }

        /// <summary>
        /// Renders this batch based on variables in the provided planet.
        /// </summary>
        /// <param name="thePlanet">The current planet being rendered</param>
        public virtual void drawBatch(World thePlanet)
        {

        }

        protected void bindAllTextures()
        {

        }

        /// <summary>
        /// Should be called after dividing maxBufferSizeBytes to accomidate all buffer objects
        /// </summary>
        protected void calculateBatchLimitations()
        {
            maxIndiciesCount = maxBufferSizeBytes / sizeof(uint);
            maxVertexCount = maxBufferSizeBytes / Vertex.SIZE_BYTES;
            maxDrawCommandCount = maxBufferSizeBytes / DrawCommand.SIZE_BYTES;
            maxMatrixCount = maxBufferSizeBytes / (sizeof(float) * 16);
            maxPositionCount = maxBufferSizeBytes / (sizeof(float) * 3);
            maxSprite3DCount = maxBufferSizeBytes / Sprite3D.sizeInBytes;
            maxPointCount = maxBufferSizeBytes / PointParticle.SIZE_BYTES;
        }

        /// <summary>
        /// Adds a drawcommand to buffers at the requestedobjectitterator based on given parameters
        /// </summary>
        /// <param name="objIndCount">Number of indices of the object</param>
        /// <param name="quads">True if the object is made of seperate quads (such as 3d text)</param>
        public void configureDrawCommandsForCurrentObject(int objIndCount, bool quads)
        {
            int n;
            if((n = requestedObjectItterator + 1) >= drawCommands.Length)//resizing drawcommands
            {
                if((n*=2) >= maxDrawCommandCount)
                {
                    Array.Resize<DrawCommand>(ref drawCommands, maxDrawCommandCount);
                    vao.resizeIndirect(maxDrawCommandCount);
                }
                else
                {
                    Array.Resize<DrawCommand>(ref drawCommands, n);
                    vao.resizeIndirect(n);
                }
            }

            if(quads)
            drawCommands[requestedObjectItterator] = new DrawCommand((uint)(objIndCount), (uint)(1), (uint)(0), (uint)(requestedVerticesCount), (uint)(requestedObjectItterator));
            else
            drawCommands[requestedObjectItterator] = new DrawCommand((uint)(objIndCount), (uint)(1), (uint)(requestedIndicesCount), (uint)(requestedVerticesCount), (uint)(requestedObjectItterator));
        }

        /// <summary>
        /// Resets all itteratiors and counts to 0 to prepare for new render update
        /// </summary>
        public virtual void reset()
        {
            requestedVerticesCount = 0;
            requestedIndicesCount = 0;
            requestedObjectItterator = 0;
            matricesItterator = 0;
            pointsItterator = 0;
            positionItterator = 0;
            spriteItterator = 0;
        }

        public virtual void preRednerUpdate()
        {
            reset();
        }

        public virtual void postRenderUpdate()
        {
            updateBuffers();
            hasBeenUsed = false;
        }

        public RenderType getRenderType()
        {
            return this.batchType;
        }

        public Texture getBatchtexture(int index)
        {
            if (index >= RenderConstants.MAX_BATCH_TEXTURES) return null;
            return batchTextures[index];
        }

        public bool hasBeenUsedSinceLastUpdate()
        {
            return hasBeenUsed;
        }

        public void deleteVAO()
        {
            vao.delete();
        }

        public bool containsTexture(Texture tex)
        {
            foreach(Texture t in batchTextures)
            {
                if (t == tex) return true;
            }
            return false;
        }

        public Batch addTexture(Texture tex)
        {
            if (texturesFull) return this;
            batchTextures[requestedTextures++] = tex;
            texturesFull = requestedTextures >= RenderConstants.MAX_BATCH_TEXTURES;
            return this;
        }

    }
}