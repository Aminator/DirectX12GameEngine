using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Lights;
using SharpDX.Direct3D12;

using CommandList = DirectX12GameEngine.Graphics.CommandList;

namespace DirectX12GameEngine.Engine
{
    public sealed class RenderSystem : EntitySystem<ModelComponent>
    {
        private const int MaxLights = 512;

        private readonly List<CommandList> commandLists = new List<CommandList>();

        private readonly Dictionary<Model, (CompiledCommandList[]?, Texture[]?)> models = new Dictionary<Model, (CompiledCommandList[]?, Texture[]?)>();

        public unsafe RenderSystem(IServiceProvider services) : base(services, typeof(TransformComponent))
        {
            Span<ViewProjectionTransform> viewProjectionTransforms = stackalloc ViewProjectionTransform[2];
            ViewProjectionTransformBuffer = Texture.CreateConstantBufferView(GraphicsDevice, viewProjectionTransforms);

            DirectionalLightGroupBuffer = Texture.CreateConstantBufferView(GraphicsDevice, sizeof(int) + sizeof(DirectionalLightData) * MaxLights);
        }

        public Texture ViewProjectionTransformBuffer { get; }

        public Texture DirectionalLightGroupBuffer { get; }

        public override void Draw(TimeSpan deltaTime)
        {
            if (GraphicsDevice.Presenter is null) return;

            UpdateViewProjectionMatrices();
            UpdateLights();

            var componentsWithSameModel = Components.GroupBy(m => m.Model).ToArray();

            int batchCount = Math.Min(Environment.ProcessorCount, componentsWithSameModel.Length);
            int batchSize = (int)Math.Ceiling((double)componentsWithSameModel.Length / batchCount);

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

                commandList.SetViewport(GraphicsDevice.Presenter.Viewport);
                commandList.SetScissorRectangles(GraphicsDevice.Presenter.ScissorRect);
                commandList.SetRenderTargets(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

                int end = Math.Min((batchIndex * batchSize) + batchSize, componentsWithSameModel.Length);

                for (int i = batchIndex * batchSize; i < end; i++)
                {
                    Model? model = componentsWithSameModel[i].Key;
                    var modelComponents = componentsWithSameModel[i];

                    if (model is null) continue;

                    if (!models.ContainsKey(model))
                    {
                        models.Add(model, (null, null));
                    }

                    (CompiledCommandList[]? bundles, Texture[]? worldMatrixBuffers) = models[model];

                    int meshCount = model.Meshes.Count;
                    int highestPassCount = model.Materials.Max(m => m.Passes.Count);

                    if (worldMatrixBuffers is null || worldMatrixBuffers.Length != meshCount)
                    {
                        if (worldMatrixBuffers != null)
                        {
                            foreach (Texture constantBuffer in worldMatrixBuffers)
                            {
                                constantBuffer?.Dispose();
                            }
                        }

                        worldMatrixBuffers = new Texture[meshCount];

                        for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
                        {
                            worldMatrixBuffers[meshIndex] = Texture.CreateConstantBufferView(GraphicsDevice, modelComponents.Count() * 16 * sizeof(float));
                        }

                        if (bundles != null)
                        {
                            foreach (CompiledCommandList bundle in bundles)
                            {
                                bundle.Builder.Dispose();
                            }
                        }

                        bundles = new CompiledCommandList[highestPassCount];

                        for (int passIndex = 0; passIndex < highestPassCount; passIndex++)
                        {
                            CompiledCommandList? bundle = RecordCommandList(
                                model,
                                new CommandList(GraphicsDevice, CommandListType.Bundle),
                                worldMatrixBuffers,
                                modelComponents.Count(),
                                passIndex);

                            if (bundle != null)
                            {
                                bundles[passIndex] = bundle;
                            }
                        }

                        models[model] = (bundles, worldMatrixBuffers);
                    }

                    int modelComponentIndex = 0;

                    foreach (ModelComponent modelComponent in modelComponents)
                    {
                        if (modelComponent.Entity != null)
                        {
                            for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
                            {
                                unsafe
                                {
                                    Matrix4x4 worldMatrix = model.Meshes[meshIndex].WorldMatrix * modelComponent.Entity.Transform.WorldMatrix;
                                    SharpDX.Utilities.Write(worldMatrixBuffers[meshIndex].MappedResource + modelComponentIndex * sizeof(Matrix4x4), ref worldMatrix);
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

            GraphicsDevice.CommandList.SetViewport(GraphicsDevice.Presenter.Viewport);
            GraphicsDevice.CommandList.SetScissorRectangles(GraphicsDevice.Presenter.ScissorRect);
            GraphicsDevice.CommandList.SetRenderTargets(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
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
                (CompiledCommandList[]? bundles, Texture[]? worldMatrixBuffers) = item.Value;

                if (worldMatrixBuffers != null)
                {
                    foreach (Texture constantBuffer in worldMatrixBuffers)
                    {
                        constantBuffer.Dispose();
                    }
                }

                if (bundles != null)
                {
                    foreach (CompiledCommandList bundle in bundles)
                    {
                        bundle.Builder.Dispose();
                    }
                }
            }
        }

        private CompiledCommandList? RecordCommandList(Model model, CommandList commandList, Texture[] worldMatrixBuffers, int instanceCount, int passIndex)
        {
            int renderTargetCount = GraphicsDevice.Presenter is null ? 1 : GraphicsDevice.Presenter.PresentationParameters.Stereo ? 2 : 1;
            instanceCount *= renderTargetCount;

            for (int i = 0; i < model.Meshes.Count; i++)
            {
                Mesh mesh = model.Meshes[i];
                Material material = model.Materials[mesh.MaterialIndex];

                if (passIndex >= material.Passes.Count) continue;

                MaterialPass materialPass = material.Passes[passIndex];

                if (mesh.VertexBufferViews is null) throw new ArgumentException("The vertex buffer views of the mesh cannot be null.");

                commandList.SetIndexBuffer(mesh.IndexBufferView);
                commandList.SetVertexBuffers(mesh.VertexBufferViews);

                commandList.SetPipelineState(materialPass.PipelineState);

                int rootParameterIndex = 0;

                commandList.SetGraphicsRoot32BitConstant(rootParameterIndex++, renderTargetCount, 0);
                commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, ViewProjectionTransformBuffer.NativeGpuDescriptorHandle);
                commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, worldMatrixBuffers[i].NativeGpuDescriptorHandle);

                commandList.SetGraphicsRootDescriptorTable(rootParameterIndex++, DirectionalLightGroupBuffer.NativeGpuDescriptorHandle);

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

                if (mesh.IndexBufferView.HasValue)
                {
                    commandList.DrawIndexedInstanced(mesh.IndexBufferView.Value.SizeInBytes / SharpDX.DXGI.FormatHelper.SizeOfInBytes(mesh.IndexBufferView.Value.Format), instanceCount);
                }
                else
                {
                    commandList.DrawInstanced(mesh.VertexBufferViews[0].SizeInBytes / mesh.VertexBufferViews[0].StrideInBytes, instanceCount);
                }
            }

            return commandList.CommandListType == CommandListType.Bundle ? commandList.Close() : null;
        }

        private void UpdateViewProjectionMatrices()
        {
            if (SceneSystem.CurrentCamera != null && SceneSystem.CurrentCamera.Entity != null)
            {
                if (GraphicsDevice.Presenter is HolographicGraphicsPresenter graphicsPresenter)
                {
                    var cameraPose = graphicsPresenter.HolographicFrame.CurrentPrediction.CameraPoses[0];

                    cameraPose.HolographicCamera.SetNearPlaneDistance(SceneSystem.CurrentCamera.NearPlaneDistance);
                    cameraPose.HolographicCamera.SetFarPlaneDistance(SceneSystem.CurrentCamera.FarPlaneDistance);

                    var viewTransform = cameraPose.TryGetViewTransform(graphicsPresenter.SpatialStationaryFrameOfReference.CoordinateSystem);

                    Span<ViewProjectionTransform> viewProjectionTransforms = stackalloc ViewProjectionTransform[2];

                    if (viewTransform.HasValue)
                    {
                        Matrix4x4.Decompose(SceneSystem.CurrentCamera.Entity.Transform.WorldMatrix, out _,
                            out Quaternion rotation,
                            out Vector3 translation);

                        Matrix4x4 positionMatrix = Matrix4x4.CreateTranslation(-translation) * Matrix4x4.CreateFromQuaternion(rotation);

                        viewProjectionTransforms[0].ViewMatrix = positionMatrix * viewTransform.Value.Left;
                        viewProjectionTransforms[1].ViewMatrix = positionMatrix * viewTransform.Value.Right;
                    }

                    viewProjectionTransforms[0].ProjectionMatrix = cameraPose.ProjectionTransform.Left;
                    viewProjectionTransforms[1].ProjectionMatrix = cameraPose.ProjectionTransform.Right;

                    viewProjectionTransforms[0].ViewProjectionMatrix = viewProjectionTransforms[0].ViewMatrix * viewProjectionTransforms[0].ProjectionMatrix;
                    viewProjectionTransforms[1].ViewProjectionMatrix = viewProjectionTransforms[1].ViewMatrix * viewProjectionTransforms[1].ProjectionMatrix;

                    SharpDX.Utilities.Write(ViewProjectionTransformBuffer.MappedResource, viewProjectionTransforms.ToArray(), 0, viewProjectionTransforms.Length);
                }
                else
                {
                    ViewProjectionTransform viewProjectionTransform = new ViewProjectionTransform
                    {
                        ViewMatrix = SceneSystem.CurrentCamera.ViewMatrix,
                        ProjectionMatrix = SceneSystem.CurrentCamera.ProjectionMatrix,
                        ViewProjectionMatrix = SceneSystem.CurrentCamera.ViewProjectionMatrix
                    };

                    Span<ViewProjectionTransform> viewProjectionTransforms = stackalloc ViewProjectionTransform[] { viewProjectionTransform, viewProjectionTransform };
                    SharpDX.Utilities.Write(ViewProjectionTransformBuffer.MappedResource, viewProjectionTransforms.ToArray(), 0, viewProjectionTransforms.Length);
                }
            }
        }

        private unsafe void UpdateLights()
        {
            LightSystem? lightSystem = SceneSystem.EntitySystems.Get<LightSystem>();

            if (lightSystem is null) return;

            var lights = lightSystem.Lights;
            int lightCount = lights.Count;

            DirectionalLightData[] lightData = new DirectionalLightData[lights.Count];

            for (int i = 0; i < lights.Count; i++)
            {
                lightData[i] = new DirectionalLightData { Color = lights[i].Color, Direction = lights[i].Direction };
            }

            SharpDX.Utilities.Write(DirectionalLightGroupBuffer.MappedResource, ref lightCount);
            SharpDX.Utilities.Write(DirectionalLightGroupBuffer.MappedResource + sizeof(Vector4), lightData, 0, lightData.Length);
        }
    }
}
