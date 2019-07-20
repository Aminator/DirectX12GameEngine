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

namespace DirectX12Game
{
    public class TestScript : StartupScript
    {
        public override void Start()
        {
#if WINDOWS_UWP
            if (Game.Context is GameContextCoreWindow context)
            {
                context.Control.KeyDown += Control_KeyDown;
            }
            else if (Game.Context is GameContextXaml xamlContext)
            {
                xamlContext.Control.KeyDown += Control_KeyDown1;
            }
#endif
#if NETCOREAPP
            if (Game.Context is GameContextWinForms winFormsContext)
            {
                winFormsContext.Control.KeyDown += Control_KeyDown2;
            }
#endif
        }

#if WINDOWS_UWP
        private void Control_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            OnKeyDown(args.VirtualKey);
        }

        private void Control_KeyDown1(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            OnKeyDown(e.Key);
        }
#endif
#if NETCOREAPP
        private void Control_KeyDown2(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            OnKeyDown((Windows.System.VirtualKey)e.KeyCode);
        }
#endif

        public override void Cancel()
        {
#if WINDOWS_UWP
            if (Game.Context is GameContextCoreWindow context)
            {
                context.Control.KeyDown -= Control_KeyDown;
            }
            else if (Game.Context is GameContextXaml xamlContext)
            {
                xamlContext.Control.KeyDown -= Control_KeyDown1;
            }
#endif
#if NETCOREAPP
            if (Game.Context is GameContextWinForms winFormsContext)
            {
                winFormsContext.Control.KeyDown -= Control_KeyDown2;
            }
#endif
        }

        private async void OnKeyDown(Windows.System.VirtualKey key)
        {
            Entity? cameraEntity = SceneSystem.CurrentCamera?.Entity;
            Entity scene = SceneSystem.SceneInstance?.RootEntity ?? throw new InvalidOperationException();

            switch (key)
            {
                case Windows.System.VirtualKey.Number0 when GraphicsDevice?.Presenter != null:
                    GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 0;
                    break;
                case Windows.System.VirtualKey.Number1 when GraphicsDevice?.Presenter != null:
                    GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 1;
                    break;
                case Windows.System.VirtualKey.D:
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
                case Windows.System.VirtualKey.R:
                    Entity? cliffhouse = SceneSystem.SceneInstance?.RootEntity?.Children.FirstOrDefault(m => m.Name == "Cliffhouse");
                    if (cliffhouse != null)
                    {
                        SceneSystem.SceneInstance?.RootEntity?.Children.Remove(cliffhouse);
                    }
                    break;
                case Windows.System.VirtualKey.T:
                    Entity? cliffhouse1 = SceneSystem.SceneInstance?.RootEntity?.Children.FirstOrDefault(m => m.Name == "Cliffhouse");
                    cliffhouse1?.Remove<ModelComponent>();
                    break;
                case Windows.System.VirtualKey.A:
                    Entity newCliffhouse = new Entity("Cliffhouse")
                    {
                        new TransformComponent { Position = new Vector3(-200.0f, 120.0f, 500.0f) },
                        new ModelComponent(await Services.GetRequiredService<ContentManager>().LoadAsync<Model>("Assets\\Models\\Cliffhouse_Model"))
                    };

                    SceneSystem.SceneInstance?.RootEntity?.Children.Add(newCliffhouse);
                    break;
                case Windows.System.VirtualKey.P:
                    Entity? child1 = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Child1");
                    if (child1 != null && child1.Parent is null)
                    {
                        child1.Transform.Parent = null;
                        SceneSystem.SceneInstance?.RootEntity?.Children.Add(child1);
                    }
                    break;
                case Windows.System.VirtualKey.Q:
                    Entity? child2 = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Child1");
                    if (child2 != null && child2.Parent != null)
                    {
                        child2.Parent = null;
                        child2.Transform.Parent = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Parent1").Transform;
                    }
                    break;
                case Windows.System.VirtualKey.S:
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
                case Windows.System.VirtualKey.O:
                    Entity? entity = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "CustomCliffhouse");
                    if (entity != null)
                    {
                        await Content.SaveAsync(@"Assets\CustomCliffhouse", entity);
                    }
                    break;
                case Windows.System.VirtualKey.I:
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
                case Windows.System.VirtualKey.G:
                    GC.Collect();
                    break;
                case Windows.System.VirtualKey.K:
                    await Content.ReloadAsync(scene);
                    break;
            }
        }
    }
}
