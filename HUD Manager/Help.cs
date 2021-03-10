using System;
using System.Collections.Generic;

namespace HUD_Manager {
    [Serializable]
    public class HelpFile {
        public List<HelpEntry> Help { get; set; } = new();
    }

    [Serializable]
    public class HelpEntry {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
