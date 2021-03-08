using System;
using System.Runtime.InteropServices;

namespace HUD_Manager {
    public class GameFunctions {
        private delegate IntPtr GetUiBaseDelegate();
        private delegate IntPtr GetUiWindowDelegate(IntPtr uiBase, string uiName, int index);
        private delegate void MoveWindowDelegate(IntPtr windowBase, short x, short y);

        private readonly GetUiBaseDelegate _getUiBase;
        private readonly GetUiWindowDelegate _getUiWindow;
        private readonly MoveWindowDelegate _moveWindow;

        private Plugin Plugin { get; }

        public GameFunctions(Plugin plugin) {
            this.Plugin = plugin;

            var getUiBasePtr = this.Plugin.Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 41 b8 01 00 00 00 48 8d 15 ?? ?? ?? ?? 48 8b 48 20 e8 ?? ?? ?? ?? 48 8b cf");
            var getUiWindowPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("e8 ?? ?? ?? ?? 48 8b cf 48 89 87 ?? ?? 00 00 e8 ?? ?? ?? ?? 41 b8 01 00 00 00");
            var moveWindowPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 83 BB ?? ?? ?? ?? 00 74 ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 ?? E8 ?? ?? ?? ??");

            this._getUiBase = Marshal.GetDelegateForFunctionPointer<GetUiBaseDelegate>(getUiBasePtr);
            this._getUiWindow = Marshal.GetDelegateForFunctionPointer<GetUiWindowDelegate>(getUiWindowPtr);
            this._moveWindow = Marshal.GetDelegateForFunctionPointer<MoveWindowDelegate>(moveWindowPtr);
        }

        private IntPtr GetUiBase() {
            return this._getUiBase.Invoke();
        }

        private IntPtr GetUiWindow(string uiName, int index) {
            var uiBase = this.GetUiBase();
            var offset = Marshal.ReadIntPtr(uiBase, 0x20);

            return this._getUiWindow.Invoke(offset, uiName, index);
        }

        public void MoveWindow(string uiName, short x, short y) {
            var windowBase = this.GetUiWindow(uiName, 1);
            if (windowBase == IntPtr.Zero) {
                return;
            }

            this._moveWindow.Invoke(windowBase, x, y);
        }

        public Vector2<short>? GetWindowPosition(string uiName) {
            var windowBase = this.GetUiWindow(uiName, 1);
            if (windowBase == IntPtr.Zero) {
                return null;
            }

            var x = Marshal.ReadInt16(windowBase + 0x1bc);
            var y = Marshal.ReadInt16(windowBase + 0x1bc + 2);

            return new Vector2<short>(x, y);
        }
    }
}
