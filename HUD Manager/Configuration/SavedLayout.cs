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

        public string Name { get; private set; }

        [JsonConstructor]
        public SavedLayout(string name, Dictionary<ElementKind, Element> elements, Dictionary<string, Vector2<short>> positions) {
            this.Name = name;
            this.Elements = elements;
            this.Positions = positions;
        }

        public SavedLayout(string name, Layout hud, Dictionary<string, Vector2<short>> positions) {
            this.Name = name;
            this.Elements = hud.ToDictionary();
            this.Positions = positions;
        }

        public Layout ToLayout() {
            var elements = this.Elements.Values.ToList();

            while (elements.Count < 81) {
                elements.Add(new Element());
            }

            return new Layout {
                elements = elements.ToArray(),
            };
        }
    }
}
