using System;
using Newtonsoft.Json;

namespace HUD_Manager.Configuration {
    [Serializable]
    public class Window {
        public const WindowComponent AllEnabled = WindowComponent.X | WindowComponent.Y;

        public WindowComponent Enabled { get; set; } = WindowComponent.X | WindowComponent.Y;

        public Vector2<short> Position { get; set; }

        public bool this[WindowComponent component] {
            get => (this.Enabled & component) > 0;
            set {
                if (value) {
                    this.Enabled |= component;
                } else {
                    this.Enabled &= ~component;
                }
            }
        }

        [JsonConstructor]
        public Window(WindowComponent enabled, Vector2<short> position) {
            this.Enabled = enabled;
            this.Position = position;
        }

        public Window(Vector2<short> position) {
            this.Position = position;
        }

        public Window Clone() {
            return new Window(this.Enabled, new Vector2<short>(this.Position.X, this.Position.Y));
        }

        public void UpdateEnabled(Window other) {
            if (other[WindowComponent.X]) {
                this.Position.X = other.Position.X;
            }

            if (other[WindowComponent.Y]) {
                this.Position.Y = other.Position.Y;
            }
        }
    }

    [Flags]
    public enum WindowComponent {
        X = 1 << 0,
        Y = 1 << 1,
    }
}
