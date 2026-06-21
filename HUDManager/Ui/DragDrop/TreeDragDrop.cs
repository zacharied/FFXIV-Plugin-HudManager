using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace HUDManager.Ui.DragDrop;

public sealed class TreeDragDrop(string payloadId) {
    private readonly DragDropState<Guid> state = new(payloadId);
    public string? SourceName { get; set; }
    public string? HoverName { get; set; }
    public TreeActionKind Action { get; set; } = TreeActionKind.None;

    public Guid SourceId => state.SourceId;

    public void EndFrame() {
        if (!state.CheckHover())
            Action = TreeActionKind.None;

        if (state.CheckActive()) {
            DrawTooltip();
        } else {
            SourceName = null;
            HoverName = null;
        }
    }

    public DragDisposable Drag(Guid sourceId, string sourceName) {
        var result = state.Drag(sourceId);
        if (result) {
            SourceName = sourceName;
        }
        return result;
    }

    public DropDisposable Drop(Guid hoverId, string hoverName) {
        var result = state.Drop(hoverId);

        if (!result.Success || state.IsSource(hoverId))
            return result.Reject();

        HoverName = hoverName;

        return result.Accept(ImGuiDragDropFlags.AcceptNoDrawDefaultRect);
    }

    private void DrawTooltip() {
        using var tooltip = ImRaii.Tooltip();
        if (Action is TreeActionKind.None) {
            ImGui.TextColored(ImGuiColors.DalamudGrey, $"Drag ");
            ImGui.SameLine(0, 0);
            ImGui.Text(SourceName);
        } else if (Action is TreeActionKind.InsertBefore or TreeActionKind.InsertAfter) {
            ImGui.TextColored(ImGuiColors.InfoForeground, $"Move ");
            ImGui.SameLine(0, 0);
            ImGui.Text(SourceName);
            if (GetDragMode() is TreeDragMode.Insert) {
                ImGui.TextColored(ImGuiColors.DalamudGrey3, $"(hold shift to set parent)");
            }
        } else if (Action is TreeActionKind.Attach) {
            ImGui.TextColored(ImGuiColors.ParsedPink, $"Set parent of ");
            ImGui.SameLine(0, 0);
            ImGui.Text(SourceName);
            ImGui.SameLine(0, 0);
            ImGui.TextColored(ImGuiColors.ParsedPink, $" to ");
            ImGui.SameLine(0, 0);
            ImGui.Text(HoverName);
        }
    }

    public bool IsSource(Guid id) => state.IsSource(id);

    public TreeDragMode GetDragMode() {
        if (ImGui.IsKeyDown(ImGuiKey.ModShift))
            return TreeDragMode.Attach;
        if (ImGui.IsKeyDown(ImGuiKey.ModAlt))
            return TreeDragMode.Any;
        return TreeDragMode.Insert;
    }

    public void PreviewInsertBefore(Vector2 itemRectMin, Vector2 itemRectMax) {
        Action = TreeActionKind.InsertBefore;
        DrawInsertionPreview(itemRectMin, itemRectMax, true);
    }

    public void PreviewInsertAfter(Vector2 itemRectMin, Vector2 itemRectMax) {
        Action = TreeActionKind.InsertAfter;
        DrawInsertionPreview(itemRectMin, itemRectMax, false);
    }

    public void PreviewAttach(Vector2 itemRectMin, Vector2 itemRectMax) {
        Action = TreeActionKind.Attach;
        DrawAttachPreview(itemRectMin, itemRectMax);
    }

    private static void DrawInsertionPreview(Vector2 itemRectMin, Vector2 itemRectMax, bool isBefore) {
        var drawList = ImGui.GetForegroundDrawList();
        var insertColor = ImGui.ColorConvertFloat4ToU32(ImGuiColors.WarningForeground);

        var (start, end) = isBefore switch {
            true => (itemRectMin, itemRectMax with { Y = itemRectMin.Y }),
            false => (itemRectMin with { Y = itemRectMax.Y }, itemRectMax)
        };

        var thickness = 2f * ImGuiHelpers.GlobalScale;
        drawList.AddLine(start, end, insertColor, thickness);

        var arrowSize = 8f * ImGuiHelpers.GlobalScale;
        start += ImGuiHelpers.ScaledVector2(2f, 0.5f);
        drawList.AddTriangle(
            start,
            start + new Vector2(-arrowSize, -arrowSize * 0.6f),
            start + new Vector2(-arrowSize, arrowSize * 0.6f),
            insertColor, thickness
        );
    }

    private static void DrawAttachPreview(Vector2 itemRectMin, Vector2 itemRectMax) {
        var addChildColor = ImGui.ColorConvertFloat4ToU32(ImGuiColors.WarningForeground * new Vector4(1, 1, 1, 0.3f));
        ImGui.GetForegroundDrawList().AddRectFilled(itemRectMin, itemRectMax, addChildColor, ImDrawFlags.None);
    }
}

public enum TreeDragMode {
    Insert,
    Attach,
    Any
}

public enum TreeActionKind : byte {
    None,
    InsertBefore,
    InsertAfter,
    Attach,
}
