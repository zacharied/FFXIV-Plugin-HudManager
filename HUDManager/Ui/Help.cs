using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace HUDManager.Ui;

public class Help
{
    private Plugin Plugin { get; }

    public Help(Plugin plugin)
    {
        Plugin = plugin;
    }

    internal void Draw(ref bool update)
    {
        using var tab = ImRaii.TabItem("Help");
        if (!tab) return;

        var hideHelpPanels = Plugin.Config.DisableHelpPanels;
        if (ImGui.Checkbox("Hide help text in plugin menus", ref hideHelpPanels)) {
            Plugin.Config.DisableHelpPanels = hideHelpPanels;
            update = true;
        }

        using var wrapPos = ImRaii.TextWrapPos(0f);

        void DrawHelp(HelpEntry help)
        {
            if (ImGui.CollapsingHeader(help.Name)) {
                if (help.Description is not null) {
                    ImGui.Text(help.Description.Replace("\n", "\n\n"));
                }

                if (help.Help is not null) {
                    ImGui.Spacing();
                    foreach (var subHelp in help.Help) {
                        using var indent = ImRaii.PushIndent();
                        DrawHelp(subHelp);
                    }
                    ImGui.Spacing();
                }
            }
        }

        foreach (var entry in Plugin.Help.Help) {
            DrawHelp(entry);
        }
    }
}
