using DirectX12GameEngine.Core.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;
using DirectX12GameEngine.Input;

#nullable enable

namespace DirectX12Game
{
    public class TestScript : StartupScript
    {
        public string? NullProperty { get; set; }

        public ColorChannel MyColorChannel { get; set; } = ColorChannel.G;

        public ColorChannel MyOtherColorChannel { get; set; } = ColorChannel.B;

        public DateTimeOffset? MyDateTime { get; set; }

        public List<string> MyStringList { get; } = new List<string> { "One", "Two", "Three", "Four" };

        public List<Vector3> MyVectorList { get; } = new List<Vector3> { new Vector3(4, 3, 2), new Vector3(34, 2, 9) };

        public override void Start()
        {
            if (Input.Keyboard != null)
            {
                Input.Keyboard.KeyDown += Keyboard_KeyDown;
            }
        }

        public override void Cancel()
        {
            if (Input.Keyboard != null)
            {
                Input.Keyboard.KeyDown -= Keyboard_KeyDown;
            }
        }

        private void Keyboard_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.Key);
        }

        private async void OnKeyDown(VirtualKey key)
        {
            Entity? cameraEntity = SceneSystem.CurrentCamera?.Entity;
            Entity scene = SceneSystem.SceneInstance?.RootEntity ?? throw new InvalidOperationException();

            switch (key)
            {
                case VirtualKey.Number0 when GraphicsDevice?.Presenter != null:
                    GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 0;
                    break;
                case VirtualKey.Number1 when GraphicsDevice?.Presenter != null:
                    GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 1;
                    break;
                case VirtualKey.D:
                    Entity? customCliffhouse = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "CustomCliffhouse");
                    if (customCliffhouse != null)
                    {
                        if (customCliffhouse.Get<ModelComponent>()?.Model?.Materials[2].Descriptor?.Attributes.Diffuse is MaterialDiffuseMapFeature diffuseMapFeature)
                        {
                            if (diffuseMapFeature.DiffuseMap is DissolveShader dissolveShader && dissolveShader.DissolveStrength is ComputeScalar strength)
                            {
                                strength.Value = 0.8f;
                            }
                        }
                    }
                    break;
                case VirtualKey.R:
                    Entity? cliffhouse = SceneSystem.SceneInstance?.RootEntity?.Children.FirstOrDefault(m => m.Name == "Cliffhouse");
                    if (cliffhouse != null)
                    {
                        SceneSystem.SceneInstance?.RootEntity?.Children.Remove(cliffhouse);
                    }
                    break;
                case VirtualKey.T:
                    Entity? cliffhouse1 = SceneSystem.SceneInstance?.RootEntity?.Children.FirstOrDefault(m => m.Name == "Cliffhouse");
                    cliffhouse1?.Remove<ModelComponent>();
                    break;
                case VirtualKey.A:
                    Entity newCliffhouse = new Entity("Cliffhouse")
                    {
                        new TransformComponent { Position = new Vector3(-200.0f, 120.0f, 500.0f) },
                        new ModelComponent(await Services.GetRequiredService<ContentManager>().LoadAsync<Model>("Assets\\Models\\Cliffhouse_Model"))
                    };

                    SceneSystem.SceneInstance?.RootEntity?.Children.Add(newCliffhouse);
                    break;
                case VirtualKey.P:
                    Entity? child1 = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Child1");
                    if (child1 != null && child1.Parent is null)
                    {
                        child1.Transform.Parent = null;
                        SceneSystem.SceneInstance?.RootEntity?.Children.Add(child1);
                    }
                    break;
                case VirtualKey.Q:
                    Entity? child2 = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Child1");
                    if (child2 != null && child2.Parent != null)
                    {
                        child2.Parent = null;
                        child2.Transform.Parent = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Parent1").Transform;
                    }
                    break;
                case VirtualKey.S:
                    SceneInstance? sceneInstance = SceneSystem.SceneInstance;
                    Entity? previousRootScene = sceneInstance?.RootEntity;

                    if (sceneInstance != null && previousRootScene != null && cameraEntity != null)
                    {
                        cameraEntity.Parent = null;

                        sceneInstance.RootEntity = new Entity { new TransformComponent { Position = new Vector3(500.0f, 0.0f, 0.0f) } };
                        sceneInstance.RootEntity.Children.Add(cameraEntity);
                        sceneInstance.RootEntity.Children.Add(previousRootScene);
                    }
                    break;
                case VirtualKey.O:
                    Entity? entity = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "CustomCliffhouse");
                    if (entity != null)
                    {
                        await Content.SaveAsync(@"Assets\CustomCliffhouse", entity);
                    }
                    break;
                case VirtualKey.I:
                    {
                        if (scene != null)
                        {
                            Entity copy = new Entity();
                            List<Entity> entities = new List<Entity>(scene.Children);

                            foreach (var entity2 in entities)
                            {
                                scene.Children.Remove(entity2);
                                copy.Children.Add(entity2);
                            }

                            await Content.SaveAsync(@"Assets\CustomScene", copy);
                        }
                    }
                    break;
                case VirtualKey.G:
                    GC.Collect();
                    break;
                case VirtualKey.K:
                    await Content.ReloadAsync(scene);
                    break;
            }
        }
    }
}
