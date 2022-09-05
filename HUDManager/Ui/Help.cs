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

            foreach (var entry in this.Plugin.Help.Help) {
                if (ImGui.CollapsingHeader(entry.Name)) {
                    ImGui.TextUnformatted(entry.Description.Replace("\n", "\n\n"));
                }
            }

            ImGui.PopTextWrapPos();

            ImGui.EndTabItem();
        }
    }
}
