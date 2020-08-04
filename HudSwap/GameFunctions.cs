using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HudSwap {
    public class GameFunctions {
        private delegate IntPtr GetUIBaseDelegate();
        private delegate IntPtr GetUIWindowDelegate(IntPtr uiBase, string uiName, int index);
        private delegate void MoveWindowDelegate(IntPtr windowBase, short x, short y);

        private readonly GetUIBaseDelegate getUIBase;
        private readonly GetUIWindowDelegate getUIWindow;
        private readonly MoveWindowDelegate moveWindow;

        public GameFunctions(DalamudPluginInterface pi) {
            if (pi == null) {
                throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
            }

            IntPtr getUIBasePtr = pi.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 41 b8 01 00 00 00 48 8d 15 ?? ?? ?? ?? 48 8b 48 20 e8 ?? ?? ?? ?? 48 8b cf");
            IntPtr getUIWindowPtr = pi.TargetModuleScanner.ScanText("e8 ?? ?? ?? ?? 48 8b cf 48 89 87 ?? ?? 00 00 e8 ?? ?? ?? ?? 41 b8 01 00 00 00");
            IntPtr moveWindowPtr = pi.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 83 BB ?? ?? ?? ?? 00 74 ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 ?? E8 ?? ?? ?? ??");

            if (getUIBasePtr == IntPtr.Zero || getUIWindowPtr == IntPtr.Zero || moveWindowPtr == IntPtr.Zero) {
                throw new ApplicationException("could not get game functions");
            }

            this.getUIBase = Marshal.GetDelegateForFunctionPointer<GetUIBaseDelegate>(getUIBasePtr);
            this.getUIWindow = Marshal.GetDelegateForFunctionPointer<GetUIWindowDelegate>(getUIWindowPtr);
            this.moveWindow = Marshal.GetDelegateForFunctionPointer<MoveWindowDelegate>(moveWindowPtr);
        }

        private IntPtr GetUIBase() {
            return this.getUIBase.Invoke();
        }

        private IntPtr GetUIWindow(string uiName, int index) {
            IntPtr uiBase = this.GetUIBase();
            IntPtr offset = Marshal.ReadIntPtr(uiBase, 0x20);

            return this.getUIWindow.Invoke(offset, uiName, index);
        }

        public void MoveWindow(string uiName, short x, short y) {
            IntPtr windowBase = this.GetUIWindow(uiName, 1);
            if (windowBase == IntPtr.Zero) {
                return;
            }

            this.moveWindow.Invoke(windowBase, x, y);
        }

        public Vector2<short> GetWindowPosition(string uiName) {
            IntPtr windowBase = this.GetUIWindow(uiName, 1);
            if (windowBase == IntPtr.Zero) {
                return null;
            }

            short x = Marshal.ReadInt16(windowBase + 0x1bc);
            short y = Marshal.ReadInt16(windowBase + 0x1bc + 2);

            return new Vector2<short>(x, y);
        }
    }
}
