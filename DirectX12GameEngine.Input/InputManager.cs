using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Input
{
    public class InputManager : GameSystemBase
    {
        private readonly List<IGamepadInputSource> gamepadSources = new List<IGamepadInputSource>();
        private readonly List<IKeyboardInputSource> keyboardSources = new List<IKeyboardInputSource>();
        private readonly List<IPointerInputSource> pointerSources = new List<IPointerInputSource>();

        private readonly HashSet<VirtualKey> noKeys = new HashSet<VirtualKey>();

        public ObservableCollection<IInputSource> Sources { get; } = new ObservableCollection<IInputSource>();

        public IGamepadInputSource? Gamepad => gamepadSources.FirstOrDefault();

        public IKeyboardInputSource? Keyboard => keyboardSources.FirstOrDefault();

        public IPointerInputSource? Pointer => pointerSources.FirstOrDefault();

        public IReadOnlyList<IGamepadInputSource> GamepadSources => gamepadSources.AsReadOnly();

        public IReadOnlyList<IKeyboardInputSource> KeyboardSources => keyboardSources.AsReadOnly();

        public IReadOnlyList<IPointerInputSource> PointerSources => pointerSources.AsReadOnly();

        public InputManager()
        {
            Sources.CollectionChanged += OnSourcesCollectionChanged;
        }

        public InputManager(IInputSourceConfiguration configuration) : this()
        {
            AddSourcesFromConfiguration(configuration);
        }

        public void AddSourcesFromConfiguration(IInputSourceConfiguration configuration)
        {
            foreach (IInputSource inputSource in configuration.Sources)
            {
                Sources.Add(inputSource);
            }
        }

        public bool IsKeyDown(VirtualKey key)
        {
            return Keyboard?.DownKeys.Contains(key) ?? false;
        }

        public bool IsKeyPressed(VirtualKey key)
        {
            return Keyboard?.PressedKeys.Contains(key) ?? false;
        }

        public bool IsKeyReleased(VirtualKey key)
        {
            return Keyboard?.ReleasedKeys.Contains(key) ?? false;
        }

        public ISet<VirtualKey> DownKeys => Keyboard is null ? noKeys : Keyboard.DownKeys;

        public ISet<VirtualKey> PressedKeys => Keyboard is null ? noKeys : Keyboard.PressedKeys;

        public ISet<VirtualKey> ReleasedKeys => Keyboard is null ? noKeys : Keyboard.ReleasedKeys;

        public void Scan()
        {
            foreach (IInputSource source in Sources)
            {
                source.Scan();
            }
        }

        public override void Update(GameTime gameTime)
        {
            foreach (IInputSource source in Sources)
            {
                source.Update();
            }
        }

        private void OnInputSourceAdded(IInputSource source)
        {
            switch (source)
            {
                case IGamepadInputSource gamepadSource:
                    gamepadSources.Add(gamepadSource);
                    break;
                case IKeyboardInputSource keyboardSource:
                    keyboardSources.Add(keyboardSource);
                    break;
                case IPointerInputSource pointerSource:
                    pointerSources.Add(pointerSource);
                    break;
            }
        }

        private void OnInputSourceRemoved(IInputSource source)
        {
            switch (source)
            {
                case IGamepadInputSource gamepadSource:
                    gamepadSources.Remove(gamepadSource);
                    break;
                case IKeyboardInputSource keyboardDevice:
                    keyboardSources.Remove(keyboardDevice);
                    break;
                case IPointerInputSource pointerSource:
                    pointerSources.Remove(pointerSource);
                    break;
            }
        }

        private void OnSourcesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (IInputSource source in e.NewItems.Cast<IInputSource>())
                    {
                        OnInputSourceAdded(source);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (IInputSource source in e.OldItems.Cast<IInputSource>())
                    {
                        OnInputSourceRemoved(source);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    gamepadSources.Clear();
                    keyboardSources.Clear();
                    pointerSources.Clear();
                    break;
            }
        }
    }
}
