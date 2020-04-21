using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Input
{
    public class InputManager : GameSystem
    {
        private static readonly HashSet<VirtualKey> noKeys = new HashSet<VirtualKey>();

        public ObservableCollection<IInputSource> Sources { get; } = new ObservableCollection<IInputSource>();

        public IGamepadInputSource? Gamepad => GamepadSources.FirstOrDefault();

        public IKeyboardInputSource? Keyboard => KeyboardSources.FirstOrDefault();

        public IPointerInputSource? Pointer => PointerSources.FirstOrDefault();

        public IEnumerable<IGamepadInputSource> GamepadSources => Sources.OfType<IGamepadInputSource>();

        public IEnumerable<IKeyboardInputSource> KeyboardSources => Sources.OfType<IKeyboardInputSource>();

        public IEnumerable<IPointerInputSource> PointerSources => Sources.OfType<IPointerInputSource>();

        public InputManager()
        {
        }

        public InputManager(IInputSourceConfiguration configuration)
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

        public ISet<VirtualKey> DownKeys => Keyboard?.DownKeys ?? noKeys;

        public ISet<VirtualKey> PressedKeys => Keyboard?.PressedKeys ?? noKeys;

        public ISet<VirtualKey> ReleasedKeys => Keyboard?.ReleasedKeys ?? noKeys;

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
    }
}
