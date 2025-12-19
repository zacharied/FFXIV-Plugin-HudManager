using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using HUDManager.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HUDManager;

public class Statuses
{
    private Plugin Plugin { get; }

    public readonly Dictionary<Status, bool> Condition = new();
    private readonly Status[] _statusTypes = Enum.GetValues<Status>();
    private uint _lastJobId = uint.MaxValue;

    public (HudConditionMatch? activeLayout, List<HudConditionMatch> layeredLayouts) ResultantLayout = (null, []);
    private readonly Dictionary<HudConditionMatch, float> _conditionHoldTimers = new();

    public CustomConditionStatusContainer CustomConditionStatus { get; } = new();

    public ForceState NeedsForceUpdate { get; internal set; }

    private long _lastUpdateTime;

    public enum ForceState
    {
        None,
        NoActiveLayout,
        SwapSettingChanged,
        EditLockRemoved,
    }

    public Statuses(Plugin plugin)
    {
        Plugin = plugin;

        foreach (var cond in Plugin.Config.CustomConditions) {
            CustomConditionStatus[cond] = false;
        }
    }

    public bool Update()
    {
        UpdateConditionHoldTimers();

        var player = Plugin.ObjectTable.LocalPlayer; // TODO: Was ClientState.LocalPlayer, consider alternatives
        if (player is null) {
            return false;
        }

        var anyChanged = false;

        var currentJobId = player.ClassJob.RowId;
        if (_lastJobId != currentJobId) {
            anyChanged = true;
        }

        _lastJobId = currentJobId;

        foreach (var status in _statusTypes) {
            if (Condition.TryGetValue(status, out var oldVal)) {
                var newVal = status.Active(Plugin, player);
                if (newVal != oldVal) {
                    anyChanged = true;
                    Condition[status] = newVal;
                }
            }
            else {
                var newVal = status.Active(Plugin, player);
                anyChanged |= newVal != oldVal;
                Condition[status] = newVal;
            }
        }

        return anyChanged;
    }

    /// <summary>
    /// Get the current layout data according to the conditions that match the game state.
    /// </summary>
    private (HudConditionMatch? layoutId, List<HudConditionMatch> layers) CalculateResultantLayout()
    {
        List<HudConditionMatch> layers = [];
        var player = Plugin.ObjectTable.LocalPlayer; // TODO: Was ClientState.LocalPlayer, consider alternatives
        if (player == null) {
            return (null, layers);
        }

        foreach (var match in Plugin.Config.HudConditionMatches) {
            var isActivated = match.IsActivated(Plugin, out var transitioned);
            var startTimer = !isActivated && transitioned && match.CustomCondition?.HoldTime > 0;
            if (isActivated || startTimer) {
                if (startTimer) {
                    Plugin.Log.Debug($"Starting timer for \"{match.CustomCondition?.Name}\" ({match.CustomCondition?.HoldTime}s)");
                    _conditionHoldTimers[match] = match.CustomCondition?.HoldTime ?? 0;
                }

                if (match.IsLayer && Plugin.Config.AdvancedSwapMode) {
                    layers.Add(match);
                    continue;
                }

                // The first non-layer condition is the base
                return (match, layers);
            }
        }

        return (null, layers);
    }

