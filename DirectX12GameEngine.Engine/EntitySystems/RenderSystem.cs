using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using SharpDX.Direct3D12;

using Buffer = DirectX12GameEngine.Graphics.Buffer;
using CommandList = DirectX12GameEngine.Graphics.CommandList;

namespace DirectX12GameEngine.Engine
{
    public sealed class RenderSystem : EntitySystem<ModelComponent>
    {
        private const int MaxLights = 512;

        private readonly List<CommandList> commandLists = new List<CommandList>();

        private readonly Dictionary<Model, (CompiledCommandList[] Bundles, Buffer[] WorldMatrixBuffers)> models = new Dictionary<Model, (CompiledCommandList[], Buffer[])>();

        public unsafe RenderSystem(IServiceProvider services) : base(services, typeof(TransformComponent))
        {
            Components.CollectionChanged += Components_CollectionChanged;

            DirectionalLightGroupBuffer = Buffer.Constant.New(GraphicsDevice, sizeof(int) + sizeof(DirectionalLightData) * MaxLights);
            GlobalBuffer = Buffer.Constant.New(GraphicsDevice, sizeof(GlobalBuffer));
            ViewProjectionTransformBuffer = Buffer.Constant.New(GraphicsDevice, sizeof(StereoViewProjectionTransform));
        }

        public Buffer DirectionalLightGroupBuffer { get; }

        public Buffer GlobalBuffer { get; }

        public Buffer ViewProjectionTransformBuffer { get; }

