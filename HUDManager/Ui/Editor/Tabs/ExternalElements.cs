using Dalamud.Interface;
using HUD_Manager;
using HUD_Manager.Configuration;
using HUD_Manager.Ui;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
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
        }

        public interface IExternalElement
        {
            public void AddButtonToList(SavedLayout layout, ref bool update);
            public void DrawControls(SavedLayout layout, ref bool update);
        }

        private readonly IExternalElement[] Elements =
        {
            new Browsingway(), 
            new CrossUp()
        };

        internal void Draw(SavedLayout layout, ref bool update)
        {
            foreach (var e in Elements) e.AddButtonToList(layout, ref update);

            if (!ImGui.BeginChild("uimanager-overlay-edit", new Vector2(0, 0), true)) return;
            
            foreach (var e in Elements) e.DrawControls(layout, ref update);

            if (update)
            {
                this.Plugin.Hud.WriteEffectiveLayout(this.Plugin.Config.StagingSlot, this.Ui.SelectedLayout);
                this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
            }

            ImGui.EndChild();
        }
    }
}
