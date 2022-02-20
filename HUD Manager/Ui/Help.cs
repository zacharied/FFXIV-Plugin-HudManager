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

        internal void Draw()
        {
            if (!ImGui.BeginTabItem("Help")) {
                return;
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
