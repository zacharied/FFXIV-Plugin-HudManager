using System;

namespace HUD_Manager.Structs
{
    [Flags]
    public enum VisibilityFlags : byte
    {
        Keyboard = 1 << 0,
        Gamepad = 1 << 1,
    }
}
