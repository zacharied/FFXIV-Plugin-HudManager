using System;

namespace HUD_Manager.Structs.Options
{
    public class StatusSplitOptions
    {
        private const int GamepadBit = 1 << 4;

        private readonly Element _element;
        private readonly ElementKind _kind;
        private readonly byte[] _options;

        public StatusSplitLayout Layout
        {
            get => this.ExtractStyle().Item1;
            set
            {
                this._options[0] = this.ComputeStyle(value, this.Alignment, this.Gamepad);

                var size = value.Size();
                this._element.Width = size.X;
                this._element.Height = size.Y;
            }
        }

        public StatusSplitAlignment Alignment
        {
            get => this.ExtractStyle().Item2;
            set => this._options[0] = this.ComputeStyle(this.Layout, value, this.Gamepad);
        }

        public StatusSplitGamepad Gamepad
        {
            get => this.ExtractStyle().Item3;
            set => this._options[0] = this.ComputeStyle(this.Layout, this.Alignment, value);
        }

        public StatusSplitOptions(Element element)
        {
            this._element = element;
            this._kind = element.Id;
            this._options = element.Options;
        }

        private byte ComputeStyle(StatusSplitLayout layout, StatusSplitAlignment alignment, StatusSplitGamepad gamepad)
        {
            byte result = layout switch
            {
                StatusSplitLayout.TenByTwo => 0,
                StatusSplitLayout.TwentyByOne => 1,
                StatusSplitLayout.SevenByThree => 2,
                StatusSplitLayout.FiveByFour => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null),
            };

            if (alignment == StatusSplitAlignment.RightJustified) {
                result += 4;
            }

            if (this._kind != ElementKind.StatusInfoOther && gamepad == StatusSplitGamepad.NonFocusable) {
                result |= GamepadBit;
            }

            if (this._kind == ElementKind.StatusInfoOther && gamepad == StatusSplitGamepad.Focusable) {
                result |= GamepadBit;
            }

            return result;
        }

        private Tuple<StatusSplitLayout, StatusSplitAlignment, StatusSplitGamepad> ExtractStyle()
        {
            var gamepadBitSet = (this._options[0] & GamepadBit) > 0;
            var gamepad = this._kind == ElementKind.StatusInfoOther
                ? gamepadBitSet
                    ? StatusSplitGamepad.Focusable
                    : StatusSplitGamepad.NonFocusable
                : gamepadBitSet
                    ? StatusSplitGamepad.NonFocusable
                    : StatusSplitGamepad.Focusable;
            var basic = this._options[0] & ~GamepadBit;

            var alignment = basic < 4 ? StatusSplitAlignment.LeftJustified : StatusSplitAlignment.RightJustified;

            var layout = (basic % 4) switch
            {
                0 => StatusSplitLayout.TenByTwo,
                1 => StatusSplitLayout.TwentyByOne,
                2 => StatusSplitLayout.SevenByThree,
                3 => StatusSplitLayout.FiveByFour,
                _ => throw new ArgumentOutOfRangeException(),
            };

            return Tuple.Create(layout, alignment, gamepad);
        }
    }

    public enum StatusSplitLayout
    {
        TwentyByOne,
        TenByTwo,
        SevenByThree,
        FiveByFour,
    }

    public static class StatusSplitLayoutExt
    {
        public static string Name(this StatusSplitLayout layout)
        {
            return layout switch
            {
                StatusSplitLayout.TwentyByOne => "20x1",
                StatusSplitLayout.TenByTwo => "10x2",
                StatusSplitLayout.SevenByThree => "7x3",
                StatusSplitLayout.FiveByFour => "5x4",
                _ => layout.ToString(),
            };
        }

        public static Vector2<ushort> Size(this StatusSplitLayout layout)
        {
            return layout switch
            {
                StatusSplitLayout.TwentyByOne => new Vector2<ushort>(500, 41),
                StatusSplitLayout.TenByTwo => new Vector2<ushort>(250, 82),
                StatusSplitLayout.SevenByThree => new Vector2<ushort>(175, 123),
                StatusSplitLayout.FiveByFour => new Vector2<ushort>(125, 164),
                _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null),
            };
        }
    }

    public enum StatusSplitAlignment
    {
        LeftJustified,
        RightJustified,
    }

    public static class StatusSplitAlignmentExt
    {
        public static string Name(this StatusSplitAlignment splitAlignment)
        {
            return splitAlignment switch
            {
                StatusSplitAlignment.LeftJustified => "Left-justified",
                StatusSplitAlignment.RightJustified => "Right-justified",
                _ => splitAlignment.ToString(),
            };
        }
    }

    public enum StatusSplitGamepad
    {
        Focusable,
        NonFocusable,
    }
}
