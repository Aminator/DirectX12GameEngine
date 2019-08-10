using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;

using Buffer = DirectX12GameEngine.Graphics.Buffer;
using CommandList = DirectX12GameEngine.Graphics.CommandList;

namespace DirectX12GameEngine.Engine
{
    public sealed class RenderSystem : EntitySystem<ModelComponent>
    {
        private const int MaxLights = 512;

        private readonly GraphicsDevice graphicsDevice;
        private readonly SceneSystem sceneSystem;

        private readonly List<CommandList> commandLists = new List<CommandList>();

        private readonly Dictionary<Model, (CompiledCommandList[] Bundles, Buffer[] WorldMatrixBuffers)> models = new Dictionary<Model, (CompiledCommandList[], Buffer[])>();

        public RenderSystem(GraphicsDevice device, SceneSystem sceneSystem) : base(typeof(TransformComponent))
        {
            graphicsDevice = device;
            this.sceneSystem = sceneSystem;

            if (graphicsDevice is null) throw new InvalidOperationException();

            DirectionalLightGroupBuffer = Buffer.Constant.New(graphicsDevice, sizeof(int) + Unsafe.SizeOf<DirectionalLightData>() * MaxLights);
            GlobalBuffer = Buffer.Constant.New(graphicsDevice, Unsafe.SizeOf<GlobalBuffer>());
            ViewProjectionTransformBuffer = Buffer.Constant.New(graphicsDevice, Unsafe.SizeOf<StereoViewProjectionTransform>());
        }

        public Buffer DirectionalLightGroupBuffer { get; }

        public Buffer GlobalBuffer { get; }

        public Buffer ViewProjectionTransformBuffer { get; }

        public override void Draw(GameTime gameTime)
        {
            if (graphicsDevice is null) return;

            UpdateGlobals(gameTime);
            UpdateLights();
            UpdateViewProjectionMatrices();

            var componentsWithSameModel = Components.GroupBy(m => m.Model).ToArray();

            if (componentsWithSameModel.Length < 1) return;

            int batchCount = Math.Min(Environment.ProcessorCount, componentsWithSameModel.Length);
            int batchSize = (int)Math.Ceiling((double)componentsWithSameModel.Length / batchCount);

            Texture? depthStencilBuffer = graphicsDevice.CommandList.DepthStencilBuffer;
            Texture[] renderTargets = graphicsDevice.CommandList.RenderTargets;
            var viewports = graphicsDevice.CommandList.Viewports;
            var scissorRectangles = graphicsDevice.CommandList.ScissorRectangles;

            for (int i = commandLists.Count; i < batchCount; i++)
            {
                CommandList commandList = new CommandList(graphicsDevice, CommandListType.Direct);
                commandList.Close();

                commandLists.Add(commandList);
            }

            CompiledCommandList[] compiledCommandLists = new CompiledCommandList[batchCount];

            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++) /*Parallel.For(0, batchCount, batchIndex =>*/
            {
                CommandList commandList = commandLists[batchIndex];

                commandList.Reset();
                commandList.ClearState();

                commandList.SetRenderTargets(depthStencilBuffer, renderTargets);
                commandList.SetViewports(viewports);
                commandList.SetScissorRectangles(scissorRectangles);

                int end = Math.Min((batchIndex * batchSize) + batchSize, componentsWithSameModel.Length);

                for (int i = batchIndex * batchSize; i < end; i++)
                {
                    Model? model = componentsWithSameModel[i].Key;
                    var modelComponents = componentsWithSameModel[i];

                    if (model is null || model.Meshes.Count == 0 || model.Materials.Count == 0) continue;

                    int meshCount = model.Meshes.Count;
                    int highestPassCount = model.Materials.Max(m => m.Passes.Count);

                    if (!models.ContainsKey(model))
                    {
                        Buffer[] newWorldMatrixBuffers = new Buffer[meshCount];

                        for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
                        {
                            newWorldMatrixBuffers[meshIndex] = Buffer.Constant.New(graphicsDevice, modelComponents.Count() * 16 * sizeof(float));
                        }

                        CompiledCommandList[] newBundles = new CompiledCommandList[highestPassCount];

                        for (int passIndex = 0; passIndex < highestPassCount; passIndex++)
                        {
                            CommandList bundleCommandList = new CommandList(graphicsDevice, CommandListType.Bundle);

                            RecordCommandList(
                                model,
                                bundleCommandList,
                                newWorldMatrixBuffers,
                                modelComponents.Count(),
                                passIndex);

                            CompiledCommandList? bundle = bundleCommandList.Close();

                            if (bundle != null)
                            {
                                newBundles[passIndex] = bundle;
                            }
                        }

                        models.Add(model, (newBundles, newWorldMatrixBuffers));
                    }

                    (CompiledCommandList[] bundles, Buffer[] worldMatrixBuffers) = models[model];

                    int modelComponentIndex = 0;

                    foreach (ModelComponent modelComponent in modelComponents)
                    {
                        if (modelComponent.Entity != null)
                        {
                            for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
                            {
                                Matrix4x4 worldMatrix = model.Meshes[meshIndex].WorldMatrix * modelComponent.Entity.Transform.WorldMatrix;
                                worldMatrixBuffers[meshIndex].SetData(worldMatrix, modelComponentIndex * Unsafe.SizeOf<Matrix4x4>());
                            }
                        }

                        modelComponentIndex++;
                    }

                    for (int passIndex = 0; passIndex < highestPassCount; passIndex++)
                    {
                        commandList.BeginRenderPass();

                        // Without bundles:
                        //RecordCommandList(model, commandList, worldMatrixBuffers, modelComponents.Count(), passIndex);

                        commandList.ExecuteBundle(bundles[passIndex]);

                        commandList.EndRenderPass();
                    }
                }

                compiledCommandLists[batchIndex] = commandList.Close();
            }

