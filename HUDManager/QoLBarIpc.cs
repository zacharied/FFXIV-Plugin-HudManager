using Dalamud.Logging;
using Dalamud.Plugin.Ipc;
using HUDManager.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HUD_Manager;

public class QoLBarIpc
{
    public const int IndexUnset = -1;
    public const int IndexRemoved = -2;

    private readonly Plugin _plugin;
    private string[] _conditionList = Array.Empty<string>();
    private Dictionary<int, ConditionState> _cache = new();

    private readonly ICallGateSubscriber<object> _qolBarInitializedSubscriber;
    private readonly ICallGateSubscriber<object> _qolBarDisposedSubscriber;
    private readonly ICallGateSubscriber<string> _qolBarGetVersionSubscriber;
    private readonly ICallGateSubscriber<int> _qolBarGetIpcVersionSubscriber;
    private readonly ICallGateSubscriber<string[]> _qolBarGetConditionSetsProvider;
    private readonly ICallGateSubscriber<int, bool> _qolBarCheckConditionSetProvider;
    private readonly ICallGateSubscriber<int, int, object> _qolBarMovedConditionSetProvider;
    private readonly ICallGateSubscriber<int, object> _qolBarRemovedConditionSetProvider;

    public bool Enabled { get; private set; }

    public string Version
    {
        get
        {
            try {
                return _qolBarGetVersionSubscriber.InvokeFunc();
            }
            catch {
                return "0.0.0.0";
            }
        }
    }

    public int IpcVersion
    {
        get
        {
            try {
                return _qolBarGetIpcVersionSubscriber.InvokeFunc();
            }
            catch {
                return 0;
            }
        }
    }

    public string[] GetConditionSets()
    {
        try {
            _conditionList = _qolBarGetConditionSetsProvider.InvokeFunc();
            return _conditionList;
        }
        catch (Exception e) {
            _plugin.Log.Warning(e, "Error fetching QoL Bar condition sets");
            return Array.Empty<string>();
        }
    }

    public ConditionState GetConditionChange(int index, out ConditionState? oldState)
    {
        if (_cache.TryGetValue(index, out var cachedState)) {
            oldState = cachedState;
        } else {
            oldState = null;
        }

        var state = this.GetConditionState(index);
        _cache[index] = state;
        return state;
    }

    public ConditionState GetConditionState(int index)
    {
        if (!this.Enabled) {
            return ConditionState.ErrorPluginUnavailable;
        }

        if (index < 0) {
            if (index == IndexRemoved) {
                return ConditionState.ErrorConditionRemoved;
            }
            return ConditionState.ErrorConditionNotSet;
        }

        if (index >= _conditionList.Length) {
            return ConditionState.ErrorConditionNotFound;
        }

        try {
            return _qolBarCheckConditionSetProvider.InvokeFunc(index) ? ConditionState.True : ConditionState.False;
        }
        catch {
            return ConditionState.ErrorUnknown;
        }
    }

    private void OnMovedConditionSet(int from, int to)
    {
        _plugin.Log.Debug($"QoL Bar conditions swapped: {from} <-> {to}");

        var changed = false;
        foreach (var condition in _plugin.Config.CustomConditions) {
            if (condition.ConditionType == CustomConditionType.QoLBarCondition) {
                if (condition.ExternalIndex == from) {
                    condition.ExternalIndex = to;
                    changed = true;
                } else if (condition.ExternalIndex == to) {
                    condition.ExternalIndex = from;
                    changed = true;
                }
            }
        }

        if (changed) {
            _plugin.Config.Save();
        }

        this.ClearCache();
    }

    private void OnRemovedConditionSet(int removed)
    {
        _plugin.Log.Debug($"QoL Bar condition removed: {removed}");

        var changed = false;
        foreach (var condition in _plugin.Config.CustomConditions) {
            if (condition.ConditionType == CustomConditionType.QoLBarCondition && condition.ExternalIndex == removed) {
                condition.ExternalIndex = -2;
                changed = true;
            }
        }

        if (changed) {
            _plugin.Config.Save();
        }

        this.ClearCache();
    }

    public void ClearCache()
    {
        _cache = new();
        if (this.Enabled) {
            this.GetConditionSets();
        } else {
            _conditionList = Array.Empty<string>();
        }
    }

    public QoLBarIpc(Plugin plugin)
    {
        _plugin = plugin;

        _qolBarInitializedSubscriber = plugin.Interface.GetIpcSubscriber<object>("QoLBar.Initialized");
        _qolBarDisposedSubscriber = plugin.Interface.GetIpcSubscriber<object>("QoLBar.Disposed");
        _qolBarGetIpcVersionSubscriber = plugin.Interface.GetIpcSubscriber<int>("QoLBar.GetIPCVersion");
        _qolBarGetVersionSubscriber = plugin.Interface.GetIpcSubscriber<string>("QoLBar.GetVersion");
        _qolBarGetConditionSetsProvider = plugin.Interface.GetIpcSubscriber<string[]>("QoLBar.GetConditionSets");
        _qolBarCheckConditionSetProvider = plugin.Interface.GetIpcSubscriber<int, bool>("QoLBar.CheckConditionSet");
        _qolBarMovedConditionSetProvider = plugin.Interface.GetIpcSubscriber<int, int, object>("QoLBar.MovedConditionSet");
        _qolBarRemovedConditionSetProvider = plugin.Interface.GetIpcSubscriber<int, object>("QoLBar.RemovedConditionSet");

        _qolBarInitializedSubscriber.Subscribe(this.Enable);
        _qolBarDisposedSubscriber.Subscribe(this.Disable);
        _qolBarMovedConditionSetProvider.Subscribe(this.OnMovedConditionSet);
        _qolBarRemovedConditionSetProvider.Subscribe(this.OnRemovedConditionSet);

        this.Enable();
    }

    private void Enable()
    {
        if (this.IpcVersion != 1) {
            return;
        }

        _plugin.Log.Debug("Enabling QoLBar IPC");
        this.Enabled = true;
        this.ClearCache();
    }

    private void Disable()
    {
        if (!this.Enabled) {
            return;
        }

        _plugin.Log.Debug("Disabling QoLBar IPC");
        this.Enabled = false;
        this.ClearCache();
    }

    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
    public void Dispose()
    {
        this.Enabled = false;
        _qolBarInitializedSubscriber?.Unsubscribe(this.Enable);
        _qolBarDisposedSubscriber?.Unsubscribe(this.Disable);
        _qolBarMovedConditionSetProvider?.Unsubscribe(this.OnMovedConditionSet);
        _qolBarRemovedConditionSetProvider?.Unsubscribe(this.OnRemovedConditionSet);
    }
}

public enum ConditionState
{
    False = 0,
    True = 1,
    ErrorConditionNotSet = 2,
    ErrorPluginUnavailable = 3,
    ErrorConditionRemoved = 4,
    ErrorConditionNotFound = 5,
    ErrorUnknown = 6,
}
