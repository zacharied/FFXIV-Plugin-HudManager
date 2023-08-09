using Dalamud.Interface;
using HUD_Manager.Structs;
using ImGuiNET;
using System;
using System.Numerics;

namespace HUD_Manager.Ui
{
    public static class ImGuiExt
    {
        private static readonly Vector2 verticalSpace = new Vector2(0, 5f);
        public static void VerticalSpace() => ImGui.Dummy(verticalSpace);

        public static void HoverTooltip(string text)
        {
            if (!ImGui.IsItemHovered()) {
                return;
            }

            ImGui.BeginTooltip();
            ImGui.TextUnformatted(text);
            ImGui.EndTooltip();
        }

        public static void HelpMarker(string text)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
            ImGui.PopFont();

            if (!ImGui.IsItemHovered()) {
                return;
            }

            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        public static bool IconButton(FontAwesomeIcon icon, string? id = null)
        {
            ImGui.PushFont(UiBuilder.IconFont);

            var text = icon.ToIconString();
            if (id != null) {
                text += $"##{id}";
            }

            var result = ImGui.Button(text);

            ImGui.PopFont();

            return result;
        }

        public static bool IconCheckbox(FontAwesomeIcon icon, ref bool value, string? id = null)
        {
            ImGui.PushFont(UiBuilder.IconFont);

            var text = icon.ToIconString();
            if (id != null) {
                text += $"##{id}";
            }

            var result = ImGui.Checkbox(text, ref value);

            ImGui.PopFont();

            return result;
        }

        public static void CenterColumnText(string text)
        {
            var posX = ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X - ImGui.GetScrollX() - 2 * ImGui.GetStyle().ItemSpacing.X;
            if (posX > ImGui.GetCursorPosX()) {
                ImGui.SetCursorPosX(posX);
            }
            ImGui.Text(text);
        }

        public record OverlayPosition(Tuple<Vector2, Vector2> Outer, Tuple<Vector2, Vector2>? Inner);

        public static OverlayPosition ConvertGameToImGui(Element element)
        {
            var inner = element.Id switch
            {
                ElementKind.TargetInfoProgressBar => ConvertGameToImGui(element, 246, 10, 204, 24, 1),
                ElementKind.TargetInfoStatus => ConvertGameToImGui(element, 13, 45, 375, 82, 1),
                ElementKind.TargetInfoHp => ConvertGameToImGui(element, 0, 0, -1, -1, 0.5f),
                _ => null
            };

            return new OverlayPosition(ConvertGameToImGui(element, 0, 0, -1, -1, 1), inner);
        }

        private static Tuple<Vector2, Vector2> ConvertGameToImGui(Element element, int offsetX, int offsetY, int innerWidth, int innerHeight, float heightScale)
        {
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
            var subX = xMeasure switch
            {
                MeasuredX.Left => 0,
                MeasuredX.Middle => size.X / 2,
                MeasuredX.Right => size.X,
                _ => throw new ArgumentOutOfRangeException(),
            };

            var subY = yMeasure switch
            {
                MeasuredY.Top => 0,
                MeasuredY.Middle => size.Y / 2,
                MeasuredY.Bottom => size.Y,
                _ => throw new ArgumentOutOfRangeException(),
            };

            // transform coords to top left for ImGui
            pixelPos.X -= subX;
            pixelPos.Y -= subY;

            // round the coords
            pixelPos.X = (float)Math.Round(pixelPos.X) + offsetX * element.Scale;
            pixelPos.Y = (float)Math.Round(pixelPos.Y) + offsetY * element.Scale;

            if (innerWidth > 0) {
                size.X = innerWidth * element.Scale;
            }

            if (innerHeight > 0) {
                size.Y = innerHeight * element.Scale;
            }

            size.Y *= heightScale;

            return Tuple.Create(pixelPos, size);
        }

        public static Vector2 ConvertImGuiToGame(Element element, Vector2 im)
        {
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
            var addX = xMeasure switch
            {
                MeasuredX.Left => 0,
                MeasuredX.Middle => size.X / 2,
                MeasuredX.Right => size.X,
                _ => throw new ArgumentOutOfRangeException(),
            };

            var addY = yMeasure switch
            {
                MeasuredY.Top => 0,
                MeasuredY.Middle => size.Y / 2,
                MeasuredY.Bottom => size.Y,
                _ => throw new ArgumentOutOfRangeException(),
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
    }
}
