using System.Collections.Generic;
using System.Linq;

namespace DirectX12GameEngine.Input
{
    internal class UwpPointerEventArgs : PointerEventArgs
    {
        private readonly Windows.UI.Core.PointerEventArgs args;

        public UwpPointerEventArgs(Windows.UI.Core.PointerEventArgs args)
        {
            this.args = args;
        }

        public override bool Handled { get => args.Handled; set => args.Handled = value; }

        public override PointerPoint CurrentPoint => new UwpPointerPoint(args.CurrentPoint);

        public override VirtualKeyModifiers KeyModifiers => (VirtualKeyModifiers)args.KeyModifiers;

        public override IList<PointerPoint> GetIntermediatePoints() => args.GetIntermediatePoints().Select(p => new UwpPointerPoint(p)).ToArray();
    }
}
