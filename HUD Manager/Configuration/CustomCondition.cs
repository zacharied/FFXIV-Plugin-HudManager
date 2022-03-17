using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
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
        public MultiCondition MultiCondition { get; set; } = new();

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

                case CustomConditionType.MultiCondition:
                    return MultiCondition!.IsActive(plugin);

                default:
                    throw new InvalidOperationException("invalid condition type");
            }
        }
    }

    /// <summary>
    /// A container class that can store several different condition types.
    /// All fields are immutable and only one may be assigned at a time.
    /// </summary>
    [Serializable]
    public struct CustomConditionUnion
    {
        public CustomCondition? Custom { get; init; } = null;
        public Status? Game { get; init; } = null;
        public ClassJobCategoryId? ClassJob { get; init; } = null;

        public CustomConditionUnion() {
            throw new ArgumentException("one of the union members must be defined");
        }

        public CustomConditionUnion(CustomCondition cond)
        {
            Custom = cond ?? throw new ArgumentNullException(nameof(cond));
        }

        public CustomConditionUnion(Status cond)
        {
            Game = cond;
        }

        public CustomConditionUnion(ClassJobCategoryId cond)
        {
            ClassJob = cond;
        }

        public Type CurrentType =>
            Custom is not null ? typeof(CustomCondition) :
            Game is not null ? typeof(Status) :
            ClassJob is not null ? typeof(ClassJobCategoryId) :
            throw new InvalidOperationException("no members of union are defined");

        public bool IsActive(Plugin plugin)
        {
            if (CurrentType == typeof(CustomCondition)) {
                return Custom!.IsMet(plugin);
            } else if (CurrentType == typeof(Status)) {
                return Game!.Value.Active(plugin);
            } else if (CurrentType == typeof(ClassJobCategoryId)) {
                var player = plugin.ClientState.LocalPlayer;
                if (player is null || player.ClassJob.GameData is null)
                    return false;
                return ClassJob!.Value.IsActivated(player.ClassJob.GameData);
            }
            throw new InvalidOperationException("no members of union are defined");
        }

        public string UiName(Plugin plugin, bool partial = false) =>
            CurrentType == typeof(CustomCondition) ? Custom!.Name :
            CurrentType == typeof(Status) ? Game!.Value.Name() :
            CurrentType == typeof(ClassJobCategoryId) ? "Class/Job" + (partial ? string.Empty : $": {ClassJob!.Value.DisplayName(plugin)}") :
            throw new InvalidOperationException("no members of union are defined");
    }

    public enum CustomConditionType
    {
        ConsoleToggle,
        HoldToActivate,
        MultiCondition
    }

    public static class CustomConditionTypeExt
    {
        public static string DisplayName(this CustomConditionType type)
            => type switch
            {
                CustomConditionType.ConsoleToggle => "Toggle by command",
                CustomConditionType.HoldToActivate => "Hold key",
                CustomConditionType.MultiCondition => "Multiple conditions",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
    }
}