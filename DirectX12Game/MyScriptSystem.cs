using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Serialization;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12Game
{
    public class MyScriptSystem : EntitySystem<MyScriptComponent>
    {
        private float time;
        private float scrollAmount = 50.0f;
        private readonly float scrollSpeed = 0.01f;

        public MyScriptSystem(IServiceProvider services) : base(services, typeof(TransformComponent))
        {
            GameBase game = services.GetRequiredService<GameBase>();

            Content = services.GetRequiredService<ContentManager>();
            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
            SceneSystem = services.GetRequiredService<SceneSystem>();

#if WINDOWS_UWP
            if (game.Context is GameContextCoreWindow context)
            {
                context.Control.KeyDown += (s, e) => OnKeyDown(e.VirtualKey);
                context.Control.PointerPressed += (s, e) => OnPointerPressed(e.CurrentPoint);
                context.Control.PointerWheelChanged += (s, e) => OnPointerWheelChanged(e.CurrentPoint);
            }
            else if (game.Context is GameContextXaml xamlContext)
            {
                xamlContext.Control.KeyDown += (s, e) => OnKeyDown(e.Key);
                xamlContext.Control.PointerPressed += (s, e) => OnPointerPressed(e.GetCurrentPoint(xamlContext.Control));
                xamlContext.Control.PointerWheelChanged += (s, e) => OnPointerWheelChanged(e.GetCurrentPoint(xamlContext.Control));
            }
#endif
#if NETCOREAPP
            if (game.Context is GameContextWinForms winFormsContext)
            {
                winFormsContext.Control.KeyDown += async (s, e) =>
                {
                    if (e.KeyCode == System.Windows.Forms.Keys.O)
                    {
                        Entity customCliffhouse = EntityManager.FirstOrDefault(m => m.Name == "CustomCliffhouse");
                        if (customCliffhouse != null)
                        {
                            await Content.SaveAsync(@"Assets\CustomCliffhouse.xml", customCliffhouse);
                        }
                    }
                    else if (e.KeyCode == System.Windows.Forms.Keys.I)
                    {
                        Scene? scene = SceneSystem.SceneInstance?.RootScene;
                        if (scene != null)
                        {
                            Scene copy = new Scene();
                            List<Entity> entities = new List<Entity>(scene);

                            foreach (var entity in entities)
                            {
                                scene.Remove(entity);
                                copy.Add(entity);
                            }

                            await Content.SaveAsync(@"Assets\CustomScene.xml", copy);
                        }
                    }
                };
            }
#endif
        }

        public ContentManager Content { get; }

        public GraphicsDevice GraphicsDevice { get; }

        public SceneSystem SceneSystem { get; }

        public override void Update(GameTime gameTime)
        {
            //System.Diagnostics.Debug.WriteLine(1.0 / deltaTime.TotalSeconds);

            if (EntityManager is null) return;

            foreach (MyScriptComponent component in Components)
            {
                time += (float)gameTime.Elapsed.TotalSeconds;

                Quaternion timeRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, time * 0.2f);

                Scene? scene = SceneSystem.SceneInstance?.RootScene;

                if (scene != null)
                {
                    CameraComponent? camera = component.Camera;
                    //Entity camera = scene.FirstOrDefault(m => m.Name == "MyCamera");
                    if (camera != null && camera.Entity != null)
                    {
                        SceneSystem.CurrentCamera = camera;
                        camera.Entity.Transform.Position = new Vector3(camera.Entity.Transform.Position.X, camera.Entity.Transform.Position.Y, 10.0f * scrollAmount * 3);
                    }

                    Entity light = EntityManager.FirstOrDefault(m => m.Name == "MyLight");
                    if (light != null)
                    {
                        light.Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, time) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)(-Math.PI / 4.0f));
                    }

                    var rexes = EntityManager.Where(m => m.Name == "T-Rex");
                    foreach (var item in rexes)
                    {
                        item.Transform.RotationEuler = QuaternionExtensions.ToEuler(timeRotation);
                    }

                    //Entity video = scene.FirstOrDefault(m => m.Name == "MyVideo");
                    //if (video != null)
                    //{
                    //    VideoComponent? videoComponent = video.Get<VideoComponent>();

                    //    if (videoComponent?.Target is null)
                    //    {
                    //        Model? model = tRex?.Get<ModelComponent>()?.Model;
                    //        //videoComponent.Target = GraphicsDevice.Presenter?.BackBuffer;
                    //    }
                    //}

                    Entity customCliffhouse = EntityManager.FirstOrDefault(m => m.Name == "CustomCliffhouse");
                    if (customCliffhouse != null)
                    {
                        customCliffhouse.Transform.Rotation = timeRotation;
                    }

                    Entity cliffhouse = EntityManager.FirstOrDefault(m => m.Name == "Cliffhouse");
                    if (cliffhouse != null)
                    {
                        cliffhouse.Transform.Rotation = timeRotation;
                    }

                    Entity rightHandModel = EntityManager.FirstOrDefault(m => m.Name == "RightHandModel");
                    if (rightHandModel != null)
                    {
                        rightHandModel.Transform.Rotation = timeRotation;
                    }

                    Entity leftHandModel = EntityManager.FirstOrDefault(m => m.Name == "HoloTile");
                    if (leftHandModel != null)
                    {
                        leftHandModel.Transform.Rotation = timeRotation;
                    }

                    Entity icon = EntityManager.FirstOrDefault(m => m.Name == "Icon_Failure");
                    if (icon != null)
                    {
                        icon.Transform.Rotation = timeRotation;
                    }

                    Entity cube = EntityManager.FirstOrDefault(m => m.Name == "LiveCube");
                    if (cube != null)
                    {
                        cube.Transform.Rotation = timeRotation;
                    }

                    Entity parent1 = EntityManager.FirstOrDefault(m => m.Name == "Parent1");
                    if (parent1 != null)
                    {
                        parent1.Transform.Rotation = timeRotation;
                    }
                }
            }
        }

