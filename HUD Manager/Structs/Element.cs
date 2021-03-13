using System;

namespace HUD_Manager.Structs {
    [Serializable]
    public class Element {
        public const ElementComponent AllEnabled = ElementComponent.X
                                                   | ElementComponent.Y
                                                   | ElementComponent.Scale
                                                   | ElementComponent.Visibility
                                                   | ElementComponent.Opacity
                                                   | ElementComponent.Options;

        public ElementKind Id { get; set; }

        public ElementComponent Enabled { get; set; } = AllEnabled;

        public float X { get; set; }

        public float Y { get; set; }

        public float Scale { get; set; }

        public byte[] Options { get; set; }

        public ushort Width { get; set; }

        public ushort Height { get; set; }

        public MeasuredFrom MeasuredFrom { get; set; }

        public VisibilityFlags Visibility { get; set; }

        public byte Unknown6 { get; set; }

        public byte Opacity { get; set; }

        public byte[] Unknown8 { get; set; }

        public bool this[VisibilityFlags flags] {
            get => (this.Visibility & flags) > 0;
            set {
                if (value) {
                    this.Visibility |= flags;
                } else {
                    this.Visibility &= ~flags;
                }
            }
        }

        public bool this[ElementComponent component] {
            get => (this.Enabled & component) > 0;
            set {
                if (value) {
                    this.Enabled |= component;
                } else {
                    this.Enabled &= ~component;
                }
            }
        }

        #pragma warning disable 8618
        private Element() {
        }
        #pragma warning restore 8618

        public Element(RawElement raw) {
            this.Id = raw.id;
            this.X = raw.x;
            this.Y = raw.y;
            this.Scale = raw.scale;
            this.Options = raw.options;
            this.Width = raw.width;
            this.Height = raw.height;
            this.MeasuredFrom = raw.measuredFrom;
            this.Visibility = raw.visibility;
            this.Unknown6 = raw.unknown6;
            this.Opacity = raw.opacity;
            this.Unknown8 = raw.unknown8;
        }

        public Element Clone() {
            return new() {
                Enabled = this.Enabled,
                Id = this.Id,
                X = this.X,
                Y = this.Y,
                Scale = this.Scale,
                Options = (byte[]) this.Options.Clone(),
                Width = this.Width,
                Height = this.Height,
                MeasuredFrom = this.MeasuredFrom,
                Visibility = this.Visibility,
                Unknown6 = this.Unknown6,
                Opacity = this.Opacity,
                Unknown8 = (byte[]) this.Unknown8.Clone(),
            };
        }

        public void UpdateEnabled(Element other) {
            if (other[ElementComponent.X]) {
                this.X = other.X;
            }

            if (other[ElementComponent.Y]) {
                this.Y = other.Y;
            }

            if (other[ElementComponent.Scale]) {
                this.Scale = other.Scale;
            }

            if (other[ElementComponent.Visibility]) {
                this.Visibility = other.Visibility;
            }

            if (other[ElementComponent.Opacity]) {
                this.Opacity = other.Opacity;
            }

            if (other[ElementComponent.Options]) {
                this.Options = other.Options;
            }

            this.Height = other.Height;
            this.Width = other.Width;
            this.MeasuredFrom = other.MeasuredFrom;
            this.Unknown6 = other.Unknown6;
            this.Unknown8 = other.Unknown8;
        }
    }

    [Flags]
    public enum ElementComponent : uint {
        X = 1 << 0,
        Y = 1 << 1,
        Scale = 1 << 2,
        Visibility = 1 << 3,
        Opacity = 1 << 4,
        Options = 1 << 5,
    }
}
