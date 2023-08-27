using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using HUD_Manager;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HUDManager.Configuration
{
    [Serializable]
    [JsonObject(IsReference = true)]
    public class CustomCondition
    {
        public string Name { get; set; }
        public string DisplayName => $"{Name}";

        public CustomConditionType ConditionType { get; set; } = CustomConditionType.ConsoleToggle;

        public VirtualKey ModifierKeyCode { get; set; } = VirtualKey.NO_KEY;
        public VirtualKey KeyCode { get; set; } = VirtualKey.NO_KEY;
        public MultiCondition MultiCondition { get; set; } = new();
        public int ExternalIndex { get; set; } = QoLBarIpc.IndexUnset;
        public bool Negate { get; set; }
        public List<uint> MapIds { get; set; } = new();
        public float HoldTime { get; set; }

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

                case CustomConditionType.InZone:
                    var playerMap = Map.GetRootZoneId(plugin.DataManager, plugin.ClientState.TerritoryType);
                    return playerMap is not null ? this.MapIds.Contains(playerMap.Value) : false;

                case CustomConditionType.QoLBarCondition:
                    if (this.Negate) {
                        return plugin.QoLBarIpc.GetConditionState(this.ExternalIndex) == ConditionState.False;
                    }
                    return plugin.QoLBarIpc.GetConditionState(this.ExternalIndex) == ConditionState.True;

                case CustomConditionType.MultiCondition:
                    return MultiCondition!.IsActive(plugin);

                default:
                    throw new InvalidOperationException("invalid condition type");
            }
        }

        public ConditionState? IpcState(Plugin plugin)
        {
            switch (this.ConditionType) {
                case CustomConditionType.QoLBarCondition:
                    return plugin.QoLBarIpc.GetConditionState(this.ExternalIndex);
                default:
                    return null;
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
            throw new CustomConditionUnionUndefinedException();
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

        [JsonConstructor]
        public CustomConditionUnion(CustomCondition? Custom, Status? Game, ClassJobCategoryId? ClassJob)
        {
            if (Custom is not null)
                this.Custom = Custom;
            else if (Game is not null)
                this.Game = Game;
            else if (ClassJob is not null)
                this.ClassJob = ClassJob;
            else
                throw new CustomConditionUnionUndefinedException();
        }

        [JsonIgnore]
        public Type CurrentType =>
            Custom is not null ? typeof(CustomCondition) :
            Game is not null ? typeof(Status) :
            ClassJob is not null ? typeof(ClassJobCategoryId) :
            throw new CustomConditionUnionUndefinedException();

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
            throw new CustomConditionUnionUndefinedException();
        }

        public string UiName(Plugin plugin, bool partial = false) =>
            CurrentType == typeof(CustomCondition) ? Custom!.DisplayName :
            CurrentType == typeof(Status) ? Game!.Value.Name() :
            CurrentType == typeof(ClassJobCategoryId) ? "Class/Job" + (partial ? string.Empty : $"  {ClassJob!.Value.DisplayName(plugin)}") :
            throw new CustomConditionUnionUndefinedException();

        /// <summary>
        /// Thrown when an attempt is made to instantiate or access a <see cref="CustomConditionUnion"/> with no members defined.
        /// </summary>
        public class CustomConditionUnionUndefinedException : InvalidOperationException {
            public CustomConditionUnionUndefinedException()
                : base($"no members of union are defined")
            { }
        }
    }

    public enum CustomConditionType
    {
        ConsoleToggle = 0,
        HoldToActivate = 1,
        InZone = 3,
        QoLBarCondition = 4,
        MultiCondition = 2,
    }

    public static class CustomConditionTypeExt
    {
        public static string DisplayName(this CustomConditionType type)
            => type switch
            {
                CustomConditionType.ConsoleToggle => "Toggle by command",
                CustomConditionType.HoldToActivate => "Hold key",
                CustomConditionType.InZone => "In zone",
                CustomConditionType.QoLBarCondition => "QoL Bar condition",
                CustomConditionType.MultiCondition => "Multiple conditions",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

        public static int DisplayOrder(this CustomConditionType type)
            => type switch
            {
                CustomConditionType.ConsoleToggle => 0,
                CustomConditionType.HoldToActivate => 1,
                CustomConditionType.InZone => 2,
                CustomConditionType.QoLBarCondition => 3,
                CustomConditionType.MultiCondition => 99,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
    }
}