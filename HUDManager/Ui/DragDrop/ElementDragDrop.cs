using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using HUDManager.Configuration;
using HUDManager.Structs;

namespace HUDManager.Ui.DragDrop;

public sealed class ElementDragDrop(string payloadId) {
    private readonly DragDropState<uint> state = new(payloadId);
    public string? SourceName { get; set; }
    public SavedLayout? SourceLayout { get; set; }
    public Element? SourceElement { get; set; }
    public bool ElementExists { get; set; }
    private ElementActionKind Action { get; set; } = ElementActionKind.None;

    public void EndFrame() {
        if (state.CheckHover()) {
            Action = GetActionKind();
        } else {
            Action = ElementActionKind.None;
        }

        if (state.CheckActive()) {
            DrawTooltip();
        } else {
            SourceElement = null;
            SourceName = null;
        }
    }

    public DragDisposable Drag() {
        return state.Drag(0);
    }

    public DropDisposable Drop() {
        var result = state.Drop(0);

        if (!result.Success)
            return result.Reject();

        return result.Accept();
    }

    private void DrawTooltip() {
        using var tooltip = ImRaii.Tooltip();
        if (Action is ElementActionKind.None) {
            ImGui.TextColored(ImGuiColors.DalamudGrey, $"Drag ");
            ImGui.SameLine(0, 0);
            ImGui.Text(SourceName);
        } else if (Action is ElementActionKind.Move) {
            ImGui.TextColored(ImGuiColors.WarningForeground, $"Move ");
            ImGui.SameLine(0, 0);
            ImGui.Text(SourceName);
            ShowOverrideAlert();
        } else if (Action is ElementActionKind.Copy) {
            ImGui.TextColored(ImGuiColors.SuccessForeground, $"Copy ");
            ImGui.SameLine(0, 0);
            ImGui.Text(SourceName);
            ShowOverrideAlert();
            ImGui.TextColored(ImGuiColors.DalamudGrey3, "(hold Shift to move)");
        }
    }

    private void ShowOverrideAlert() {
        if (AllowOverwrite)
            ImGui.TextColored(ImGuiColors.SuccessForeground, $"(will overwrite existing element)");
        else if (ElementExists)
            ImGui.TextColored(ImGuiColors.ErrorForeground, $"(element already exists, hold Control to overwrite)");
    }

    public ElementActionKind GetActionKind() {
        if (ImGui.IsKeyDown(ImGuiKey.ModShift))
            return ElementActionKind.Move;
        return ElementActionKind.Copy;
    }

    public bool AllowWrite => !ElementExists || AllowOverwrite;

    private bool AllowOverwrite => ImGui.GetIO().KeyCtrl;
}

public enum ElementActionKind {
    None,
    Copy,
    Move
}
