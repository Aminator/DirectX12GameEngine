using System;
using System.Numerics;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Input;
using Windows.ApplicationModel.Core;
using Windows.UI.Composition;

namespace DirectX12Game
{
    public class CameraController : SyncScript
    {
        private readonly float scrollSpeed = 0.05f;

        public CameraComponent? Camera { get; set; }

        //public Compositor? Compositor { get; set; }

        //PointLight? AnimationObject { get; set; }

        public override void Start()
        {
            SceneSystem.CurrentCamera = Camera;

            if (Input.Keyboard != null)
            {
                Input.Keyboard.KeyDown += OnKeyDown;
            }

            if (Input.Pointer != null)
            {
                Input.Pointer.PointerPressed += OnPointerPressed;
                Input.Pointer.PointerWheelChanged += OnPointerWheelChanged;
            }

            //await CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            //{
            //    Compositor = new Compositor();

            //    AnimationObject = Compositor.CreatePointLight();
            //    AnimationObject.Offset = Camera!.Entity!.Transform.Position;
            //    //AnimationObject.Properties.InsertVector3("Position", Camera!.Entity!.Transform.Position);

            //    Vector3KeyFrameAnimation animation = Compositor.CreateVector3KeyFrameAnimation();

            //    animation.InsertKeyFrame(0.5f, new Vector3(0.0f, 300.0f, 0.0f));
            //    animation.InsertKeyFrame(1.0f, new Vector3(0.0f, 500.0f, 0.0f));
            //    animation.Duration = TimeSpan.FromSeconds(10);

            //    AnimationObject.StartAnimation("Offset", animation);
            //});
        }

        public override void Update()
        {
            if (SceneSystem.CurrentCamera != Camera)
            {
                SceneSystem.CurrentCamera = Camera;
            }

            //if (AnimationObject != null)
            //{
            //    //if (AnimationObject.Properties.TryGetVector3("Offset", out Vector3 position) == CompositionGetValueStatus.Succeeded)
            //    //{
            //    //    Camera!.Entity!.Transform.Position = position;
            //    //}
            //    //else
            //    {
            //        var controller = AnimationObject.TryGetAnimationController("Offset");
            //        float progress = controller.Progress;
            //        Camera!.Entity!.Transform.Position = Vector3.Lerp(AnimationObject.Offset, new Vector3(0.0f, 500.0f, 0.0f), progress);
            //    }
            //}
        }

        public override void Cancel()
        {
            if (Input.Keyboard != null)
            {
                Input.Keyboard.KeyDown -= OnKeyDown;
            }

            if (Input.Pointer != null)
            {
                Input.Pointer.PointerPressed -= OnPointerPressed;
                Input.Pointer.PointerWheelChanged -= OnPointerWheelChanged;
            }
        }

        private void MoveCamera(float value)
        {
            if (Camera != null && Camera.Entity != null)
            {
                Vector3 position = Camera.Entity.Transform.Position;
                position.Z += value;
                Camera.Entity.Transform.Position = position;
            }
        }

        private void OnPointerPressed(object? sender, PointerEventArgs e)
        {
            if (e.CurrentPoint.Properties.IsLeftButtonPressed)
            {
                //if (Input.Pointer != null)
                //{
                //    Input.Pointer.IsPointerPositionLocked = true;
                //}
            }
            else if (e.CurrentPoint.Properties.IsRightButtonPressed)
            {
                MoveCamera(10.0f);
            }
        }

        private void OnPointerWheelChanged(object? sender, PointerEventArgs e)
        {
            MoveCamera(-e.CurrentPoint.Properties.MouseWheelDelta * scrollSpeed);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Left:
                    if (Camera?.Entity != null) Camera.Entity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(10 * Math.PI / 180.0f));
                    break;
                case VirtualKey.Right:
                    if (Camera?.Entity != null) Camera.Entity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(-10 * Math.PI / 180.0f));
                    break;
                case VirtualKey.Up:
                    MoveCamera(-10.0f);
                    break;
                case VirtualKey.Down:
                    MoveCamera(10.0f);
                    break;
                case VirtualKey.Shift:
                    if (Camera?.Entity != null) Camera.Entity.Transform.Position += Vector3.UnitY * 10.0f;
                    break;
                case VirtualKey.Control:
                    if (Camera?.Entity != null) Camera.Entity.Transform.Position -= Vector3.UnitY * 10.0f;
                    break;
                case VirtualKey.Escape:
                    if (Input.Pointer != null)
                    {
                        Input.Pointer.IsPointerPositionLocked = false;
                    }
                    break;
            }
        }
    }
}
