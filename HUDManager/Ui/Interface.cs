using Dalamud.Interface.Utility;
using HUDManager.Ui.Editor;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace HUDManager.Ui;

public sealed class Interface : Window {
    private Plugin Plugin { get; }

    private LayoutEditor LayoutEditor { get; }
    private Swaps Swaps { get; }
    private Help Help { get; }
    private FirstUseWarning FirstUseWarning { get; }
#if DEBUG
    private Debug Debug { get; }
#endif

    public Guid SelectedLayout { get; set; } = Guid.Empty;

    public Interface(Plugin plugin) : base("HUD Manager Settings") {
        Plugin = plugin;

        LayoutEditor = new LayoutEditor(plugin, this);
        Swaps = new Swaps(plugin);
        Help = new Help(plugin);
        FirstUseWarning = new FirstUseWarning(plugin);
#if DEBUG
        Debug = new Debug(plugin);
#endif

        Size = ImGuiHelpers.ScaledVector2(530, 530);
        SizeConstraints = new WindowSizeConstraints() {
            MinimumSize = ImGuiHelpers.ScaledVector2(530, 530),
            MaximumSize = new Vector2(int.MaxValue, int.MaxValue)
        };
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    internal void Open() {
        IsOpen = true;
    }

    public override void OnClose() {
        Plugin.Swapper.SetEditLock(false);
    }

    public override void Draw() {
        using var tabs = ImRaii.TabBar("##hudmanager-tabs");
        if (!tabs) return;

        var update = false;

        if (!Plugin.Config.UnderstandsRisks) {
            FirstUseWarning.Draw(ref update);
        } else {
            LayoutEditor.Draw();
            Swaps.Draw();
            Help.Draw(ref update);
#if DEBUG
            Debug.Draw();
#endif
        }

        if (update) {
            Plugin.Config.Save();
        }
    }
}
