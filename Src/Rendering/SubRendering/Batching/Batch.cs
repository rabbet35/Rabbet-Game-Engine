﻿using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using RabbetGameEngine.Models;
using System;

namespace RabbetGameEngine.SubRendering
{
    //TODO: Fix point cloud batching and rendering
    //TODO: Render lerp points as cheated spherical billboard sprites and measure performance diff.
    public class Batch
    {
        public static readonly int initialArraySize = 32;
        public static readonly int maxBufferSizeBytes = 8388608;
        public static readonly int maxIndiciesCount = maxBufferSizeBytes / sizeof(uint);
        public static readonly int maxVertexCount = maxBufferSizeBytes / Vertex.vertexByteSize;
        public static readonly int maxPositionCount = maxBufferSizeBytes / (sizeof(float) * 3);
        public static readonly int maxPointCount = maxBufferSizeBytes / PointParticle.pParticleByteSize;
        public static readonly int maxMatrixCount = maxBufferSizeBytes / (sizeof(float) * 16);
        public static readonly int maxDrawCommandCount = maxBufferSizeBytes / DrawCommand.sizeInBytes;

        private bool usingLerpPoints = false;
        private bool usesPositions = false;
        private bool usesPrevPositions = false;

        /// <summary>
        /// true if this batch requires transparency sorting
        /// </summary>
        public bool requiresSorting = false;

        /// <summary>
        /// true if this batch should be rendered last.
        /// </summary>
        public bool transparentGUI = false;
        private VertexArrayObject VAO;
        private Model batchedModel = null;

        /*points are stored in a single VBO half packed, meaning the prevTickBatchedPoints start in the second half of the array. all the batchedPoints come first, and then the prevTickBatchedPoints are packed after them.*/
        private PointParticle[] batchedPoints = null;

        /*Matrices are stored in a single VBO interlaced. all the modelMatrices come first, and then the prevTickModelMatrices are packed after them.*/
        private Matrix4[] modelMatrices = null;

        /*If using prev positions for lerping, they will be stored in a single vbo interlaced. Otherwise if positions are being used, they are stored flat in a single vbo.*/
        private Vector3[] positions = null;
   

        private DrawCommand[] drawCommands = null;

        private bool usesMultiDrawIndirect = false;
        private bool usesLerpMatrices = false;
        private bool pointBased = false;

        /// <summary>
        /// number of individual objects requested. This must be used as an identifier for each vertex of 
        /// the individual objects so the shader can determine which model matrices to use to transform it.
        /// </summary>
        private int requestedObjectItterator = 0;

        /// <summary>
        /// Used for properly interlacing and including new requests for lerp triangle types which require 2 matrices per object
        /// </summary>
        private int matricesItterator = 0;

        /// <summary>
        /// Used for properly interlacing and including new requests for lerp points which require 2 points per point.
        /// </summary>
        private int pointsItterator = 0;

        /// <summary>
        /// Used for properly interlacing and including new requests for lerp 3d text or any other type which uses 2 positions per object.
        /// </summary>
        private int positionItterator = 0;


        /// <summary>
        /// number of vertices requested to be added to this batch since the last update.
        /// </summary>
        private int requestedVerticesCount = 0;

        /// <summary>
        /// number of indices requested to be added to this batch since the last update
        /// </summary>
        private int requestedIndicesCount = 0;

        private BatchType batchType;
        private Texture batchTex;
        private Shader batchShader;

        public Batch(BatchType type, Texture tex)
        {
            if(type == BatchType.lerpISpheres || type == BatchType.lerpISpheresTransparent)
            {
                Application.error("Wrong batch constructor being used for batch of type points!!!");
                return;
            }
            batchType = type;
            batchTex = tex;
            VAO = VertexArrayObject.createDynamic(batchType, maxBufferSizeBytes);
            batchedModel = new Model(new Vertex[initialArraySize], new uint[initialArraySize]);
            initializeBatchFormat();
        }

