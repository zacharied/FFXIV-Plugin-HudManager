using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace HUDManager.Ui;

internal class FirstUseWarning
{
    private readonly Plugin _plugin;

    public FirstUseWarning(Plugin plugin)
    {
        _plugin = plugin;
    }

    public void Draw(ref bool update)
    {
        using var tab = ImRaii.TabItem("About");
        if (!tab) return;

        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "Read this first");
        ImGui.Separator();
        using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 20f)) {
            ImGui.Text("HUD Manager will use the configured staging slot as its own slot to make changes to. This means the staging slot will be overwritten whenever any swap happens.");
            ImGui.Spacing();
            ImGui.Text("When HUD Manager is enabled, making changes to the HUD layout with the in-game HUD Layout editor is strongly discouraged."
                       + "\nChanges may be lost no matter which slot. HUD Manager provides all the features of that editor and more, so please use HUD Manager's editor instead.");
            ImGui.Spacing();
            ImGui.Text("If you are a new user, HUD Manager auto-imported your existing layouts on startup.");
            ImGui.Spacing();
            ImGui.Text("Finally, HUD Manager is beta software. Back up your character data before using this plugin. You may lose some to all of your HUD layouts while testing this plugin.");
            ImGui.Separator();
            ImGui.Text("If you have read all of the above and are okay with continuing, check the box below to enable HUD Manager. You only need to do this once.");
        }
        var understandsRisks = _plugin.Config.UnderstandsRisks;
        if (ImGui.Checkbox("I understand", ref understandsRisks)) {
            _plugin.Config.UnderstandsRisks = understandsRisks;
            update = true;
        }
    }
}
