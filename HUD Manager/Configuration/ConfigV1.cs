using System;
using System.Collections.Generic;

namespace HUD_Manager.Configuration
{
    [Serializable]
    public class ConfigV1
    {
        public bool FirstRun { get; set; }
        public bool UnderstandsRisks { get; set; }
        public bool SwapsEnabled { get; set; }
        public HudSlot StagingSlot { get; set; }
        public Dictionary<Guid, ConfigV1Layout> Layouts2 { get; set; } = null!;
    }

    [Serializable]
    public class ConfigV1Layout
    {
        public string Name { get; set; } = null!;
        public Dictionary<string, Vector2<short>> Positions { get; set; } = null!;
        public byte[] Hud { get; set; } = null!;
    }
}
