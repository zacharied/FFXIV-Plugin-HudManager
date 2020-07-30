using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;

namespace HudSwap {
    public class HUD {
        private const int LAYOUT_SIZE = 0xb40;

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

        public uint SelectSlot(HudSlot slot, bool force = false) {
            IntPtr file = this.GetFilePointer(0);
            if (force) {
                IntPtr currentSlotPtr = this.GetDataPointer() + 0x5958;
                uint currentSlot = (uint)Marshal.ReadInt32(currentSlotPtr);
                if (currentSlot < 3) {
                    currentSlot += 1;
                } else {
                    currentSlot = 0;
                }
                Marshal.WriteInt32(currentSlotPtr, (int)currentSlot);
            }
            return this._setHudLayout.Invoke(file, (uint)slot, 0, 1);
        }

        private IntPtr GetDataPointer() {
            IntPtr dataPtr = this.GetFilePointer(0) + 0x50;
            return Marshal.ReadIntPtr(dataPtr);
        }

        private IntPtr GetLayoutPointer(HudSlot slot) {
            int slotNum = (int)slot;
            return this.GetDataPointer() + 0x2c58 + (slotNum * LAYOUT_SIZE);
        }

        public byte[] ReadLayout(HudSlot slot) {
            IntPtr slotPtr = this.GetLayoutPointer(slot);
            byte[] bytes = new byte[LAYOUT_SIZE];
            Marshal.Copy(slotPtr, bytes, 0, LAYOUT_SIZE);
            return bytes;
        }

        public void WriteLayout(HudSlot slot, byte[] layout) {
            if (layout.Length != LAYOUT_SIZE) {
                throw new ArgumentException($"layout must be {LAYOUT_SIZE} bytes", nameof(layout));
            }
            IntPtr slotPtr = this.GetLayoutPointer(slot);
            Marshal.Copy(layout, 0, slotPtr, LAYOUT_SIZE);
        }
    }

    public enum HudSlot {
        One = 0,
        Two = 1,
        Three = 2,
        Four = 3,
    }
}
