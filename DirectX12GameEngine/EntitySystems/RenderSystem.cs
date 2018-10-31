using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine
{
    public sealed class RenderSystem : EntitySystem<ModelComponent>
    {
        private CompiledCommandList[]? compiledCommandLists;

        private readonly List<CommandList> commandLists = new List<CommandList>();

        public RenderSystem(IServiceProvider services) : base(services, typeof(TransformComponent))
        {
            SceneSystem = Services.GetRequiredService<SceneSystem>();

            Span<Matrix4x4> matrices = stackalloc Matrix4x4[] { Matrix4x4.Identity, Matrix4x4.Identity };
            ViewProjectionBuffer = GraphicsDevice.CreateConstantBufferView(matrices);
        }

        public SceneSystem SceneSystem { get; }

        public Texture ViewProjectionBuffer { get; }

        public override void Draw(TimeSpan deltaTime)
        {
            if (GraphicsDevice.Presenter is null) return;

            UpdateViewProjectionMatrices();

            ModelComponent[] modelComponents = Components.ToArray();

            int batchCount = /*Math.Min(Environment.ProcessorCount, modelComponents.Length)*/1;
            int batchSize = (int)Math.Ceiling((double)modelComponents.Length / batchCount);

            if (compiledCommandLists is null || compiledCommandLists.Length < batchCount + 1)
            {
                Array.Resize(ref compiledCommandLists, batchCount + 1);

                for (int i = commandLists.Count; i < batchCount; i++)
                {
                    commandLists.Add(new CommandList(GraphicsDevice, SharpDX.Direct3D12.CommandListType.Direct));
                    commandLists[i].Close();
                }
            }

            compiledCommandLists[0] = GraphicsDevice.CommandList.Close();

            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++) /*Parallel.For(0, batchCount, batchIndex =>*/
            {
                CommandList commandList = commandLists[batchIndex];
                commandList.Reset();

                commandList.SetViewport(GraphicsDevice.Presenter.Viewport);
                commandList.SetScissorRectangles(GraphicsDevice.Presenter.ScissorRect);
                commandList.SetRenderTargets(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

                int end = Math.Min((batchIndex * batchSize) + batchSize, modelComponents.Length);

                for (int i = batchIndex * batchSize; i < end; i++)
                {
                    ModelComponent modelComponent = modelComponents[i];

                    if (modelComponent.Model is null || modelComponent.Entity is null) continue;

                    int count = modelComponent.Model.Meshes.Count;

                    if (modelComponent.ConstantBuffers is null)
                    {
                        modelComponent.ConstantBuffers = new Texture[count];

                        for (int j = 0; j < count; j++)
                        {
                            Matrix4x4 worldMatrix = modelComponent.Model.Meshes[j].WorldMatrix * modelComponent.Entity.Transform.WorldMatrix;
                            Texture constantBuffer = GraphicsDevice.CreateConstantBufferView(worldMatrix).DisposeBy(GraphicsDevice);
                            modelComponent.ConstantBuffers[j] = constantBuffer;
                        }
                    }

                    for (int j = 0; j < count; j++)
                    {
                        Matrix4x4 worldMatrix = modelComponent.Model.Meshes[j].WorldMatrix * modelComponent.Entity.Transform.WorldMatrix;
                        SharpDX.Utilities.Write(modelComponent.ConstantBuffers[j].MappedResource, ref worldMatrix);
                    }

                    if (modelComponent.CommandList is null)
                    {
                        modelComponent.CommandList = RecordCommandList(
                            modelComponent,
                            new CommandList(GraphicsDevice, SharpDX.Direct3D12.CommandListType.Bundle).DisposeBy(GraphicsDevice));
                    }

                    //RecordCommandList(modelComponent, commandList);

                    if (modelComponent.CommandList != null && modelComponent.CommandList.Builder.CommandListType == SharpDX.Direct3D12.CommandListType.Bundle)
                    {
                        commandList.ExecuteBundle(modelComponent.CommandList);
                    }
                }

                compiledCommandLists[batchIndex + 1] = commandList.Close();
            }

            GraphicsDevice.ExecuteCommandLists(compiledCommandLists);

            GraphicsDevice.CommandList.Reset();

            GraphicsDevice.CommandList.SetViewport(GraphicsDevice.Presenter.Viewport);
            GraphicsDevice.CommandList.SetScissorRectangles(GraphicsDevice.Presenter.ScissorRect);
            GraphicsDevice.CommandList.SetRenderTargets(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
        }

        public override void Dispose()
        {
            ViewProjectionBuffer.Dispose();

            foreach (CommandList commandList in commandLists)
            {
                commandList.Dispose();
            }
        }

        private CompiledCommandList? RecordCommandList(ModelComponent modelComponent, CommandList commandList)
        {
            if (modelComponent.Model is null) throw new ArgumentException("The model of the model component can't be null.");
            if (modelComponent.ConstantBuffers is null) throw new ArgumentException("The constant buffers of the model component can't be null.");

            for (int i = 0; i < modelComponent.Model.Meshes.Count; i++)
            {
                Mesh mesh = modelComponent.Model.Meshes[i];

                if (mesh.VertexBufferViews is null) throw new ArgumentException("The vertex buffer views of the mesh can't be null.");

                Material material = modelComponent.Model.Materials[mesh.MaterialIndex];

                commandList.SetPipelineState(material.PipelineState);
                commandList.SetGraphicsRootDescriptorTable(0, ViewProjectionBuffer.NativeGpuDescriptorHandle);
                commandList.SetGraphicsRootDescriptorTable(1, modelComponent.ConstantBuffers[i].NativeGpuDescriptorHandle);

                for (int j = 0; j < material.Textures.Count; j++)
                {
                    commandList.SetGraphicsRootDescriptorTable(j + 2, material.Textures[j].NativeGpuDescriptorHandle);
                }

                commandList.SetIndexBuffer(mesh.IndexBufferView);
                commandList.SetVertexBuffers(mesh.VertexBufferViews);

                int instanceCount = GraphicsDevice.Presenter is null ? 1 : GraphicsDevice.Presenter.PresentationParameters.Stereo ? 2 : 1;

                if (mesh.IndexBufferView.HasValue)
                {
                    commandList.DrawIndexedInstanced(mesh.IndexBufferView.Value.SizeInBytes / 2, instanceCount);
                }
                else
                {
                    commandList.DrawInstanced(mesh.VertexBufferViews[0].SizeInBytes / mesh.VertexBufferViews[0].StrideInBytes, instanceCount);
                }
            }

            if (commandList.CommandListType == SharpDX.Direct3D12.CommandListType.Bundle)
            {
                return commandList.Close();
            }
            else
            {
                return null;
            }
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

                    Span<Matrix4x4> viewMatrices = stackalloc Matrix4x4[] { Matrix4x4.Identity, Matrix4x4.Identity };
                    Span<Matrix4x4> projectionMatrices = stackalloc Matrix4x4[] { Matrix4x4.Identity, Matrix4x4.Identity };

                    if (viewTransform.HasValue)
                    {
                        Matrix4x4.Decompose(SceneSystem.CurrentCamera.Entity.Transform.WorldMatrix, out _,
                            out Quaternion rotation,
                            out Vector3 translation);

                        Matrix4x4 positionMatrix = Matrix4x4.CreateTranslation(-translation) * Matrix4x4.CreateFromQuaternion(rotation);

                        viewMatrices[0] = positionMatrix * viewTransform.Value.Left;
                        viewMatrices[1] = positionMatrix * viewTransform.Value.Right;
                    }

                    projectionMatrices[0] = cameraPose.ProjectionTransform.Left;
                    projectionMatrices[1] = cameraPose.ProjectionTransform.Right;

                    Span<Matrix4x4> matrices = stackalloc Matrix4x4[]
                    {
                        viewMatrices[0] * projectionMatrices[0],
                        viewMatrices[1] * projectionMatrices[1]
                    };

                    SharpDX.Utilities.Write(ViewProjectionBuffer.MappedResource, matrices.ToArray(), 0, matrices.Length);
                }
                else
                {
                    Matrix4x4 viewProjectionMatrix = SceneSystem.CurrentCamera.ViewProjectionMatrix;
                    Span<Matrix4x4> matrices = stackalloc Matrix4x4[] { viewProjectionMatrix, viewProjectionMatrix };
                    SharpDX.Utilities.Write(ViewProjectionBuffer.MappedResource, matrices.ToArray(), 0, matrices.Length);
                }
            }
        }
    }
}