        public Batch(bool pointTransparency, bool lerp)
        {
            pointBased = true;
            if(pointTransparency)
            {
                if(!lerp)
                {
                    batchType = BatchType.iSpheresTransparent;
                    ShaderUtil.tryGetShader(ShaderUtil.iSpheresTransparentName, out batchShader);
                    requiresSorting = true;
                    VAO = VertexArrayObject.createDynamic(batchType, maxBufferSizeBytes);
                    batchedPoints = new PointParticle[initialArraySize];
                    return;
                }
                else
                {
                    batchType = BatchType.lerpISpheresTransparent;
                    ShaderUtil.tryGetShader(ShaderUtil.lerpISpheresTransparentName, out batchShader);
                    VAO = VertexArrayObject.createDynamic(batchType, maxBufferSizeBytes);
                    requiresSorting = true;
                    usingLerpPoints = true;
                    batchedPoints = new PointParticle[initialArraySize];
                    return;
                }
            }
            else
            {
                if (!lerp)
                {
                    batchType = BatchType.iSpheres;
                    ShaderUtil.tryGetShader(ShaderUtil.iSpheresName, out batchShader);
                    VAO = VertexArrayObject.createDynamic(batchType, maxBufferSizeBytes);
                    batchedPoints = new PointParticle[initialArraySize];
                    return;
                }
                else
                {
                    batchType = BatchType.lerpISpheres;
                    ShaderUtil.tryGetShader(ShaderUtil.lerpISpheresName, out batchShader);
                    usingLerpPoints = true;
                    VAO = VertexArrayObject.createDynamic(batchType, maxBufferSizeBytes);
                    batchedPoints = new PointParticle[initialArraySize];
                    return;
                }
            }
           
        }

        private void initializeBatchFormat()
        {
            switch (batchType)
            {
                case BatchType.none:
                    ShaderUtil.tryGetShader("debug", out batchShader);
                    break;

                case BatchType.guiCutout:
                    ShaderUtil.tryGetShader(ShaderUtil.guiCutoutName, out batchShader);
                    break;

                case BatchType.guiText:
                    ShaderUtil.tryGetShader(ShaderUtil.text2DName, out batchShader);
                    transparentGUI = true;
                    break;

                case BatchType.text3D:
                    ShaderUtil.tryGetShader(ShaderUtil.text3DName, out batchShader);
                    positions = new Vector3[initialArraySize];
                    drawCommands = new DrawCommand[initialArraySize];
                    usesMultiDrawIndirect = true;
                    usesPositions = true;
                    break;

                case BatchType.lerpText3D:
                    ShaderUtil.tryGetShader(ShaderUtil.lerpText3DName, out batchShader);
                    positions = new Vector3[initialArraySize];
                    drawCommands = new DrawCommand[initialArraySize];
                    usesMultiDrawIndirect = true;
                    usesPositions = true;
                    usesPrevPositions = true;
                    break;

                case BatchType.triangles:
                    ShaderUtil.tryGetShader(ShaderUtil.trianglesName, out batchShader);
                    break;

                case BatchType.trianglesTransparent:
                    ShaderUtil.tryGetShader(ShaderUtil.trianglesTransparentName, out batchShader);
                    requiresSorting = true;
                    break;

                case BatchType.lines:
                    ShaderUtil.tryGetShader(ShaderUtil.linesName, out batchShader);
                    break;

                case BatchType.lerpTriangles:
                    ShaderUtil.tryGetShader(ShaderUtil.lerpTrianglesName, out batchShader);
                    modelMatrices = new Matrix4[initialArraySize];
                    drawCommands = new DrawCommand[initialArraySize];
                    usesMultiDrawIndirect = true;
                    usesLerpMatrices = true;
                    break;

                case BatchType.lerpTrianglesTransparent:
                    ShaderUtil.tryGetShader(ShaderUtil.lerpTrianglesTransparentName, out batchShader);
                    modelMatrices = new Matrix4[initialArraySize];
                    drawCommands = new DrawCommand[initialArraySize];
                    usesMultiDrawIndirect = true;
                    usesLerpMatrices = true;
                    requiresSorting = true;
                    break;

                case BatchType.lerpLines:
                    ShaderUtil.tryGetShader(ShaderUtil.lerpLinesName, out batchShader);
                    modelMatrices = new Matrix4[initialArraySize];
                    drawCommands = new DrawCommand[initialArraySize];
                    usesMultiDrawIndirect = true;
                    usesLerpMatrices = true;
                    break;
            }
        }

