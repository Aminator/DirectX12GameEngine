using System;
using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

namespace DirectX12GameEngine.Editor.Views
{
    public abstract class ShellLight : XamlLight
    {
        public static string Id { get; } = typeof(ShellLight).FullName;

        public static readonly DependencyProperty IsTargetProperty = DependencyProperty.RegisterAttached("IsTarget", typeof(bool), typeof(ShellLight), new PropertyMetadata(null, OnIsTargetChanged));

        public static bool GetIsTarget(DependencyObject target)
        {
            return (bool)target.GetValue(IsTargetProperty);
        }

        public static void SetIsTarget(DependencyObject target, bool value)
        {
            target.SetValue(IsTargetProperty, value);
        }

        private static void OnIsTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            bool isAdding = (bool)e.NewValue;

            if (isAdding)
            {
                AddTarget(Id, d);
            }
            else
            {
                RemoveTarget(Id, d);
            }
        }

        private static void AddTarget(string lightId, DependencyObject dependencyObject)
        {
            if (dependencyObject is UIElement element)
            {
                AddTargetElement(lightId, element);
            }
            else if (dependencyObject is Brush brush)
            {
                AddTargetBrush(lightId, brush);
            }
        }

        private static void RemoveTarget(string lightId, DependencyObject dependencyObject)
        {
            if (dependencyObject is UIElement element)
            {
                RemoveTargetElement(lightId, element);
            }
            else if (dependencyObject is Brush brush)
            {
                RemoveTargetBrush(lightId, brush);
            }
        }

        protected override string GetId()
        {
            return Id;
        }

        protected override void OnDisconnected(UIElement oldElement)
        {
            if (CompositionLight != null)
            {
                CompositionLight.Dispose();
                CompositionLight = null;
            }
        }
    }

    public class ShellPointLight : ShellLight
    {
        public static readonly DependencyProperty DelayTimeProperty = DependencyProperty.Register("DelayTime", typeof(TimeSpan), typeof(ShellLight), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan DelayTime
        {
            get { return (TimeSpan)GetValue(DelayTimeProperty); }
            set { SetValue(DelayTimeProperty, value); }
        }

        protected override void OnConnected(UIElement newElement)
        {
            if (CompositionLight is null)
            {
                PointLight pointLight = Window.Current.Compositor.CreatePointLight();
                pointLight.Color = Colors.White;
                pointLight.Intensity = 4.0f;

                CompositionLight = pointLight;

                Vector3KeyFrameAnimation animation = Window.Current.Compositor.CreateVector3KeyFrameAnimation();

                Visual visual = ElementCompositionPreview.GetElementVisual(newElement);

                animation.InsertExpressionKeyFrame(0.25f, ExpressionFunctions.Vector3(visual.GetReference().Size.X, 0.0f, 100.0f));
                animation.InsertExpressionKeyFrame(0.50f, ExpressionFunctions.Vector3(visual.GetReference().Size.X, visual.GetReference().Size.Y, 100.0f));
                animation.InsertExpressionKeyFrame(0.75f, ExpressionFunctions.Vector3(0.0f, visual.GetReference().Size.Y, 100.0f));
                animation.InsertExpressionKeyFrame(1.00f, ExpressionFunctions.Vector3(0.0f, 0.0f, 100.0f));

                animation.Duration = TimeSpan.FromSeconds(6);
                animation.IterationBehavior = AnimationIterationBehavior.Forever;

                animation.DelayTime = DelayTime;

                pointLight.StartAnimation(nameof(PointLight.Offset), animation);
            }
        }
    }

    public class ShellAmbientLight : ShellLight
    {
        protected override void OnConnected(UIElement newElement)
        {
            if (CompositionLight is null)
            {
                AmbientLight ambientLight = Window.Current.Compositor.CreateAmbientLight();
                ambientLight.Color = Colors.White;
                ambientLight.Intensity = 1.0f;

                CompositionLight = ambientLight;
            }
        }
    }
}
