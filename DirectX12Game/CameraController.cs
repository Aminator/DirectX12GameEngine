using System;
using System.Numerics;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Input;

namespace DirectX12Game
{
    public class CameraController : SyncScript
    {
        private readonly float scrollSpeed = 0.05f;
        private CameraComponent? camera;

        public CameraComponent? Camera { get => camera; set { camera = value; if (SceneSystem != null) SceneSystem.CurrentCamera = camera; } }

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
        }

        public override void Update()
        {
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
                if (Input.Pointer != null)
                {
                    Input.Pointer.IsPointerPositionLocked = true;
                }
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
