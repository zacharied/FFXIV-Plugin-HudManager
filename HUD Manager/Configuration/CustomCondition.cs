using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using HUD_Manager;
using Newtonsoft.Json;
using System;

namespace HUDManager.Configuration
{
    [Serializable]
    [JsonObject(IsReference = true)]
    public class CustomCondition
    {
        public string Name { get; set; }

        public CustomConditionType ConditionType { get; set; } = CustomConditionType.ConsoleToggle;

        public VirtualKey ModifierKeyCode { get; set; } = VirtualKey.NO_KEY;
        public VirtualKey KeyCode { get; set; } = VirtualKey.NO_KEY;

        [JsonConstructor]
        private CustomCondition(string name)
        {
            Name = name;
        }

        public CustomCondition(string name, Plugin plugin) : this(name)
        { 
            // Auto-add self to the dictionary
            plugin.Statuses.CustomConditionStatus[this] = default;
        }

        public bool IsMet(Plugin plugin)
        {
            switch (this.ConditionType) {
                case CustomConditionType.ConsoleToggle:
                    if (plugin.Statuses.CustomConditionStatus.TryGetValue(this, out bool val))
                        return val;
                    throw new InvalidOperationException($"no entry for condition {Name} in CustomConditionStatus");

                case CustomConditionType.HoldToActivate:
                    return plugin.Keybinder.KeybindIsPressed(this.KeyCode, this.ModifierKeyCode);

                default:
                    throw new InvalidOperationException("invalid condition type");
            }
        }
    }

    public enum CustomConditionType
    {
        ConsoleToggle,
        HoldToActivate
    }

    public static class CustomConditionTypeExt
    {
        public static string DisplayName(this CustomConditionType type)
            => type switch
            {
                CustomConditionType.ConsoleToggle => "Toggle by command",
                CustomConditionType.HoldToActivate => "Hold key",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
    }
}