            graphicsDevice.CommandList.Flush();
            graphicsDevice.ExecuteCommandLists(compiledCommandLists);

            graphicsDevice.CommandList.Reset();
            graphicsDevice.CommandList.ClearState();

            graphicsDevice.CommandList.SetRenderTargets(depthStencilBuffer, renderTargets);
            graphicsDevice.CommandList.SetViewports(viewports);
            graphicsDevice.CommandList.SetScissorRectangles(scissorRectangles);
        }

        public override void Dispose()
        {
            ViewProjectionTransformBuffer.Dispose();

            foreach (CommandList commandList in commandLists)
            {
                commandList.Dispose();
            }

            foreach (var item in models)
            {
                (CompiledCommandList[]? bundles, Buffer[]? worldMatrixBuffers) = item.Value;

                DisposeModel(bundles, worldMatrixBuffers);
            }
        }

        protected override void OnEntityComponentAdded(ModelComponent component)
        {
            if (component.Model != null && models.TryGetValue(component.Model, out var tuple))
            {
                models.Remove(component.Model);
                DisposeModel(tuple.Bundles, tuple.WorldMatrixBuffers);
            }
        }

        protected override void OnEntityComponentRemoved(ModelComponent component)
        {
            if (component.Model != null && models.TryGetValue(component.Model, out var tuple))
            {
                models.Remove(component.Model);
                DisposeModel(tuple.Bundles, tuple.WorldMatrixBuffers);
            }
        }

        private static void DisposeModel(CompiledCommandList[] bundles, Buffer[] worldMatrixBuffers)
        {
            if (worldMatrixBuffers != null)
            {
                foreach (Buffer constantBuffer in worldMatrixBuffers)
                {
                    constantBuffer.Dispose();
                }
            }

            if (bundles != null)
            {
                foreach (CompiledCommandList bundle in bundles)
                {
                    //bundle.Builder.Dispose();
                }
            }
        }

