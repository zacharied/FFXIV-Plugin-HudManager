using System;
using Newtonsoft.Json;

namespace HUD_Manager.Configuration {
    [Serializable]
    public class Window {
        public WindowComponent Enabled { get; set; } = WindowComponent.X | WindowComponent.Y;

        public Vector2<short> Position { get; set; }

        [JsonConstructor]
        public Window(WindowComponent enabled, Vector2<short> position) {
            this.Enabled = enabled;
            this.Position = position;
        }

        public Window(Vector2<short> position) {
            this.Position = position;
        }
    }

    [Flags]
    public enum WindowComponent {
        X,
        Y,
    }
}
