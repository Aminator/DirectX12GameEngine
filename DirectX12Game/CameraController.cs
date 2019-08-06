using System;
using System.Numerics;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Input;

namespace DirectX12Game
{
    public class CameraController : SyncScript
    {
        private readonly float scrollSpeed = 0.05f;

        public CameraComponent? Camera { get; set; }

        public override void Start()
        {
            SceneSystem.CurrentCamera = Camera;

            if (Input.Keyboard != null)
            {
                Input.Keyboard.KeyDown += (s, e) => OnKeyDown(e.Key);
            }

            if (Input.Pointer != null)
            {
                Input.Pointer.PointerPressed += (s, e) => OnPointerPressed(e.CurrentPoint);
                Input.Pointer.PointerWheelChanged += (s, e) => OnPointerWheelChanged(e.CurrentPoint);
            }
        }

        public override void Update()
        {
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

        private void OnPointerPressed(PointerPoint pointerPoint)
        {
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                if (Input.Pointer != null)
                {
                    Input.Pointer.IsPointerPositionLocked = true;
                }
            }
            else if (pointerPoint.Properties.IsRightButtonPressed)
            {
                MoveCamera(10.0f);
            }
        }

        private void OnPointerWheelChanged(PointerPoint pointerPoint)
        {
            MoveCamera(-pointerPoint.Properties.MouseWheelDelta * scrollSpeed);
        }

        private void OnKeyDown(VirtualKey key)
        {
            switch (key)
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
