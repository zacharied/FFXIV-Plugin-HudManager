using System;
using System.Collections.Generic;

namespace HudSwap {
    [Serializable]
    public class Layout {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
        public byte[] Hud { get; private set; }
        public Dictionary<string, Vector2<short>> Positions { get; private set; }

        public string Name { get; private set; }

        public Layout(string name, byte[] hud, Dictionary<string, Vector2<short>> positions) {
            this.Name = name;
            this.Hud = hud;
            this.Positions = positions;
        }
    }
}
