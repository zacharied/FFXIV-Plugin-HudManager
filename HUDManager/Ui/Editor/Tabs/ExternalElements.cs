using HUD_Manager;
using HUD_Manager.Configuration;
using HUD_Manager.Ui;
using ImGuiNET;
using System.Numerics;

namespace HUDManager.Ui.Editor.Tabs
{
    internal partial class ExternalElements
    {
        private Plugin Plugin { get; }
        private Interface Ui { get; }

        public ExternalElements(Plugin plugin, Interface ui)
        {
            this.Plugin = plugin;
            this.Ui = ui;

            Elements = new IExternalElement[] {
                new Browsingway(this.Plugin), 
                new CrossUp(this.Plugin)
            };
        }

        public interface IExternalElement
        {
            public bool Available();
            public void AddButtonToList(SavedLayout layout, ref bool update, bool available);
            public void DrawControls(SavedLayout layout, ref bool update);
        }

        private readonly IExternalElement[] Elements;

        internal void Draw(SavedLayout layout, ref bool update)
        {
            foreach (var elem in Elements) elem.AddButtonToList(layout, ref update, elem.Available());

            if (!ImGui.BeginChild("uimanager-overlay-edit", new Vector2(0, 0), true)) return;
            
            foreach (var elem in Elements) elem.DrawControls(layout, ref update);

            if (update)
            {
                this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, this.Ui.SelectedLayout);
                this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
            }

            ImGui.EndChild();
        }
    }
}
