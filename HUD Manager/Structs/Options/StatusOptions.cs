using System;

namespace HUD_Manager.Structs.Options {
    public class StatusOptions {
        private readonly byte[] _options;

        public StatusStyle Style {
            get => (StatusStyle) this._options[0];
            set => this._options[0] = (byte) value;
        }

        public StatusOptions(byte[] options) {
            this._options = options;
        }
    }

    public enum StatusStyle : byte {
        Normal = 1,
        NormalLeftJustified1 = 11,
        NormalLeftJustified2 = 21,
        NormalLeftJustified3 = 31,
        ThreeGroups = 0,
        FourGroups = 2,
    }

    public static class StatusStyleExt {
        public static string Name(this StatusStyle style) {
            return style switch {
                StatusStyle.Normal => "Normal",
                StatusStyle.NormalLeftJustified1 => "Normal (left-justified 1)",
                StatusStyle.NormalLeftJustified2 => "Normal (left-justified 2)",
                StatusStyle.NormalLeftJustified3 => "Normal (left-justified 3)",
                StatusStyle.ThreeGroups => "Split into three groups",
                StatusStyle.FourGroups => "Split into four groups",
                _ => style.ToString(),
            };
        }
    }

    public class StatusInfoOptions {
        private const int GamepadBit = 1 << 4;

        private readonly ElementKind _kind;
        private readonly byte[] _options;

        public StatusLayout Layout {
            get => this.ExtractStyle().Item1;
            set => this._options[0] = this.ComputeStyle(value, this.Alignment, this.Gamepad);
        }

        public StatusAlignment Alignment {
            get => this.ExtractStyle().Item2;
            set => this._options[0] = this.ComputeStyle(this.Layout, value, this.Gamepad);
        }

        public StatusGamepad Gamepad {
            get => this.ExtractStyle().Item3;
            set => this._options[0] = this.ComputeStyle(this.Layout, this.Alignment, value);
        }

        public StatusInfoOptions(ElementKind kind, byte[] options) {
            this._kind = kind;
            this._options = options;
        }

        private byte ComputeStyle(StatusLayout layout, StatusAlignment alignment, StatusGamepad gamepad) {
            byte result = layout switch {
                StatusLayout.TenByTwo => 0,
                StatusLayout.TwentyByOne => 1,
                StatusLayout.SevenByThree => 2,
                StatusLayout.FiveByFour => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null),
            };

            if (alignment == StatusAlignment.RightJustified) {
                result += 4;
            }

            if (this._kind != ElementKind.StatusInfoOther && gamepad == StatusGamepad.NonFocusable) {
                result |= GamepadBit;
            }

            if (this._kind == ElementKind.StatusInfoOther && gamepad == StatusGamepad.Focusable) {
                result |= GamepadBit;
            }

            return result;
        }

        private Tuple<StatusLayout, StatusAlignment, StatusGamepad> ExtractStyle() {
            var gamepadBitSet = (this._options[0] & GamepadBit) > 0;
            var gamepad = this._kind == ElementKind.StatusInfoOther
                ? gamepadBitSet
                    ? StatusGamepad.Focusable
                    : StatusGamepad.NonFocusable
                : gamepadBitSet
                    ? StatusGamepad.NonFocusable
                    : StatusGamepad.Focusable;
            var basic = this._options[0] & ~GamepadBit;

            var alignment = basic < 4 ? StatusAlignment.LeftJustified : StatusAlignment.RightJustified;

            var layout = (basic % 4) switch {
                0 => StatusLayout.TenByTwo,
                1 => StatusLayout.TwentyByOne,
                2 => StatusLayout.SevenByThree,
                3 => StatusLayout.FiveByFour,
                _ => throw new ArgumentOutOfRangeException(),
            };

            return Tuple.Create(layout, alignment, gamepad);
        }
    }

    public enum StatusLayout {
        TwentyByOne,
        TenByTwo,
        SevenByThree,
        FiveByFour,
    }

    public static class StatusLayoutExt {
        public static string Name(this StatusLayout layout) {
            return layout switch {
                StatusLayout.TwentyByOne => "20x1",
                StatusLayout.TenByTwo => "10x2",
                StatusLayout.SevenByThree => "7x3",
                StatusLayout.FiveByFour => "5x4",
                _ => layout.ToString(),
            };
        }
    }

    public enum StatusAlignment {
        LeftJustified,
        RightJustified,
    }

    public static class StatusAlignmentExt {
        public static string Name(this StatusAlignment alignment) {
            return alignment switch {
                StatusAlignment.LeftJustified => "Left-justified",
                StatusAlignment.RightJustified => "Right-justified",
                _ => alignment.ToString(),
            };
        }
    }

    public enum StatusGamepad {
        Focusable,
        NonFocusable,
    }
}
