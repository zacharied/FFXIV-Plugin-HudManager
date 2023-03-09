using ImGuiNET;

namespace HUD_Manager.Ui
{
    public class Help
    {
        private Plugin Plugin { get; }

        public Help(Plugin plugin)
        {
            this.Plugin = plugin;
        }

        internal void Draw(ref bool update)
        {
            if (!ImGui.BeginTabItem("Help")) {
                return;
            }

            bool hideHelpPanels = Plugin.Config.DisableHelpPanels;
            if (ImGui.Checkbox("Hide help text in plugin menus", ref hideHelpPanels)) {
                Plugin.Config.DisableHelpPanels = hideHelpPanels;
                update = true;
            }

            ImGui.PushTextWrapPos();

            void DrawHelp(HelpEntry help)
            {
                if (ImGui.CollapsingHeader(help.Name)) {
                    if (help.Description is not null) {
                        ImGui.TextUnformatted(help.Description.Replace("\n", "\n\n"));
                    }
                    
                    if (help.Help is not null) {
                        ImGui.Spacing();
                        foreach (var subHelp in help.Help) {
                            ImGui.Indent();
                            DrawHelp(subHelp);
                            ImGui.Unindent();
                        }
                        ImGui.Spacing();
                    }
                }
            }

            foreach (var entry in this.Plugin.Help.Help) {
                DrawHelp(entry);
            }

            ImGui.PopTextWrapPos();

            ImGui.EndTabItem();
        }
    }
}
