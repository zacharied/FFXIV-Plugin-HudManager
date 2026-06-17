using Dalamud.Interface;
using HUDManager.Structs;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui;

public static class ImGuiExt {
    public static void HoverTooltip(string text) {
        if (text == "") return;
        using (ImRaii.DefaultStyle()) {
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip(text);
            }
        }
    }

    public static void HelpMarker(string text) {
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
        }

        if (ImGui.IsItemHovered()) {
            using (ImRaii.Tooltip())
            using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 20f)) {
                ImGui.Text(text);
            }
        }
    }

    public static bool IconButton(FontAwesomeIcon icon, string? id = null) {
        return ImGuiComponents.IconButton(id ?? "button", icon);
    }

    public static float IconButtonWidth(FontAwesomeIcon icon) {
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            return ImGui.CalcTextSize(icon.ToIconString()).X + ImGui.GetStyle().FramePadding.X * 2;
        }
    }

    public static bool IconCheckbox(FontAwesomeIcon icon, ref bool value, string? id = null) {
        using var font = ImRaii.PushFont(UiBuilder.IconFont);

        var text = icon.ToIconString();
        if (id != null) {
            text += $"##{id}";
        }

        return ImGui.Checkbox(text, ref value);
    }

    public record OverlayPosition(Tuple<Vector2, Vector2> Outer, Tuple<Vector2, Vector2>? Inner);

    public static OverlayPosition ConvertGameToImGuiWithInner(Element element) {
        var pos = ConvertGameToImGui(element);
        var inner = element.Id switch {
            ElementKind.TargetInfoProgressBar => CreateInner(pos, element.Scale, 246, 10, 204, 24),
            ElementKind.TargetInfoStatus => CreateInner(pos, element.Scale, 13, 45, 375, 82),
            ElementKind.TargetInfoHp => CreateInner(pos, element.Scale, 0, 0, -1, 62),
            _ => null,
        };

        return new OverlayPosition(pos, inner);
    }

    public static Tuple<Vector2, Vector2> ConvertGameToImGui(Element element) {
        // get X & Y coords from the element, which are percentages (0 - 100)
        var percentagePos = new Vector2(element.X, element.Y);

        // get size in pixels
        var size = new Vector2(element.Width, element.Height);
        // scale size according to the element's scale
        size.X = (float)Math.Round(size.X * element.Scale);
        size.Y = (float)Math.Round(size.Y * element.Scale);

        // convert the percentages into pixels
        var screen = ImGui.GetIO().DisplaySize;
        var pixelPos = new Vector2(
            (float)Math.Round(percentagePos.X * screen.X / 100),
            (float)Math.Round(percentagePos.Y * screen.Y / 100)
        );

        // split the measured from into x and y parts
        var (xMeasure, yMeasure) = element.MeasuredFrom.ToParts();

        // determine subtraction values to make the coords point to the top left
        var subX = xMeasure switch {
            MeasuredX.Left => 0,
            MeasuredX.Middle => size.X / 2,
            MeasuredX.Right => size.X,
            _ => throw new ArgumentOutOfRangeException($"Unknown measure value: {xMeasure}"),
        };

        var subY = yMeasure switch {
            MeasuredY.Top => 0,
            MeasuredY.Middle => size.Y / 2,
            MeasuredY.Bottom => size.Y,
            _ => throw new ArgumentOutOfRangeException($"Unknown measure value: {yMeasure}"),
        };

        // transform coords to top left for ImGui
        pixelPos.X -= subX;
        pixelPos.Y -= subY;

        // round the coords
        pixelPos.X = (float)Math.Round(pixelPos.X);
        pixelPos.Y = (float)Math.Round(pixelPos.Y);

        return Tuple.Create(pixelPos, size);
    }

    private static Tuple<Vector2, Vector2> CreateInner(Tuple<Vector2, Vector2> pos, float outerScale, int offsetX, int offsetY, int innerWidth, int innerHeight) {
        var pixelPos = pos.Item1;
        var size = pos.Item2;

        // round the coords
        pixelPos.X = (float)Math.Round(pixelPos.X) + offsetX * outerScale;
        pixelPos.Y = (float)Math.Round(pixelPos.Y) + offsetY * outerScale;

        if (innerWidth > 0) {
            size.X = innerWidth * outerScale;
        }

        if (innerHeight > 0) {
            size.Y = innerHeight * outerScale;
        }

        return Tuple.Create(pixelPos, size);
    }

    public static Vector2 ConvertImGuiToGame(Element element, Vector2 im) {
        // get the coordinates in pixels
        var pos = new Vector2(im.X, im.Y);

        // get the size of the element
        var size = new Vector2(element.Width, element.Height);
        // scale the size of the element
        size.X = (float)Math.Round(size.X * element.Scale);
        size.Y = (float)Math.Round(size.Y * element.Scale);

        // split the measured from into x and y parts
        var (xMeasure, yMeasure) = element.MeasuredFrom.ToParts();

        // determine how much to add to convert top left coords into the element's system
        var addX = xMeasure switch {
            MeasuredX.Left => 0,
            MeasuredX.Middle => size.X / 2,
            MeasuredX.Right => size.X,
            _ => throw new ArgumentOutOfRangeException($"Unknown measure value: {xMeasure}"),
        };

        var addY = yMeasure switch {
            MeasuredY.Top => 0,
            MeasuredY.Middle => size.Y / 2,
            MeasuredY.Bottom => size.Y,
            _ => throw new ArgumentOutOfRangeException($"Unknown measure value: {yMeasure}"),
        };

        // convert from top left to given type
        pos.X += addX;
        pos.Y += addY;

        // convert the pixels into percentages
        var screen = ImGui.GetIO().DisplaySize;
        pos.X /= screen.X / 100;
        pos.Y /= screen.Y / 100;

        return pos;
    }

    public static bool IconButtonEnabledWhen(bool enabled, FontAwesomeIcon icon, string? id = null) {
        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, !enabled)) {
            return IconButton(icon, id) && enabled;
        }
    }

    public static bool EnumCombo<T>(string label, ref T value) where T : struct, Enum {
        var values = Enum.GetValues<T>();
        if (values.Length != 0 && values[0].GetDisplayOrder() is not null) {
            Array.Sort(values, (a, b) => a.GetDisplayOrder()!.Value.CompareTo(b.GetDisplayOrder()!.Value));
        }

        var names = values.Select(e => e.GetDisplayName()).ToArray();
        var index = Array.IndexOf(values, value);

        if (ImGui.Combo(label, ref index, names, values.Length)) {
            value = values[index];
            return true;
        }

        return false;
    }

    public static bool IconButtonWithCenteredText(FontAwesomeIcon icon, string text, Vector2? size = null, bool centerIcon = false) {
        var iconStr = icon.ToIconString();
        var idIndex = text.IndexOf("##", StringComparison.Ordinal);
        var textStr = idIndex >= 0 ? text[..idIndex] : text;

        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            iconSize = ImGui.CalcTextSize(iconStr);
        }
        var textSize = ImGui.CalcTextSize(textStr);

        var framePadding = ImGui.GetStyle().FramePadding;
        var iconPadding = 3 * ImGuiHelpers.GlobalScale;
        var width = size is { X: not 0 } ? size.Value.X : iconSize.X + textSize.X + (framePadding.X * 2) + iconPadding;
        var height = size is { Y: not 0 } ? size.Value.Y : ImGui.GetFrameHeight();

        var cursor = ImGui.GetCursorScreenPos();
        bool button;
        using (ImRaii.PushId(text)) {
            button = ImGui.Button(string.Empty, new Vector2(width, height));
        }

        var totalContentWidth = iconSize.X + iconPadding + textSize.X;
        var contentEndX = cursor.X + width - framePadding.X;
        var iconX = centerIcon
            ? Math.Clamp(cursor.X + (width - totalContentWidth) / 2f, cursor.X + framePadding.X, Math.Max(cursor.X + framePadding.X, contentEndX - totalContentWidth))
            : cursor.X + framePadding.X;
        var contentStartX = iconX + iconSize.X + iconPadding;
        var textX = centerIcon
            ? contentStartX
            : Math.Clamp((contentStartX + contentEndX - textSize.X) / 2f, contentStartX, Math.Max(contentStartX, contentEndX - textSize.X));

        var iconPos = new Vector2(iconX, cursor.Y + (height - iconSize.Y) / 2f);
        var textPos = new Vector2(textX, cursor.Y + (height - textSize.Y) / 2f);

        var dl = ImGui.GetWindowDrawList();
        var textColor = ImGui.GetColorU32(ImGuiCol.Text);
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            dl.AddText(iconPos, textColor, iconStr);
        }
        dl.PushClipRect(new Vector2(contentStartX, cursor.Y), new Vector2(contentEndX, cursor.Y + height), true);
        dl.AddText(textPos, textColor, textStr);
        dl.PopClipRect();

        return button;
    }
}