        public override void Draw(GameTime gameTime)
        {
            UpdateGlobals();
            UpdateLights();
            UpdateViewProjectionMatrices();

            var componentsWithSameModel = Components.GroupBy(m => m.Model).ToArray();

            int batchCount = Math.Min(Environment.ProcessorCount, componentsWithSameModel.Length);
            int batchSize = (int)Math.Ceiling((double)componentsWithSameModel.Length / batchCount);

            Texture? depthStencilBuffer = GraphicsDevice.CommandList.DepthStencilBuffer;
            Texture[] renderTargets = GraphicsDevice.CommandList.RenderTargets;
            var viewports = GraphicsDevice.CommandList.Viewports;
            var scissorRectangles = GraphicsDevice.CommandList.ScissorRectangles;

            for (int i = commandLists.Count; i < batchCount; i++)
            {
                CommandList commandList = new CommandList(GraphicsDevice, CommandListType.Direct);
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
                            newWorldMatrixBuffers[meshIndex] = Buffer.Constant.New(GraphicsDevice, modelComponents.Count() * 16 * sizeof(float));
                        }

                        CompiledCommandList[] newBundles = new CompiledCommandList[highestPassCount];

                        for (int passIndex = 0; passIndex < highestPassCount; passIndex++)
                        {
                            CommandList bundleCommandList = new CommandList(GraphicsDevice, CommandListType.Bundle);

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
                                unsafe
                                {
                                    worldMatrixBuffers[meshIndex].Map(0);
                                    Matrix4x4 worldMatrix = model.Meshes[meshIndex].WorldMatrix * modelComponent.Entity.Transform.WorldMatrix;
                                    MemoryHelper.Copy(worldMatrix, worldMatrixBuffers[meshIndex].MappedResource + modelComponentIndex * sizeof(Matrix4x4));
                                    worldMatrixBuffers[meshIndex].Unmap(0);
                                }
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

            GraphicsDevice.CommandList.Flush();
            GraphicsDevice.ExecuteCommandLists(compiledCommandLists);

            GraphicsDevice.CommandList.Reset();
            GraphicsDevice.CommandList.ClearState();

            GraphicsDevice.CommandList.SetRenderTargets(depthStencilBuffer, renderTargets);
            GraphicsDevice.CommandList.SetViewports(viewports);
            GraphicsDevice.CommandList.SetScissorRectangles(scissorRectangles);
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
            int renderTargetCount = GraphicsDevice.Presenter is null ? 1 : GraphicsDevice.Presenter.PresentationParameters.Stereo ? 2 : 1;
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

        private void UpdateGlobals()
        {
            GlobalBuffer globalBuffer = new GlobalBuffer
            {
                ElapsedTime = (float)Game.Time.Elapsed.TotalSeconds,
                TotalTime = (float)Game.Time.Total.TotalSeconds
            };

            GlobalBuffer.SetData(globalBuffer);
        }

        private unsafe void UpdateLights()
        {
            LightSystem? lightSystem = SceneSystem.Systems.Get<LightSystem>();

            if (lightSystem is null) return;

            var lights = lightSystem.Lights;
            int lightCount = lights.Count;

            DirectionalLightData[] lightData = new DirectionalLightData[lights.Count];

            for (int i = 0; i < lights.Count; i++)
            {
                lightData[i] = new DirectionalLightData { Color = lights[i].Color, Direction = lights[i].Direction };
            }

            DirectionalLightGroupBuffer.Map(0);
            MemoryHelper.Copy(lightCount, DirectionalLightGroupBuffer.MappedResource);
            MemoryHelper.Copy(lightData.AsSpan(), DirectionalLightGroupBuffer.MappedResource + sizeof(Vector4));
            DirectionalLightGroupBuffer.Unmap(0);
        }

        private void UpdateViewProjectionMatrices()
        {
            if (SceneSystem.CurrentCamera != null && SceneSystem.CurrentCamera.Entity != null)
            {
#if WINDOWS_UWP
                if (GraphicsDevice.Presenter is Graphics.Holographic.HolographicGraphicsPresenter graphicsPresenter)
                {
                    var cameraPose = graphicsPresenter.HolographicFrame.CurrentPrediction.CameraPoses[0];

                    cameraPose.HolographicCamera.SetNearPlaneDistance(SceneSystem.CurrentCamera.NearPlaneDistance);
                    cameraPose.HolographicCamera.SetFarPlaneDistance(SceneSystem.CurrentCamera.FarPlaneDistance);

                    var viewTransform = cameraPose.TryGetViewTransform(graphicsPresenter.SpatialStationaryFrameOfReference.CoordinateSystem);

                    StereoViewProjectionTransform stereoViewProjectionTransform = new StereoViewProjectionTransform();

                    if (viewTransform.HasValue)
                    {
                        Matrix4x4.Decompose(SceneSystem.CurrentCamera.Entity.Transform.WorldMatrix, out _,
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
#endif
                {
                    Matrix4x4.Invert(SceneSystem.CurrentCamera.ViewMatrix, out Matrix4x4 inverseViewMatrix);

                    ViewProjectionTransform viewProjectionTransform = new ViewProjectionTransform
                    {
                        ViewMatrix = SceneSystem.CurrentCamera.ViewMatrix,
                        InverseViewMatrix = inverseViewMatrix,
                        ProjectionMatrix = SceneSystem.CurrentCamera.ProjectionMatrix,
                        ViewProjectionMatrix = SceneSystem.CurrentCamera.ViewProjectionMatrix
                    };

                    StereoViewProjectionTransform stereoViewProjectionTransform = new StereoViewProjectionTransform { Left = viewProjectionTransform, Right = viewProjectionTransform };

                    ViewProjectionTransformBuffer.SetData(stereoViewProjectionTransform);
                }
            }
        }

        private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (ModelComponent modelComponent in e.NewItems)
                    {
                        if (modelComponent.Model != null && models.TryGetValue(modelComponent.Model, out var tuple))
                        {
                            models.Remove(modelComponent.Model);
                            DisposeModel(tuple.Bundles, tuple.WorldMatrixBuffers);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (ModelComponent modelComponent in e.OldItems)
                    {
                        if (modelComponent.Model != null && models.TryGetValue(modelComponent.Model, out var tuple))
                        {
                            models.Remove(modelComponent.Model);
                            DisposeModel(tuple.Bundles, tuple.WorldMatrixBuffers);
                        }
                    }
                    break;
            }
        }

        private struct StereoViewProjectionTransform
        {
            public ViewProjectionTransform Left;

            public ViewProjectionTransform Right;
        }
    }
}