#if WINDOWS_UWP
        private async void OnKeyDown(Windows.System.VirtualKey key)
        {
            Entity? cameraEntity = SceneSystem.CurrentCamera?.Entity;

            switch (key)
            {
                case Windows.System.VirtualKey.Left:
                    if (cameraEntity != null) cameraEntity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(10 * Math.PI / 180.0f));
                    break;
                case Windows.System.VirtualKey.Right:
                    if (cameraEntity != null) cameraEntity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(-10 * Math.PI / 180.0f));
                    break;
                case Windows.System.VirtualKey.Up:
                    scrollAmount--;
                    break;
                case Windows.System.VirtualKey.Down:
                    scrollAmount++;
                    break;
                case Windows.System.VirtualKey.Number0 when GraphicsDevice.Presenter != null:
                    GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 0;
                    break;
                case Windows.System.VirtualKey.Number1 when GraphicsDevice.Presenter != null:
                    GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 1;
                    break;
                case Windows.System.VirtualKey.D:
                    Entity? customCliffhouse = EntityManager.FirstOrDefault(m => m.Name == "CustomCliffhouse");
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
                    Entity? cliffhouse = SceneSystem.SceneInstance?.RootScene?.FirstOrDefault(m => m.Name == "Cliffhouse");
                    if (cliffhouse != null)
                    {
                        SceneSystem.SceneInstance?.RootScene?.Remove(cliffhouse);
                    }
                    break;
                case Windows.System.VirtualKey.A:
                    Entity newCliffhouse = new Entity("Cliffhouse")
                    {
                        new TransformComponent { Position = new Vector3(-200.0f, 120.0f, 500.0f) },
                        new ModelComponent(await Services.GetRequiredService<ContentManager>().LoadAsync<Model>("Assets/Models/Cliffhouse_Model.xml"))
                    };

                    SceneSystem.SceneInstance?.RootScene?.Add(newCliffhouse);
                    break;
                case Windows.System.VirtualKey.H:
                    Entity? cliffhouseToClone = SceneSystem.SceneInstance?.RootScene?.FirstOrDefault(m => m.Name == "Cliffhouse");

                    XmlSerializer serializer = new XmlSerializer(typeof(Entity), new[] { typeof(TransformComponent), typeof(ModelComponent) });

                    using (MemoryStream stream = new MemoryStream())
                    {
                        serializer.Serialize(stream, cliffhouseToClone);
                        stream.Flush();
                        stream.Seek(0, SeekOrigin.Begin);

                        Entity entityClone = (Entity)serializer.Deserialize(stream);
                        entityClone.Transform.Position = new Vector3(200.0f, 120.0f, 500.0f);

                        SceneSystem.SceneInstance?.RootScene?.Add(entityClone);
                    }
                    break;
                case Windows.System.VirtualKey.P:
                    Entity? child1 = EntityManager?.FirstOrDefault(m => m.Name == "Child1");
                    if (child1 != null && child1.Scene is null)
                    {
                        child1.Transform.Parent = null;
                        SceneSystem.SceneInstance?.RootScene?.Add(child1);
                    }
                    break;
                case Windows.System.VirtualKey.Q:
                    Entity? child2 = EntityManager?.FirstOrDefault(m => m.Name == "Child1");
                    if (child2 != null && child2.Scene != null)
                    {
                        child2.Scene.Remove(child2);
                        child2.Transform.Parent = EntityManager.FirstOrDefault(m => m.Name == "Parent1").Transform;
                    }
                    break;
                case Windows.System.VirtualKey.S:
                    SceneInstance? sceneInstance = SceneSystem.SceneInstance;
                    Scene? previousRootScene = sceneInstance?.RootScene;

                    if (sceneInstance != null && previousRootScene != null && cameraEntity != null)
                    {
                        cameraEntity.Scene?.Remove(cameraEntity);

                        sceneInstance.RootScene = new Scene { Offset = new Vector3(500.0f, 0.0f, 0.0f) };
                        sceneInstance.RootScene.Add(cameraEntity);
                        sceneInstance.RootScene.Children.Add(previousRootScene);
                    }
                    break;
                case Windows.System.VirtualKey.O:
                    Entity entity = EntityManager.FirstOrDefault(m => m.Name == "CustomCliffhouse");
                    if (entity != null)
                    {
                        await Content.SaveAsync(@"Assets\CustomCliffhouse.xml", entity);
                    }
                    break;
            }
        }

        private void OnPointerPressed(Windows.UI.Input.PointerPoint pointerPoint)
        {
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                scrollAmount--;
            }
            else if (pointerPoint.Properties.IsRightButtonPressed)
            {
                scrollAmount++;
            }
        }

        private void OnPointerWheelChanged(Windows.UI.Input.PointerPoint pointerPoint)
        {
            scrollAmount -= pointerPoint.Properties.MouseWheelDelta * scrollSpeed;
        }
#endif
    }
}
