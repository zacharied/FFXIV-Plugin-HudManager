using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Linq;
using System.Numerics;

namespace HUDManager.Ui;

public static class ImGuiExt {
    public const ImGuiComboFlags ImGuiComboFlagsCustomPreview = (ImGuiComboFlags)(1 << 20);

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

    public static Vector2 GetMousePosInRect(Vector2 min, Vector2 max) {
        var mousePos = ImGui.GetMousePos();
        var relativePos = new Vector2(mousePos.X - min.X, mousePos.Y - min.Y);
        return new Vector2(relativePos.X / (max.X - min.X), relativePos.Y / (max.Y - min.Y));
    }

    public static void DrawLayoutText(string? name, bool isSelected) {
        if (name == null) {
            var iconColor = isSelected ? ImGuiColors.InfoForeground : ImGuiColors.DalamudGrey3;
            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth))
            using (ImRaii.PushColor(ImGuiCol.Text, iconColor)) {
                ImGui.Text(FontAwesomeIcon.BorderNone.ToIconString());
            }
            ImGui.SameLine(0, 0);
            ImGui.Text($" <none>");
        } else {
            var iconColor = isSelected ? ImGuiColors.InfoForeground : ImGuiColors.DalamudGrey3;
            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth))
            using (ImRaii.PushColor(ImGuiCol.Text, iconColor)) {
                ImGui.Text(FontAwesomeIcon.LayerGroup.ToIconString());
            }
            ImGui.SameLine(0, 0);
            ImGui.Text($" {name}");
        }
    }

    public static void DrawLayoutTextPlain(string? name) {
        if (name == null) {
            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth)) {
                ImGui.Text(FontAwesomeIcon.BorderNone.ToIconString());
            }
            ImGui.SameLine(0, 0);
            ImGui.Text($" <none>");
        } else {
            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth)) {
                ImGui.Text(FontAwesomeIcon.LayerGroup.ToIconString());
            }
            ImGui.SameLine(0, 0);
            ImGui.Text($" {name}");
        }
    }
}
