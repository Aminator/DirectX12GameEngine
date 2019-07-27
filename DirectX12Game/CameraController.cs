using System;
using System.Numerics;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;

namespace DirectX12Game
{
    public class CameraController : SyncScript
    {
        private readonly float scrollSpeed = 0.05f;

        public CameraComponent? Camera { get; set; }

        public override void Start()
        {
            SceneSystem.CurrentCamera = Camera;

#if WINDOWS_UWP
            if (Game.Context is CoreWindowGameContext context)
            {
                context.Control.PointerPressed += (s, e) => OnPointerPressed(e.CurrentPoint);
                context.Control.PointerWheelChanged += (s, e) => OnPointerWheelChanged(e.CurrentPoint);
                context.Control.KeyDown += (s, e) => OnKeyDown(e.VirtualKey);
            }
            else if (Game.Context is XamlGameContext xamlContext)
            {
                xamlContext.Control.PointerPressed += (s, e) => OnPointerPressed(e.GetCurrentPoint(xamlContext.Control));
                xamlContext.Control.PointerWheelChanged += (s, e) => OnPointerWheelChanged(e.GetCurrentPoint(xamlContext.Control));
                xamlContext.Control.KeyDown += (s, e) => OnKeyDown(e.Key);
            }
#endif
#if NETCOREAPP
            if (Game.Context is WinFormsGameContext winFormsContext)
            {
                winFormsContext.Control.KeyDown += (s, e) => OnKeyDown((Windows.System.VirtualKey)e.KeyCode);
            }
#endif
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

        private void OnPointerPressed(Windows.UI.Input.PointerPoint pointerPoint)
        {
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                MoveCamera(-10.0f);
            }
            else if (pointerPoint.Properties.IsRightButtonPressed)
            {
                MoveCamera(10.0f);
            }
        }

        private void OnPointerWheelChanged(Windows.UI.Input.PointerPoint pointerPoint)
        {
            MoveCamera(-pointerPoint.Properties.MouseWheelDelta * scrollSpeed);
        }

        private void OnKeyDown(Windows.System.VirtualKey key)
        {
            switch (key)
            {
                case Windows.System.VirtualKey.Left:
                    if (Camera?.Entity != null) Camera.Entity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(10 * Math.PI / 180.0f));
                    break;
                case Windows.System.VirtualKey.Right:
                    if (Camera?.Entity != null) Camera.Entity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(-10 * Math.PI / 180.0f));
                    break;
                case Windows.System.VirtualKey.Up:
                    MoveCamera(-10.0f);
                    break;
                case Windows.System.VirtualKey.Down:
                    MoveCamera(10.0f);
                    break;
            }
        }
    }
}
