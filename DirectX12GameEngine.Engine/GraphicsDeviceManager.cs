//using System;
//using DirectX12GameEngine.Graphics;

//namespace DirectX12GameEngine.Engine
//{
//    public class GraphicsDeviceManager : IGraphicsDeviceManager, IDisposable
//    {
//        private readonly object deviceCeationLock = new object();

//        private bool isDrawing;

//        public GraphicsDevice? GraphicsDevice { get; private set; }

//        public bool BeginDraw()
//        {
//            if (GraphicsDevice != null)
//            {
//                isDrawing = true;
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }

//        public void CreateDevice()
//        {
//            lock (deviceCeationLock)
//            {
//                if (GraphicsDevice is null)
//                {
//                    GraphicsDevice = new GraphicsDevice();
//                }
//            }
//        }

//        public void Dispose()
//        {
//            if (GraphicsDevice != null)
//            {
//                if (GraphicsDevice.Presenter != null)
//                {
//                    GraphicsDevice.Presenter.Dispose();
//                    GraphicsDevice.Presenter = null;
//                }

//                GraphicsDevice.Dispose();
//                GraphicsDevice = null;
//            }
//        }

//        public void EndDraw()
//        {
//            if (isDrawing)
//            {
//                isDrawing = false;
//                GraphicsDevice?.Presenter?.Present();
//            }
//        }
//    }
//}
