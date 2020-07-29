using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;

namespace HudSwap {
    public class HUD {
        private delegate IntPtr GetFilePointerDelegate(byte index);
        private delegate uint SetHudLayoutDelegate(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1);

        private GetFilePointerDelegate _getFilePointer;
        private SetHudLayoutDelegate _setHudLayout;

        private readonly DalamudPluginInterface pi;

        public HUD(DalamudPluginInterface pi) {
            this.pi = pi;
            IntPtr getFilePointerPtr = this.pi.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 14 83 7B 44 00");
            IntPtr setHudLayoutPtr = this.pi.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 EB 15");
            if (getFilePointerPtr != IntPtr.Zero) {
                this._getFilePointer = Marshal.GetDelegateForFunctionPointer<GetFilePointerDelegate>(getFilePointerPtr);
            }
            if (setHudLayoutPtr != IntPtr.Zero) {
                this._setHudLayout = Marshal.GetDelegateForFunctionPointer<SetHudLayoutDelegate>(setHudLayoutPtr);
            }
        }

        private IntPtr GetFilePointer(byte index) {
            return this._getFilePointer.Invoke(index);
        }

        public uint SetHudLayout(uint hudLayout) {
            IntPtr file = this.GetFilePointer(0);
            return this._setHudLayout.Invoke(file, hudLayout, 0, 1);
        }
    }
}
