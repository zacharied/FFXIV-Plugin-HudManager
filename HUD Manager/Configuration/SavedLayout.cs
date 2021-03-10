using System;
using System.Collections.Generic;
using System.Linq;
using HUD_Manager.Structs;
using Newtonsoft.Json;

namespace HUD_Manager.Configuration {
    [Serializable]
    public class SavedLayout {
        public Dictionary<ElementKind, Element> Elements { get; }
        public Dictionary<string, Vector2<short>> Positions { get; private set; }
        public Guid Parent { get; set; } = Guid.Empty;

        public string Name { get; set; }

        [JsonConstructor]
        public SavedLayout(string name, Dictionary<ElementKind, Element> elements, Dictionary<string, Vector2<short>> positions, Guid parent) {
            this.Name = name;
            this.Elements = elements;
            this.Positions = positions;
            this.Parent = parent;
        }

        public SavedLayout(string name, Layout hud, Dictionary<string, Vector2<short>> positions) {
            this.Name = name;
            this.Elements = hud.ToDictionary();
            this.Positions = positions;
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