    public void SetHudLayout()
    {
        var forceState = NeedsForceUpdate;
        NeedsForceUpdate = ForceState.None;

        ResultantLayout = CalculateResultantLayout();
        if (ResultantLayout.activeLayout is null) {
            NeedsForceUpdate = ForceState.NoActiveLayout;
            return;
        }

        if (!Plugin.Config.Layouts.ContainsKey(ResultantLayout.activeLayout.LayoutId)) {
            Plugin.Log.Error($"Attempt to set nonexistent layout \"{ResultantLayout.activeLayout.LayoutId}\".");
            return;
        }

        if (forceState != ForceState.None) {
            Plugin.Log.Debug($"Forcing full layout write (reason={forceState})");
            Plugin.Hud.WriteEffectiveLayout(Plugin.Config.StagingSlot, ResultantLayout.activeLayout.LayoutId, ResultantLayout.layeredLayouts.ConvertAll(match => match.LayoutId));
        } else {
            Plugin.Hud.WriteEffectiveLayoutIfChanged(Plugin.Config.StagingSlot, ResultantLayout.activeLayout.LayoutId, ResultantLayout.layeredLayouts.ConvertAll(match => match.LayoutId));
        }
        //this.Plugin.Hud.SelectSlot(this.Plugin.Config.StagingSlot, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsInFate()
    {
        return FateManager.Instance()->CurrentFate != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsLevelSynced()
    {
        var uiPlayerState = UIState.Instance()->PlayerState;
        return uiPlayerState.IsLevelSynced;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsInSanctuary()
    {
        var ti = TerritoryInfo.Instance();
        return ti != null && ti->InSanctuary;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsChatFocused()
    {
        return RaptureAtkModule.Instance()->AtkModule.IsTextInputActive();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsRoleplaying(Plugin plugin, IPlayerCharacter? player)
    {
        player ??= plugin.ObjectTable.LocalPlayer;
        if (player == null)
            return false;
        return ((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player.Address)->OnlineStatus == 22;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWeaponOut(Plugin plugin, IPlayerCharacter? player)
    {
        player ??= plugin.ObjectTable.LocalPlayer;
        if (player == null)
            return false;
        return (player.StatusFlags & StatusFlags.WeaponOut) != 0;
    }

    private void UpdateConditionHoldTimers()
    {
        // Update the timers on all ticking conditions.
        var newTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        var removeKeys = new List<HudConditionMatch>();
        foreach (var (k, v) in _conditionHoldTimers) {
            var newVal = v - (float)((double)(newTimestamp - _lastUpdateTime) / 1000);
            if (newVal < 0) {
                Plugin.Log.Debug($"Condition timer for \"{k.CustomCondition?.Name}\" finished");
                removeKeys.Add(k);
            }
            else {
                _conditionHoldTimers[k] = newVal;
            }
        }

        // If any conditions finished their timers, we update the HUD layout.
        if (removeKeys.Count != 0) {
            foreach (var k in removeKeys) {
                _conditionHoldTimers.Remove(k);
            }
            SetHudLayout();
        }

        _lastUpdateTime = newTimestamp;
    }

    public bool ConditionHoldTimerIsTicking(HudConditionMatch cond)
        => _conditionHoldTimers.ContainsKey(cond);

    public class CustomConditionStatusContainer
    {
        private Dictionary<CustomCondition, bool> Status { get; } = new();
        private bool Updated { get; set; }

        public bool this[CustomCondition c]
        {
            get => Status[c];

            set
            {
                Status[c] = value;
                Updated = true;
            }
        }

        public bool IsUpdated()
        {
            var v = Updated;
            Updated = false;
            return v;
        }

        public bool ContainsKey(CustomCondition c) => Status.ContainsKey(c);

        public bool TryGetValue(CustomCondition c, out bool v) => Status.TryGetValue(c, out v);

        public void Toggle(CustomCondition c) => this[c] = !this[c];
    }
}

public class HudConditionMatch
{
    public ClassJobCategoryId? ClassJobCategory { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public Status? Status { get; set; }
    public CustomCondition? CustomCondition { get; set; }

    public Guid LayoutId { get; set; }

    public bool IsLayer { get; set; }

    private bool LastValue { get; set; }

    public bool IsActivated(Plugin plugin, out bool transitioned)
    {
        transitioned = false;


        if (plugin.PlayerState.ClassJob.ValueNullable is not { } classJob) {
            plugin.Log.Warning("can't check job activation when player is null");
            return false;
        }

        var statusMet = !Status.HasValue || plugin.Statuses.Condition[Status.Value];
        var customConditionMet = CustomCondition?.IsMet(plugin) ?? true;
        var jobMet = ClassJobCategory is null
            || ClassJobCategory.Value.IsActivated(classJob);

        var newValue = statusMet && customConditionMet && jobMet;
        if (LastValue != newValue) {
            transitioned = true;
        }

        LastValue = newValue;
        return newValue;
    }

    public HudConditionMatch Clone()
    {
        var clone = new HudConditionMatch
        {
            ClassJobCategory = ClassJobCategory,
            Status = Status,
            CustomCondition = CustomCondition,
            LayoutId = LayoutId,
            IsLayer = IsLayer,
            LastValue = LastValue,
        };
        return clone;
    }
}

// Note: Changing the names of these is a breaking change
public enum Status
{
    InCombat = ConditionFlag.InCombat,
    InInstance = ConditionFlag.BoundByDuty,
    Crafting = ConditionFlag.Crafting,
    Gathering = ConditionFlag.Gathering,
    Fishing = ConditionFlag.Fishing,
    Mounted = ConditionFlag.Mounted,
    WeaponDrawn = -1,
    Roleplaying = -2,
    PlayingMusic = -3,
    InPvp = -4,
    InDialogue = -5,
    InFate = -6,
    InFateLevelSynced = -7,
    InSanctuary = -8,
    ChatFocused = -9,
    InputModeKbm = -10,
    InputModeGamepad = -11,
    Windowed = -12,
    FullScreen = -13,
}

public static class StatusExtensions
{
    public static string Name(this Status status)
    {
        return status switch
        {
            Status.InCombat => "In combat",
            Status.WeaponDrawn => "Weapon drawn",
            Status.InInstance => "In instance",
            Status.Crafting => "Crafting",
            Status.Gathering => "Gathering",
            Status.Fishing => "Fishing",
            Status.Mounted => "Mounted",
            Status.Roleplaying => "Roleplaying",
            Status.PlayingMusic => "Performing music",
            Status.InPvp => "In PvP",
            Status.InDialogue => "In dialogue",
            Status.InFate => "In FATE area",
            Status.InFateLevelSynced => "Level-synced for FATE",
            Status.InSanctuary => "In a sanctuary",
            Status.ChatFocused => "Chat focused",
            Status.InputModeKbm => "Keyboard/mouse mode",
            Status.InputModeGamepad => "Gamepad mode",
            Status.Windowed => "Windowed",
            Status.FullScreen => "Full Screen",
            _ => throw new ApplicationException($"No name was set up for {status}"),
        };

    }

    public static bool Active(this Status status, Plugin plugin, IPlayerCharacter? player = null)
    {
        if (status > 0) {
            return plugin.Condition[(ConditionFlag)status];
        }

        switch (status) {
            case Status.WeaponDrawn:
                return Statuses.IsWeaponOut(plugin, player);
            case Status.Roleplaying:
                return Statuses.IsRoleplaying(plugin, player);
            case Status.PlayingMusic:
                return plugin.Condition[ConditionFlag.Performing];
            case Status.InPvp:
                return plugin.ClientState.IsPvP;
            case Status.InDialogue:
                return plugin.Condition[ConditionFlag.OccupiedInEvent]
                    | plugin.Condition[ConditionFlag.OccupiedInQuestEvent]
                    | plugin.Condition[ConditionFlag.OccupiedSummoningBell];
            case Status.InFate:
                return Statuses.IsInFate();
            case Status.InFateLevelSynced:
                return Statuses.IsInFate() && Statuses.IsLevelSynced();
            case Status.InSanctuary:
                return Statuses.IsInSanctuary();
            case Status.ChatFocused:
                return Statuses.IsChatFocused();
            case Status.InputModeKbm:
                return !Util.GamepadModeActive(plugin);
            case Status.InputModeGamepad:
                return Util.GamepadModeActive(plugin);
            case Status.Windowed:
                return !Util.FullScreen(plugin);
            case Status.FullScreen:
                return Util.FullScreen(plugin);
            default:
                plugin.Log.Warning($"Unknown status: {status}, returning false");
                return false;
        }
    }
}