        //For dynamic vertex objects, when submitting data, the residual un-updated data at the end of the buffer does not need to be cleared.
        //use the submittedVerticesCount or something similar with drawElements(count) to only draw the submitted vertices and ignore the residual ones.
        //This is faster than clearing the whole buffer each update.
        /// <summary>
        /// attempts to add the provided model data to the batch. Returns true if successful, and returns false if not enough room.
        /// </summary>
        /// <param name="theModel">The model to be added</param>
        /// <returns>true if the provided model can be added</returns>
        public bool addToBatch(Model theModel)
        {
            //if this batch cant fit the new models vertices or indices
            int n;
            int ind;
            if ( (n = requestedVerticesCount + theModel.vertices.Length) >= maxVertexCount || (ind = requestedIndicesCount + theModel.indices.Length) >= maxIndiciesCount)
            {
                return false;
            }
            
            if(n >= batchedModel.vertices.Length)//resize vertices array
            {
                if ((n *= 2) >= maxVertexCount)
                {
                    Array.Resize<Vertex>(ref batchedModel.vertices, maxVertexCount);
                }
                else
                {
                    Array.Resize<Vertex>(ref batchedModel.vertices, n);
                }
            }

            if(ind >= batchedModel.indices.Length)//resize Indices array
            {
                if ((ind *= 2) >= maxIndiciesCount)
                {
                    Array.Resize<uint>(ref batchedModel.indices, maxIndiciesCount);
                }
                else
                {
                    Array.Resize<uint>(ref batchedModel.indices, ind);
                }
            }

            if (usesMultiDrawIndirect)
            { 
                if(usesLerpMatrices)//resizing matrices array
                {
                    n = matricesItterator + 2;
                    if (n >= maxMatrixCount) return false;
                    if(n >= modelMatrices.Length)
                    {
                        if((n *= 2) >= maxMatrixCount)
                        {
                            Array.Resize<Matrix4>(ref modelMatrices, maxMatrixCount);
                        }
                        else
                        {
                            Array.Resize<Matrix4>(ref modelMatrices, n);
                        }
                    }
                    modelMatrices[matricesItterator] = theModel.modelMatrix;
                    modelMatrices[matricesItterator + 1] = theModel.prevModelMatrix;
                    matricesItterator += 2;
                }
                else if(usesPositions)//resizing positions array
                {
                    if(usesPrevPositions)
                    {
                        if ((n = positionItterator + 2) >= maxPositionCount) return false;
                        if(n >= positions.Length)
                        {
                            if((n *= 2) >= maxMatrixCount)
                            {
                                Array.Resize<Vector3>(ref positions, maxPositionCount);
                            }
                            else
                            {
                                Array.Resize<Vector3>(ref positions, n);
                            }
                        }
                        positions[positionItterator] = theModel.worldPos;
                        positions[positionItterator + 1] = theModel.prevWorldPos;
                        positionItterator += 2;
                    }
                    else
                    {
                        if ((n = positionItterator + 1) > maxPositionCount) return false;
                        if (n > positions.Length)
                        {
                            if ((n *= 2) > maxMatrixCount)
                            {
                                Array.Resize<Vector3>(ref positions, maxPositionCount);
                            }
                            else
                            {
                                Array.Resize<Vector3>(ref positions, n);
                            }
                        }
                        positions[positionItterator++] = theModel.worldPos;
                    }
                }
                if (requestedObjectItterator + 1 >= maxDrawCommandCount) return false;
                configureDrawCommandsForCurrentObject(theModel.indices.Length, theModel.vertices.Length);
                Array.Copy(theModel.indices, 0, batchedModel.indices, requestedIndicesCount, theModel.indices.Length);
            }
            else
            {
                for (int i = 0; i < theModel.indices.Length; ++i)
                {
                    batchedModel.indices[requestedIndicesCount + i] = (uint)(theModel.indices[i] + requestedVerticesCount);
                }

            }
            
            Array.Copy(theModel.vertices, 0, batchedModel.vertices, requestedVerticesCount, theModel.vertices.Length);

            requestedVerticesCount += theModel.vertices.Length;
            requestedIndicesCount += theModel.indices.Length;
            requestedObjectItterator++;
            return true;
        }

