using System;
using System.Collections.Generic;
using System.Linq;
using HUD_Manager.Structs;
using HUDManager.Structs.External;
using Newtonsoft.Json;

namespace HUD_Manager.Configuration {
    [Serializable]
    public class SavedLayout {
        public Dictionary<ElementKind, Element> Elements { get; }
        public Dictionary<string, Window> Windows { get; }

        // The original approach was to have a superclass "ExternalElement" but this causes some weird issues with deserialization, in
        //  which Dalamud would reset the BrowsingwayOverlay to an ExternalElement, wiping all the data in the process.
        public List<BrowsingwayOverlay> BrowsingwayOverlays { get; } = new List<BrowsingwayOverlay>();

        // public Dictionary<string, Vector2<short>> Positions { get; private set; }

        public Guid Parent { get; set; } = Guid.Empty;

        public string Name { get; set; }

        [JsonConstructor]
        public SavedLayout(string name, Dictionary<ElementKind, Element> elements, Dictionary<string, Window> windows, Guid parent) {
            this.Name = name;
            this.Elements = elements;
            this.Windows = windows;
            this.Parent = parent;
        }

        public SavedLayout(string name, Dictionary<ElementKind, Element> elements, Dictionary<string, Window> windows, List<BrowsingwayOverlay> overlays, Guid parent) : this(name, elements, windows, parent) {
            this.BrowsingwayOverlays = overlays;
        }

        public SavedLayout(string name, Layout hud, Dictionary<string, Window> windows) {
            this.Name = name;
            this.Elements = hud.ToDictionary();
            this.Windows = windows;
        }

        public SavedLayout(string name, Layout hud) {
            this.Name = name;
            this.Elements = hud.ToDictionary();
            this.Windows = new Dictionary<string, Window>();
        }

        public Layout ToLayout() {
            var elements = this.Elements.Values.ToList();

            while (elements.Count < Hud.InMemoryLayoutElements) {
                elements.Add(new Element(new RawElement()));
            }

            return new Layout {
                elements = elements.Select(elem => new RawElement(elem)).ToArray(),
            };
        }
    }
}
