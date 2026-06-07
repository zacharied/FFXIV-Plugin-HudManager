using Dalamud.Bindings.ImGui;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace HUDManager.Ui;

public static class ImCursor {
    public static Vector2 Position {
        get => ImGui.GetCursorPos();
        set => ImGui.SetCursorPos(value);
    }

    [SuppressMessage("ReSharper", "UseWithExpressionToCopyStruct")] // For consistency in the switch expression
    public static void ToNestedRect(Vector2 inner, Vector2 outer, ImAlign align = ImAlign.Center, Vector2? offset = null) {
        var delta = outer - inner;
        var relPos = align switch {
            ImAlign.TopLeft => Vector2.Zero,
            ImAlign.Top => new Vector2(delta.X / 2f, 0),
            ImAlign.TopRight => new Vector2(delta.X, 0),
            ImAlign.Left => new Vector2(0, delta.Y / 2f),
            ImAlign.Center => new Vector2(delta.X / 2f, delta.Y / 2f),
            ImAlign.Right => new Vector2(delta.X, delta.Y / 2f),
            ImAlign.BottomLeft => new Vector2(0, delta.Y),
            ImAlign.Bottom => new Vector2(delta.X / 2f, delta.Y),
            ImAlign.BottomRight => new Vector2(delta.X, delta.Y),
            _ => throw new ArgumentOutOfRangeException($"Unknown alignment: {align}"),
        };

        if (offset.HasValue)
            relPos += offset.Value;

        Position += relPos;
    }
}

public enum ImAlign {
    TopLeft,
    Top,
    TopRight,
    Left,
    Center,
    Right,
    BottomLeft,
    Bottom,
    BottomRight,
}
