using Dalamud.Logging;
using HUD_Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HUDManager.Structs.External
{
    [Serializable]
    public class BrowsingwayOverlay
    {
        public const BrowsingwayOverlayComponent AllEnabled = 
            BrowsingwayOverlayComponent.Hidden
            | BrowsingwayOverlayComponent.Locked
            | BrowsingwayOverlayComponent.Typethrough
            | BrowsingwayOverlayComponent.Clickthrough;

        public BrowsingwayOverlayComponent Enabled { get; set; } = BrowsingwayOverlayComponent.Hidden;

        public string CommandName = string.Empty;

        public bool Hidden = false;
        public bool Locked = false;
        public bool Typethrough = false;
        public bool Clickthrough = false;

        public BrowsingwayOverlay() { }

        public BrowsingwayOverlay(string name, BrowsingwayOverlayComponent enabled, bool hidden, bool locked, bool typethrough, bool clickthrough)
        {
            CommandName = name;
            Enabled = enabled;
            Hidden = hidden;
            Locked = locked;
            Typethrough = typethrough;
            Clickthrough = clickthrough;
        }

        public void ApplyOverlay(Plugin plugin)
        {
            if (CommandName == string.Empty || CommandName.Any(c => char.IsWhiteSpace(c)))
                return;

            void RunCommand(string parameter, bool option)
            {
                plugin.CommandManager.ProcessCommand($"/bw inlay {CommandName} {parameter} {(option ? "on" : "off")}");
            }

            if (this[BrowsingwayOverlayComponent.Hidden]) {
                RunCommand("hidden", Hidden);
            }
            if (this[BrowsingwayOverlayComponent.Locked]) {
                RunCommand("locked", Locked);
            }
            if (this[BrowsingwayOverlayComponent.Typethrough]) {
                RunCommand("typethrough", Typethrough);
            }
            if (this[BrowsingwayOverlayComponent.Clickthrough]) {
                RunCommand("clickthrough", Clickthrough);
            }
        }

        public bool this[BrowsingwayOverlayComponent component]
        {
            get => (this.Enabled & component) > 0;
            set
            {
                if (value) {
                    this.Enabled |= component;
                } else {
                    this.Enabled &= ~component;
                }
            }
        }

        public BrowsingwayOverlay Clone()
        {
            return new BrowsingwayOverlay(CommandName, Enabled, Hidden, Locked, Typethrough, Clickthrough);
        }

        public void UpdateEnabled(BrowsingwayOverlay other)
        {
            if (other[BrowsingwayOverlayComponent.Hidden]) {
                this.Hidden = other.Hidden;
            }
            if (other[BrowsingwayOverlayComponent.Locked]) {
                this.Locked = other.Locked;
            }
            if (other[BrowsingwayOverlayComponent.Typethrough]) {
                this.Typethrough = other.Typethrough;
            }
            if (other[BrowsingwayOverlayComponent.Clickthrough]) {
                this.Clickthrough = other.Clickthrough;
            }
        }

        [Flags]
        public enum BrowsingwayOverlayComponent
        {
            Hidden = 1 << 0,
            Locked = 1 << 1,
            Typethrough = 1 << 2,
            Clickthrough = 1 << 3
        }
    }
}
