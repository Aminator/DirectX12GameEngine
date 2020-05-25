using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DirectX12GameEngine.Assets;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Input;
using DirectX12GameEngine.Physics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Materials;
using DirectX12GameEngine.Serialization;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace DirectX12Game
{
    public class TestScript : StartupScript
    {
        public string? NullProperty { get; set; }

        public ColorChannel MyColorChannel { get; set; } = ColorChannel.G;

        public ColorChannel MyOtherColorChannel { get; set; } = ColorChannel.B;

        public DateTime MyDateTime { get; set; } = DateTime.UtcNow;

        public TimeSpan MyTimeSpan { get; set; } = new TimeSpan(3, 4, 5);

        public List<string> MyStringList { get; } = new List<string> { "One", "Two", "Three", "Four" };

        public List<Vector3> MyVectorList { get; } = new List<Vector3> { new Vector3(4, 3, 2), new Vector3(34, 2, 9) };

        public override void Start()
        {
            if (Input.Keyboard != null)
            {
                Input.Keyboard.KeyDown += OnKeyDown;
            }
        }

        public override void Cancel()
        {
            if (Input.Keyboard != null)
            {
                Input.Keyboard.KeyDown -= OnKeyDown;
            }
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            Entity? cameraEntity = SceneSystem.CurrentCamera?.Entity;
            Entity scene = await Content.GetAsync<Entity>("Assets\\Scenes\\Scene1");

            switch (e.Key)
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
                            if (diffuseMapFeature.DiffuseMap is DissolveShader dissolveShader && dissolveShader.DissolveStrength is ScalarShader strength)
                            {
                                strength.Value = 0.8f;
                            }
                        }
                    }
                    break;
                case VirtualKey.T:
                    Entity? cliffhouse1 = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "");
                    cliffhouse1?.Remove<ModelComponent>();
                    break;
                case VirtualKey.A:
                    Entity newCliffhouse = new Entity("Cliffhouse")
                    {
                        new TransformComponent { Position = new Vector3(-200.0f, 120.0f, 500.0f) },
                        new ModelComponent(await Services.GetRequiredService<IContentManager>().LoadAsync<Model>("Assets\\Models\\Cliffhouse_Model"))
                    };

                    SceneSystem.RootEntity?.Children.Add(newCliffhouse);
                    break;
                case VirtualKey.P:
                    Entity? child1 = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Child1");
                    if (child1 != null && child1.Parent != null)
                    {
                        child1.Transform.Parent = null;
                        SceneSystem.RootEntity?.Children.Add(child1);
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
                    Entity? previousRootScene = SceneSystem.RootEntity;

                    if (previousRootScene != null && cameraEntity != null)
                    {
                        cameraEntity.Parent = null;

                        SceneSystem.RootEntity = new Entity { new TransformComponent { Position = new Vector3(500.0f, 0.0f, 0.0f) } };
                        SceneSystem.RootEntity.Children.Add(cameraEntity);
                        SceneSystem.RootEntity.Children.Add(previousRootScene);
                    }
                    break;
                case VirtualKey.O:
                    Entity? entity = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Cliffhouse");
                    if (entity != null)
                    {
                        await Content.SaveAsync(@"Assets\Cliffhouse", entity);
                    }
                    break;
                case VirtualKey.I:
                    if (scene != null)
                    {
                        await Content.SaveAsync(@"Assets\CustomScene", scene);
                    }
                    break;
                case VirtualKey.U:
                    Model? model = await Content.GetAsync<Model?>(@"Assets\Models\SwimmingShark_Model");

                    if (model != null)
                    {
                        await Content.SaveAsync(@"Assets\CustomSwimmingSharkModel", model);
                    }
                    break;
                case VirtualKey.X:
                    Model? sharkModel = await Content.GetAsync<Model?>(@"Assets\Models\SwimmingShark_Model");

                    if (sharkModel != null)
                    {
                        ModelAsset modelAsset = new ModelAsset { Source = @"Assets\Resources\Models\SwimmingShark.glb" };

                        foreach (Material material in sharkModel.Materials)
                        {
                            modelAsset.Materials.Add(material);
                        }

                        await Content.SaveAsync(@"Assets\CustomSwimmingSharkModelAsset", modelAsset);
                    }
                    break;
                case VirtualKey.G:
                    GC.Collect();
                    break;
                case VirtualKey.C:
                    Entity? tRex = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "T-Rex");

                    var rigidBody = new RigidBodyComponent
                    {
                        ColliderShape = new SphereColliderShape(50.0f),
                        Mass = 100.0f
                    };

                    tRex?.Add(rigidBody);

                    Entity? child = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Child2");

                    var rigidBody2 = new RigidBodyComponent
                    {
                        ColliderShape = new SphereColliderShape(100.0f),
                        Mass = 50.0f
                    };

                    child?.Add(rigidBody2);

                    Entity? childBelow = Entity?.EntityManager?.FirstOrDefault(m => m.Name == "Child1");

                    var staticCollider = new StaticColliderComponent
                    {
                        ColliderShape = new SphereColliderShape(80.0f)
                    };

                    childBelow?.Add(staticCollider);

                    break;
                case VirtualKey.R:
                    PhysicsSimulation? simulation = this.GetSimulation();

                    Matrix4x4 cameraMatrix = cameraEntity!.Transform.WorldMatrix;
                    Vector3 forwardVector = new Vector3(-cameraMatrix.M31, -cameraMatrix.M32, -cameraMatrix.M33);

                    if (simulation != null && simulation.RayCast(cameraMatrix.Translation, forwardVector, float.MaxValue, out RayHit hit))
                    {
                        hit.Collider.Entity?.Parent?.Children.Remove(hit.Collider.Entity!);
                    }
                    break;
            }
        }
    }
}
