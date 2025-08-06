using HUDManager.Configuration;
using HUDManager.Ui.Editor.Tabs.External;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace HUDManager.Ui.Editor.Tabs;

internal class ExternalElements
{
    private Plugin Plugin { get; }
    private Interface Ui { get; }

    public ExternalElements(Plugin plugin, Interface ui)
    {
        Plugin = plugin;
        Ui = ui;

        _elements =
        [
            new Browsingway(Plugin),
            new CrossUp(Plugin),
        ];
    }

    private readonly IExternalElement[] _elements;

    internal void Draw(SavedLayout layout, ref bool update)
    {
        foreach (var elem in _elements) elem.AddButtonToList(layout, ref update, elem.Available());

        if (!ImGui.BeginChild("uimanager-overlay-edit", new Vector2(0, 0), true)) return;

        foreach (var elem in _elements) elem.DrawControls(layout, ref update);

        if (update)
        {
            Plugin.Hud.WriteEffectiveLayout(Plugin.Config.StagingSlot, Ui.SelectedLayout);
            Plugin.Hud.SelectSlot(Plugin.Config.StagingSlot, true);
        }

        ImGui.EndChild();
    }
}

public interface IExternalElement
{
    public bool Available();
    public void AddButtonToList(SavedLayout layout, ref bool update, bool available);
    public void DrawControls(SavedLayout layout, ref bool update);
}
