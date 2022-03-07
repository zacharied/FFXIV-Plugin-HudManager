using HUD_Manager;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HUDManager.Ui
{
    internal class FirstUseWarning
    {
        private readonly Plugin Plugin;

        public FirstUseWarning(Plugin plugin)
        {
            Plugin = plugin;
        }

        public void Draw(ref bool update)
        {
            if (ImGui.BeginTabItem("About")) {
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "Read this first");
                ImGui.Separator();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted("HUD Manager will use the configured staging slot as its own slot to make changes to. This means the staging slot will be overwritten whenever any swap happens.");
                ImGui.Spacing();
                ImGui.TextUnformatted("When HUD Manager is enabled, making changes to the HUD layout with the in-game HUD Layout editor is strongly discouraged."
                    + "\nChanges may be lost no matter which slot. HUD Manager provides all the features of that editor and more, so please use HUD Manager's editor instead.");
                ImGui.Spacing();
                ImGui.TextUnformatted("If you are a new user, HUD Manager auto-imported your existing layouts on startup.");
                ImGui.Spacing();
                ImGui.TextUnformatted("Finally, HUD Manager is beta software. Back up your character data before using this plugin. You may lose some to all of your HUD layouts while testing this plugin.");
                ImGui.Separator();
                ImGui.TextUnformatted("If you have read all of the above and are okay with continuing, check the box below to enable HUD Manager. You only need to do this once.");
                ImGui.PopTextWrapPos();
                var understandsRisks = this.Plugin.Config.UnderstandsRisks;
                if (ImGui.Checkbox("I understand", ref understandsRisks)) {
                    this.Plugin.Config.UnderstandsRisks = understandsRisks;
                    update = true;
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
            ImGui.End();
            return;
        }
       
    }
}
