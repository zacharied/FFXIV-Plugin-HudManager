using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HUDManager.Structs.Options;

public abstract class StatusInfoOptions<TLayout> where TLayout : struct, Enum {
    private readonly EnumBitField<TLayout> layout;
    private readonly EnumBitField<GamepadFocusType> gamepad;
    private readonly Element element;

    protected StatusInfoOptions(Element element) {
        this.element = element;
        layout = new EnumBitField<TLayout>(element.Options!, 0, 0, 3);
        gamepad = new EnumBitField<GamepadFocusType>(element.Options!, 0, 4, 1);
    }

    protected bool InvertGamepadFocus { get; init; }

    public TLayout Layout {
        get => layout.Value;
        set {
            layout.Value = value;

            var size = CalculateSize(value);
            element.Width = size.X;
            element.Height = size.Y;
        }
    }

    public GamepadFocusType Gamepad {
        get => InvertGamepadFocus ? Invert(gamepad.Value) : gamepad.Value;
        set => gamepad.Value = InvertGamepadFocus ? Invert(value) : value;
    }

    private static GamepadFocusType Invert(GamepadFocusType type) {
        return type == GamepadFocusType.Focusable ? GamepadFocusType.NonFocusable : GamepadFocusType.Focusable;
    }

    private static Vector2<ushort> CalculateSize(object layout) {
        return layout switch {
            StatusLayoutLarge largeLayout => largeLayout switch {
                StatusLayoutLarge.LeftJustified10x2 or StatusLayoutLarge.RightJustified10x2 => new Vector2<ushort>(250, 82),
                StatusLayoutLarge.LeftJustified20x1 or StatusLayoutLarge.RightJustified20x1 => new Vector2<ushort>(500, 41),
                StatusLayoutLarge.LeftJustified7x3 or StatusLayoutLarge.RightJustified7x3 => new Vector2<ushort>(175, 123),
                StatusLayoutLarge.LeftJustified5x4 or StatusLayoutLarge.RightJustified5x4 => new Vector2<ushort>(125, 164),
                _ => throw new ArgumentOutOfRangeException($"Unknown large layout type: {largeLayout}")
            },
            StatusLayoutSmall smallLayout => smallLayout switch {
                StatusLayoutSmall.LeftJustified8x1 or StatusLayoutSmall.RightJustified8x1 => new Vector2<ushort>(200, 41),
                StatusLayoutSmall.LeftJustified4x2 or StatusLayoutSmall.RightJustified4x2 => new Vector2<ushort>(100, 82),
                StatusLayoutSmall.LeftJustified3x3 or StatusLayoutSmall.RightJustified3x3 => new Vector2<ushort>(75, 123),
                _ => throw new ArgumentOutOfRangeException($"Unknown small layout type: {smallLayout}")
            },
            _ => throw new ArgumentOutOfRangeException($"Unknown layout type: {layout}")
        };
    }
}

public class StatusInfoEnhancementsOptions(Element element) : StatusInfoOptions<StatusLayoutLarge>(element) {
    private readonly EnumBitField<StatusEnhancementSettings> displaySettings = new(element.Options!, 1);

    public StatusEnhancementSettings DisplaySettings {
        get => displaySettings.Value;
        set => displaySettings.Value = value;
    }
}

public class StatusInfoConditionalOptions(Element element) : StatusInfoOptions<StatusLayoutSmall>(element);

public class StatusInfoEnfeeblementsOptions(Element element) : StatusInfoOptions<StatusLayoutLarge>(element);

public class StatusInfoOtherOptions : StatusInfoOptions<StatusLayoutLarge> {
    public StatusInfoOtherOptions(Element element) : base(element) {
        InvertGamepadFocus = true;
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")] // To match game format
public enum StatusLayoutLarge: byte {
    [Display(Name = "Left-justified 10x2", Order = 1)]
    LeftJustified10x2 = 0,
    [Display(Name = "Left-justified 20x1", Order = 0)]
    LeftJustified20x1 = 1,
    [Display(Name = "Left-justified 7x3", Order = 2)]
    LeftJustified7x3 = 2,
    [Display(Name = "Left-justified 5x4", Order = 3)]
    LeftJustified5x4 = 3,
    [Display(Name = "Right-justified 10x2", Order = 5)]
    RightJustified10x2 = 4,
    [Display(Name = "Right-justified 20x1", Order = 4)]
    RightJustified20x1 = 5,
    [Display(Name = "Right-justified 7x3", Order = 6)]
    RightJustified7x3 = 6,
    [Display(Name = "Right-justified 5x4", Order = 7)]
    RightJustified5x4 = 7,
}

[SuppressMessage("ReSharper", "InconsistentNaming")] // To match game format
public enum StatusLayoutSmall: byte {
    [Display(Name = "Left-justified 8x1", Order = 0)]
    LeftJustified8x1 = 0,
    [Display(Name = "Left-justified 4x2", Order = 1)]
    LeftJustified4x2 = 1,
    [Display(Name = "Left-justified 3x3", Order = 2)]
    LeftJustified3x3 = 2,
    [Display(Name = "Right-justified 8x1", Order = 3)]
    RightJustified8x1 = 3,
    [Display(Name = "Right-justified 4x2", Order = 4)]
    RightJustified4x2 = 4,
    [Display(Name = "Right-justified 3x3", Order = 5)]
    RightJustified3x3 = 5,
}

public enum StatusEnhancementSettings : byte {
    [Display(Name = "Default")]
    Default = 0,
    [Display(Name = "Prioritize Own Enhancements")]
    PrioritizeOwn = 1,
    [Display(Name = "Display Others' Enhancements in Status Info (Other)")]
    DisplayOthersInGroup4 = 2,
}

public enum GamepadFocusType: byte {
    Focusable = 0,
    NonFocusable = 1,
}
