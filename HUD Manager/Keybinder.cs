using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using HUD_Manager;
using HUDManager.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HUDManager
{
    public class Keybinder
    {
        public readonly ReadOnlyCollection<VirtualKey> ModifierKeys = new List<VirtualKey>()
        {  VirtualKey.CONTROL, VirtualKey.SHIFT, VirtualKey.MENU, VirtualKey.NO_KEY }.AsReadOnly();

        public readonly ReadOnlyCollection<VirtualKey> InputKeys;

        public Dictionary<VirtualKey, bool> InputKeyState { get; } = new();

        private readonly Plugin Plugin;

        public Keybinder(Plugin plugin)
        {
            Plugin = plugin;

            InputKeys = Plugin.KeyState.GetValidVirtualKeys().Cast<VirtualKey>().ToList()
                .Except(ModifierKeys)
                .Prepend(VirtualKey.NO_KEY)
                .ToList().AsReadOnly();
        }

        public bool UpdateKeyState()
        {
            // Returns true if there's a change.
            bool TrySaveKeyState(VirtualKey? key)
            {
                if (key is not null && key.Value != VirtualKey.NO_KEY) {
                    var unchanged = InputKeyState.GetValueOrDefault(key.Value) == Plugin.KeyState[key.Value];
                    InputKeyState[key.Value] = Plugin.KeyState[key.Value];
                    return !unchanged;
                }
                return false;
            }

            bool changed = false;
            foreach (var cond in Plugin.Config.CustomConditions) {
                if (cond.ConditionType != CustomConditionType.HoldToActivate)
                    continue;

                changed |= TrySaveKeyState(cond.ModifierKeyCode);
                changed |= TrySaveKeyState(cond.KeyCode);
            }

            return changed;
        }

        public bool KeybindIsPressed(VirtualKey key, VirtualKey modifier)
        {
            bool GetKeyState(VirtualKey key)
                => key == VirtualKey.NO_KEY || Plugin.KeyState[key];

            // If both keys are NO_KEY then it should be unpressable.
            if (key == VirtualKey.NO_KEY && modifier == VirtualKey.NO_KEY)
                return false;
            if (key != VirtualKey.NO_KEY && modifier != VirtualKey.NO_KEY)
                return GetKeyState(key) && GetKeyState(modifier);
            if (key == VirtualKey.NO_KEY)
                return GetKeyState(modifier);
            else
                return GetKeyState(key);
        }
    }
}
