using System;
using System.Linq;
using System.Numerics;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Rendering;

namespace DirectX12Game
{
    public class MyScriptSystem : EntitySystem<MyScriptComponent>
    {
        private float time;
        private float scrollAmount = 50.0f;
        private readonly float scrollSpeed = 0.01f;

        public MyScriptSystem(IServiceProvider services) : base(services, typeof(TransformComponent))
        {
#if WINDOWS_UWP
            if (Game.GameContext is GameContextCoreWindow context)
            {
                context.Control.KeyDown += (s, e) => OnKeyDown(e.VirtualKey);
                context.Control.PointerPressed += (s, e) => OnPointerPressed(e.CurrentPoint);
                context.Control.PointerWheelChanged += (s, e) => OnPointerWheelChanged(e.CurrentPoint);
            }
            else if (Game.GameContext is GameContextXaml xamlContext)
            {
                xamlContext.Control.KeyDown += (s, e) => OnKeyDown(e.Key);
                xamlContext.Control.PointerPressed += (s, e) => OnPointerPressed(e.GetCurrentPoint(xamlContext.Control));
                xamlContext.Control.PointerWheelChanged += (s, e) => OnPointerWheelChanged(e.GetCurrentPoint(xamlContext.Control));
            }
#endif
        }

        public override void Update(GameTime gameTime)
        {
            //System.Diagnostics.Debug.WriteLine(1.0 / deltaTime.TotalSeconds);

            foreach (MyScriptComponent component in Components)
            {
                time += (float)gameTime.Elapsed.TotalSeconds;

                Quaternion timeRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, time * 0.2f);

                var scene = SceneSystem.RootScene;

                if (scene != null)
                {
                    CameraComponent? camera = component.Camera;
                    //Entity camera = scene.FirstOrDefault(m => m.Name == "MyCamera");
                    if (camera != null && camera.Entity != null)
                    {
                        SceneSystem.CurrentCamera = camera;
                        camera.Entity.Transform.Position = new Vector3(camera.Entity.Transform.Position.X, camera.Entity.Transform.Position.Y, 10.0f * scrollAmount * 3);
                    }

                    Entity light = SceneSystem.FirstOrDefault(m => m.Name == "MyLight");
                    if (light != null)
                    {
                        light.Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, time) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 4.0f);
                    }

                    var rexes = SceneSystem.Where(m => m.Name == "T-Rex");
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

                    Entity cliffhouse = SceneSystem.FirstOrDefault(m => m.Name == "Cliffhouse");
                    if (cliffhouse != null)
                    {
                        cliffhouse.Transform.Rotation = timeRotation;
                    }

                    Entity rightHandModel = SceneSystem.FirstOrDefault(m => m.Name == "RightHandModel");
                    if (rightHandModel != null)
                    {
                        rightHandModel.Transform.Rotation = timeRotation;
                    }

                    Entity leftHandModel = SceneSystem.FirstOrDefault(m => m.Name == "HoloTile");
                    if (leftHandModel != null)
                    {
                        leftHandModel.Transform.Rotation = timeRotation;
                    }

                    Entity icon = SceneSystem.FirstOrDefault(m => m.Name == "Icon_Failure");
                    if (icon != null)
                    {
                        icon.Transform.Rotation = timeRotation;
                    }

                    Entity cube = SceneSystem.FirstOrDefault(m => m.Name == "LiveCube");
                    if (cube != null)
                    {
                        cube.Transform.Rotation = timeRotation;
                    }

                    Entity parent1 = SceneSystem.FirstOrDefault(m => m.Name == "Parent1");
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
                    if (cameraEntity != null) cameraEntity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, 10 * MathF.PI / 180.0f);
                    break;
                case Windows.System.VirtualKey.Right:
                    if (cameraEntity != null) cameraEntity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, -10 * MathF.PI / 180.0f);
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
                    Entity? cliffhouse = SceneSystem.RootScene?.FirstOrDefault(m => m.Name == "Cliffhouse");
                    if (cliffhouse != null)
                    {
                        SceneSystem.RootScene?.Remove(cliffhouse);
                    }
                    break;
                case Windows.System.VirtualKey.A:
                    Entity newCliffhouse = new Entity("Cliffhouse")
                    {
                        new TransformComponent { Position = new Vector3(-200.0f, 120.0f, 500.0f) },
                        new ModelComponent(await Content.LoadAsync<Model>(@"Assets\Models\Cliffhouse_Model.xml"))
                    };

                    SceneSystem.RootScene?.Add(newCliffhouse);
                    break;
                case Windows.System.VirtualKey.P:
                    Entity? child1 = SceneSystem.FirstOrDefault(m => m.Name == "Child1");
                    if (child1 != null && child1.Scene is null)
                    {
                        child1.Transform.Parent = null;
                        SceneSystem.RootScene?.Add(child1);
                    }
                    break;
                case Windows.System.VirtualKey.Q:
                    Entity? child2 = SceneSystem.FirstOrDefault(m => m.Name == "Child1");
                    if (child2 != null && child2.Scene != null)
                    {
                        child2.Scene.Remove(child2);
                        child2.Transform.Parent = SceneSystem.FirstOrDefault(m => m.Name == "Parent1").Transform;
                    }
                    break;
                case Windows.System.VirtualKey.S:
                    Scene? previousRootScene = SceneSystem.RootScene;

                    if (previousRootScene != null && cameraEntity != null)
                    {
                        cameraEntity.Scene?.Remove(cameraEntity);

                        SceneSystem.RootScene = new Scene { Offset = new Vector3(500.0f, 0.0f, 0.0f) };
                        SceneSystem.RootScene.Add(cameraEntity);
                        SceneSystem.RootScene.Children.Add(previousRootScene);
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
