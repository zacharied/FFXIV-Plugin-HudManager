using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HUDManager.Structs;
using HUDManager.Structs.Options;
using Lumina.Excel.Sheets;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace HUDManager.Ui;

public abstract class PreviewUtils {
    public static Preview CreatePreviewData(IDataManager dataManager, Element element) {
        var (pos, size) = ConvertGameToImGui(element);
        var realRect = new Preview.Rect(pos, size);

        var hudRowId = element.Id.ElementKindRowId();
        if (hudRowId < 0)
            return new Preview(realRect, null, null, Preview.Mode.Real);

        if (!dataManager.Excel.GetSheet<HudTransient>().TryGetRow((uint)hudRowId, out var transient))
            return new Preview(realRect, null, null, Preview.Mode.Real);

        var displayRect = GetTransientRect(transient.Unknown2);
        var simpleRect = GetTransientRect(transient.Unknown3);

        var activeRect = displayRect != null ? Preview.Mode.Override : Preview.Mode.Real;
        if (simpleRect != null && element.Id.ClassJob() is not null && element.Options is { } options) {
            var gaugeOpts = new GaugeOptions(options);
            if (gaugeOpts.Style == GaugeStyle.Simple)
                activeRect = Preview.Mode.Simple;
        }

        return new Preview(realRect, displayRect, simpleRect, activeRect);

        Preview.Rect? GetTransientRect(sbyte transientIndex) {
            if (transientIndex == 0)
                return null;
            var scale = element.Scale * AtkUnitBase.GetGlobalUIScale();
            var hudSize = dataManager.Excel.GetSheet<AddonHudSize>().GetRow((uint)transientIndex);
            var displaySize = new Vector2(hudSize.Unknown0, hudSize.Unknown1) * scale;
            var displayOffset = new Vector2(hudSize.Unknown2, hudSize.Unknown3) * scale;
            return new Preview.Rect(pos + displayOffset, displaySize);
        }
    }

    private static Vector2 GetSize(Element element) {
        var scale = element.Scale * AtkUnitBase.GetGlobalUIScale();
        return new Vector2(element.Width * scale, element.Height * scale);
    }

    private static Vector2 GetAnchorOffset(MeasuredFrom measuredFrom, Vector2 size) {
        var (xMeasure, yMeasure) = measuredFrom.ToParts();

        return new Vector2(
            xMeasure switch {
                MeasuredX.Left => 0f,
                MeasuredX.Middle => size.X * 0.5f,
                MeasuredX.Right => size.X,
                _ => throw new ArgumentOutOfRangeException(nameof(measuredFrom)),
            },
            yMeasure switch {
                MeasuredY.Top => 0f,
                MeasuredY.Middle => size.Y * 0.5f,
                MeasuredY.Bottom => size.Y,
                _ => throw new ArgumentOutOfRangeException(nameof(measuredFrom)),
            });
    }

    public static (Vector2 Position, Vector2 Size) ConvertGameToImGui(Element element) {
        var screen = ImGui.GetIO().DisplaySize;

        var size = GetSize(element);

        var anchorPos = new Vector2(
            element.X * screen.X / 100f,
            element.Y * screen.Y / 100f);

        var topLeft = anchorPos - GetAnchorOffset(element.MeasuredFrom, size);

        return (topLeft, size);
    }

    public static Vector2 ConvertImGuiToGame(Element element, Preview preview, Vector2 draggedPosition) {
        var screen = ImGui.GetIO().DisplaySize;
        var size = GetSize(element);

        var renderOffset = preview.ActiveRect.Position - preview.RealRect.Position;
        var realTopLeft = draggedPosition - renderOffset;

        var anchorPos = realTopLeft + GetAnchorOffset(element.MeasuredFrom, size);

        return new Vector2(anchorPos.X * 100f / screen.X, anchorPos.Y * 100f / screen.Y);
    }
}

public record Preview(Preview.Rect RealRect, Preview.Rect? OverrideRect, Preview.Rect? SimpleRect, Preview.Mode ActiveMode) {
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void DebugDrawActive() {
        ActiveRect.DebugDraw(0x4000FF00);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void DebugDrawAll() {
        RealRect.DebugDraw(0x400000FF);
        OverrideRect?.DebugDraw(0x4000FF00);
        SimpleRect?.DebugDraw(0x40FF0000);
    }

    public Rect ActiveRect =>
        ActiveMode switch {
            Mode.Real => RealRect,
            Mode.Override => OverrideRect ?? RealRect,
            Mode.Simple => SimpleRect ?? RealRect,
            _ => throw new ArgumentOutOfRangeException($"Unknown {nameof(Mode)}: {ActiveMode}")
        };

    public record Rect(Vector2 Position, Vector2 Size) {
        public Vector2 Max => Position + Size;

        public void DebugDraw(uint col) {
            ImGui.GetForegroundDrawList().AddRectFilled(Position, Max, col);
        }
    }

    public enum Mode {
        Real,
        Override,
        Simple
    }
}