        /// <summary>
        /// attempts to add the provided single point to the batch. This method only works for 
        /// batches with have a batch type of single points!
        /// </summary>
        /// <param name="singlePoint">The single point</param>
        /// <param name="prevTickSinglePoint">The single point's previous tick data.</param>
        /// <returns>true if this batch is able to accept the point.</returns>
        public bool addToBatch(PointParticle singlePoint, PointParticle prevTickSinglePoint)
        {
            int n;
            if((n = pointsItterator + 2) >= maxPointCount)
            {
                return false;
            }

            if(n >= batchedPoints.Length)//resizing batched lerp points
            {
                if((n *= 2) >= maxPointCount)
                {
                    Array.Resize<PointParticle>(ref batchedPoints, maxPointCount);
                }
                else
                {
                    Array.Resize<PointParticle>(ref batchedPoints, n);
                }
            }
            batchedPoints[pointsItterator] = singlePoint;
            batchedPoints[pointsItterator + 1] = prevTickSinglePoint;
            pointsItterator += 2;
            return true;
        }

        public bool addToBatch(PointParticle singlePoint)
        {
            int n;
            if ((n = pointsItterator + 1) >= maxPointCount)
            {
                return false;
            }

            if (n >= batchedPoints.Length)//resizing batched points
            {
                if ((n *= 2) >= maxPointCount)
                {
                    Array.Resize<PointParticle>(ref batchedPoints, maxPointCount);
                }
                else
                {
                    Array.Resize<PointParticle>(ref batchedPoints, n);
                }
            }

            batchedPoints[pointsItterator] = singlePoint;
            ++pointsItterator;
            return true;
        }

        /// <summary>
        /// attempts to add the provided single point to the batch. This method only works for 
        /// batches with have a batch type of single points!
        /// </summary>
        /// <param name="singlePoint">The single point</param>
        /// <param name="prevTickSinglePoint">The single point's previous tick data.</param>
        /// <returns>true if this batch is able to accept the point.</returns>
        public bool addToBatch(PointCloudModel theModel)
        {
            int n;
            int c;
            if(usingLerpPoints)
            {
                if((n = pointsItterator + (c = theModel.points.Length + theModel.prevPoints.Length)) >= maxPointCount)
                {
                    return false;
                }

                if(n >= batchedPoints.Length)//resizing batched points array
                {
                    if((n *= 2) >= maxPointCount)
                    {
                        Array.Resize<PointParticle>(ref batchedPoints, maxPointCount);
                    }
                    else
                    {
                        Array.Resize<PointParticle>(ref batchedPoints, n);
                    }
                }
                n = 0;
                //interlace points
                //TODO: impl
                pointsItterator += c;
                return true;
            }
            else
            {
                if ((n = pointsItterator + theModel.prevPoints.Length) >= maxPointCount)
                {
                    return false;
                }

                if(n >= batchedPoints.Length)//resizing batched points array
                {
                    if((n*=2) >= maxPointCount)
                    {
                        Array.Resize<PointParticle>(ref batchedPoints, maxPointCount);
                    }
                    else
                    {
                        Array.Resize<PointParticle>(ref batchedPoints, n);
                    }
                }
            }
            Array.Copy(theModel.points, 0, batchedPoints, pointsItterator, theModel.points.Length);
            pointsItterator += theModel.points.Length;
            return true;
        }

        /// <summary>
        /// Sets up and configures vertexattribptrs and vertexattribdivisors for the current requested object.
        /// This is required for LERP type batches for selecting the correct matrices.
        /// </summary>
        private void configureDrawCommandsForCurrentObject(int objIndCount, int vertCount)
        {
            int n;
            if((n = requestedObjectItterator + 1) >= drawCommands.Length)//resizing drawcommands
            {
                if((n*=2) >= maxDrawCommandCount)
                {
                    Array.Resize<DrawCommand>(ref drawCommands, maxDrawCommandCount);
                }
                else
                {
                    Array.Resize<DrawCommand>(ref drawCommands, n);
                }
            }

            drawCommands[requestedObjectItterator] = new DrawCommand((uint)(objIndCount), (uint)(1), (uint)(requestedIndicesCount), (uint)(requestedVerticesCount), (uint)(requestedObjectItterator));
        }