        private void RecordCommandList(Model model, CommandList commandList, Buffer[] worldMatrixBuffers, int instanceCount, int passIndex)
        {
            int renderTargetCount = graphicsDevice?.Presenter is null ? 1 : graphicsDevice.Presenter.PresentationParameters.Stereo ? 2 : 1;
            instanceCount *= renderTargetCount;

            for (int i = 0; i < model.Meshes.Count; i++)
            {
                Mesh mesh = model.Meshes[i];
                Material material = model.Materials[mesh.MaterialIndex];

                if (passIndex >= material.Passes.Count) continue;

                MaterialPass materialPass = material.Passes[passIndex];

                if (mesh.MeshDraw.VertexBufferViews is null) throw new ArgumentException("The vertex buffer views of the mesh cannot be null.");

                commandList.SetIndexBuffer(mesh.MeshDraw.IndexBufferView);
                commandList.SetVertexBuffers(0, mesh.MeshDraw.VertexBufferViews);

                commandList.SetPipelineState(materialPass.PipelineState);
                commandList.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

                int rootParameterIndex = 0;

                commandList.SetGraphicsRoot32BitConstant(rootParameterIndex++, renderTargetCount, 0);
                commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, GlobalBuffer);
                commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, ViewProjectionTransformBuffer);
                commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, worldMatrixBuffers[i]);

                commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, DirectionalLightGroupBuffer);

                if (materialPass.NativeConstantBufferGpuDescriptorHandle.Ptr != 0)
                {
                    commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, materialPass.NativeConstantBufferGpuDescriptorHandle);
                }

                if (materialPass.NativeSamplerGpuDescriptorHandle.Ptr != 0)
                {
                    commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, materialPass.NativeSamplerGpuDescriptorHandle);
                }

                if (materialPass.NativeTextureGpuDescriptorHandle.Ptr != 0)
                {
                    commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, materialPass.NativeTextureGpuDescriptorHandle);
                }

                if (mesh.MeshDraw.IndexBufferView.HasValue)
                {
                    commandList.DrawIndexedInstanced(mesh.MeshDraw.IndexBufferView.Value.SizeInBytes / SharpDX.DXGI.FormatHelper.SizeOfInBytes(mesh.MeshDraw.IndexBufferView.Value.Format), instanceCount);
                }
                else
                {
                    commandList.DrawInstanced(mesh.MeshDraw.VertexBufferViews[0].SizeInBytes / mesh.MeshDraw.VertexBufferViews[0].StrideInBytes, instanceCount);
                }
            }
        }

        private void UpdateGlobals(GameTime gameTime)
        {
            GlobalBuffer globalBuffer = new GlobalBuffer
            {
                ElapsedTime = (float)gameTime.Elapsed.TotalSeconds,
                TotalTime = (float)gameTime.Total.TotalSeconds
            };

            GlobalBuffer.SetData(globalBuffer);
        }

        private void UpdateLights()
        {
            LightSystem? lightSystem = EntityManager?.Systems.Get<LightSystem>();

            if (lightSystem is null) return;

            int lightCount = lightSystem.Lights.Count;

            DirectionalLightData[] lightData = new DirectionalLightData[lightCount];

            int lightIndex = 0;

            foreach (LightComponent light in lightSystem.Lights)
            {
                lightData[lightIndex] = new DirectionalLightData { Color = light.Color, Direction = light.Direction };
                lightIndex++;
            }

            DirectionalLightGroupBuffer.SetData(lightCount);
            DirectionalLightGroupBuffer.SetData(lightData.AsSpan(), Unsafe.SizeOf<Vector4>());
        }

        private void UpdateViewProjectionMatrices()
        {
            CameraComponent? currentCamera = sceneSystem.CurrentCamera;

            if (currentCamera != null && currentCamera.Entity != null)
            {
                if (graphicsDevice?.Presenter is Graphics.Holographic.HolographicGraphicsPresenter graphicsPresenter)
                {
                    var cameraPose = graphicsPresenter.HolographicFrame.CurrentPrediction.CameraPoses[0];

                    cameraPose.HolographicCamera.SetNearPlaneDistance(currentCamera.NearPlaneDistance);
                    cameraPose.HolographicCamera.SetFarPlaneDistance(currentCamera.FarPlaneDistance);

                    var viewTransform = cameraPose.TryGetViewTransform(graphicsPresenter.SpatialStationaryFrameOfReference.CoordinateSystem);

                    StereoViewProjectionTransform stereoViewProjectionTransform = new StereoViewProjectionTransform();

                    if (viewTransform.HasValue)
                    {
                        Matrix4x4.Decompose(currentCamera.Entity.Transform.WorldMatrix, out _,
                            out Quaternion rotation,
                            out Vector3 translation);

                        Matrix4x4 positionMatrix = Matrix4x4.CreateTranslation(-translation) * Matrix4x4.CreateFromQuaternion(rotation);

                        stereoViewProjectionTransform.Left.ViewMatrix = positionMatrix * viewTransform.Value.Left;
                        stereoViewProjectionTransform.Right.ViewMatrix = positionMatrix * viewTransform.Value.Right;

                        Matrix4x4.Invert(stereoViewProjectionTransform.Left.ViewMatrix, out Matrix4x4 inverseViewMatrix0);
                        Matrix4x4.Invert(stereoViewProjectionTransform.Right.ViewMatrix, out Matrix4x4 inverseViewMatrix1);

                        stereoViewProjectionTransform.Left.InverseViewMatrix = inverseViewMatrix0;
                        stereoViewProjectionTransform.Right.InverseViewMatrix = inverseViewMatrix1;
                    }

                    stereoViewProjectionTransform.Left.ProjectionMatrix = cameraPose.ProjectionTransform.Left;
                    stereoViewProjectionTransform.Right.ProjectionMatrix = cameraPose.ProjectionTransform.Right;

                    stereoViewProjectionTransform.Left.ViewProjectionMatrix = stereoViewProjectionTransform.Left.ViewMatrix * stereoViewProjectionTransform.Left.ProjectionMatrix;
                    stereoViewProjectionTransform.Right.ViewProjectionMatrix = stereoViewProjectionTransform.Right.ViewMatrix * stereoViewProjectionTransform.Right.ProjectionMatrix;

                    ViewProjectionTransformBuffer.SetData(stereoViewProjectionTransform);
                }
                else
                {
                    Matrix4x4.Invert(currentCamera.ViewMatrix, out Matrix4x4 inverseViewMatrix);

                    ViewProjectionTransform viewProjectionTransform = new ViewProjectionTransform
                    {
                        ViewMatrix = currentCamera.ViewMatrix,
                        InverseViewMatrix = inverseViewMatrix,
                        ProjectionMatrix = currentCamera.ProjectionMatrix,
                        ViewProjectionMatrix = currentCamera.ViewProjectionMatrix
                    };

                    StereoViewProjectionTransform stereoViewProjectionTransform = new StereoViewProjectionTransform { Left = viewProjectionTransform, Right = viewProjectionTransform };

                    ViewProjectionTransformBuffer.SetData(stereoViewProjectionTransform);
                }
            }
        }

        private struct StereoViewProjectionTransform
        {
            public ViewProjectionTransform Left;

            public ViewProjectionTransform Right;
        }
    }
}