        public void onTickStart()
        {
            requestedVerticesCount = 0;
            requestedIndicesCount = 0;
            requestedObjectItterator = 0;
            matricesItterator = 0;
            pointsItterator = 0;
            positionItterator = 0;
        }

        /// <summary>
        /// After each tick, all the batches must submit their updated render data to the GPU to prepare for all draw calls.
        /// </summary>
        public void onTickEnd()
        {
            if (usesMultiDrawIndirect)
            {
                if(usesLerpMatrices)
                {
                    VAO.bindMatricesVBO();
                    //interlacing both arrays into one to submit to gpu
                    //using matricesItterator to only interlace and submit the necessary amount of data.
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (requestedObjectItterator * 2) * (sizeof(float) * 16), modelMatrices);
                }
                else if(usesPositions)
                {
                    VAO.bindPBO();
                    if(usesPrevPositions)
                    {
                        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, (requestedObjectItterator * 2) * (sizeof(float) * 3), positions);
                    }
                    else
                    {
                        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, requestedObjectItterator * (sizeof(float) * 3), positions);
                    }
                }
               
                
                VAO.bindIndirectBufferObject();
                GL.BufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, requestedObjectItterator * DrawCommand.sizeInBytes, drawCommands);
            }

            VAO.bindVBO();
            VAO.bindIBO();

            if(!pointBased)
            {
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, requestedVerticesCount * Vertex.vertexByteSize, batchedModel.vertices);
                GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, requestedIndicesCount * sizeof(uint), batchedModel.indices);
            }
            else 
            {
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, pointsItterator  *  PointParticle.pParticleByteSize, batchedPoints);
            }
        }
        
        public void draw( Matrix4 viewMatrix, Vector3 fogColor)
        {
            VAO.bindVaoVboIbo();
            batchShader.use();
            batchShader.setUniformMat4F("projectionMatrix", Renderer.projMatrix);
            batchShader.setUniformMat4F("viewMatrix", viewMatrix);
            batchShader.setUniformMat4F("orthoMatrix", Renderer.orthoMatrix);
            batchShader.setUniformVec3F("cameraPos", Renderer.camPos);
            batchShader.setUniformVec3F("fogColor", fogColor);
            batchShader.setUniform1F("fogDensity", GameInstance.get.currentPlanet.getFogDensity());
            batchShader.setUniform1F("fogGradient", GameInstance.get.currentPlanet.getFogGradient());
            batchShader.setUniform1F("percentageToNextTick", TicksAndFrames.getPercentageToNextTick());
            if(pointBased)
            {
                VAO.bindInstVBO();
                GL.DrawArraysInstanced(VAO.getPrimType(), 0, 4, pointsItterator);
                return;
            }

            if (batchTex != null && !batchTex.getIsNone())
            {
                batchTex.use();
            }

            if (usesMultiDrawIndirect)
            {
                if(usesLerpMatrices)
                {
                    VAO.bindMatricesVBO();
                }
                else if(usesPositions)
                {
                    VAO.bindPBO();
                }

                VAO.bindIndirectBufferObject();
                GL.MultiDrawElementsIndirect(VAO.getPrimType(), DrawElementsType.UnsignedInt, IntPtr.Zero, requestedObjectItterator, 0);
                return;
            }
            
            GL.DrawElements(VAO.getPrimType(), requestedIndicesCount, DrawElementsType.UnsignedInt, 0);
            return;
        }

        public BatchType getBatchType()
        {
            return this.batchType;
        }

        public Texture getBatchtexture()
        {
            return batchTex;
        }


        public bool hasBeenUsedInCurrentTick()
        {
            if(pointBased)
            {
                return pointsItterator > 0;
            }
            return requestedVerticesCount > 0;
        }
        public void delete()
        {
            VAO.delete();
        }
    }
}
