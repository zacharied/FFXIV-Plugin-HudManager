using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HUD_Manager {
    public class GameFunctions {
        // private delegate IntPtr GetBaseUiObjectDelegate();

        private delegate void SetPositionDelegate(IntPtr windowBase, short x, short y);

        private delegate void SetAlphaDelegate(IntPtr windowBase, byte alpha);

        private delegate byte UpdateAddonPositionDelegate(IntPtr raptureAtkUnitManager, IntPtr addon, byte clicked);

        // private readonly GetBaseUiObjectDelegate _getBaseUiObject;
        private readonly SetPositionDelegate _setPosition;
        private readonly SetAlphaDelegate _setAlpha;
        private readonly UpdateAddonPositionDelegate _updateAddonPosition;

        private Plugin Plugin { get; }

        public GameFunctions(Plugin plugin) {
            this.Plugin = plugin;

            var setPositionPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("4C 8B 89 ?? ?? ?? ?? 41 0F BF C0");
            var setAlphaPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("F6 81 ?? ?? ?? ?? ?? 88 91 ?? ?? ?? ??");
            var updatePositionPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 33 D2 48 8B 01 FF 90 ?? ?? ?? ??");
            // var baseUiPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 0F BF D5");

            this._setPosition = Marshal.GetDelegateForFunctionPointer<SetPositionDelegate>(setPositionPtr);
            this._setAlpha = Marshal.GetDelegateForFunctionPointer<SetAlphaDelegate>(setAlphaPtr);
            this._updateAddonPosition = Marshal.GetDelegateForFunctionPointer<UpdateAddonPositionDelegate>(updatePositionPtr);
        }

        public void SetAddonPosition(string uiName, short x, short y) {
            var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName(uiName, 1);
            if (addon == null) {
                return;
            }

            var baseUi = this.Plugin.Interface.Framework.Gui.GetBaseUIObject();
            var manager = Marshal.ReadIntPtr(baseUi + 0x20);

            this._updateAddonPosition(
                manager,
                addon.Address,
                1
            );
            this._setPosition(addon.Address, x, y);
            this._updateAddonPosition(
                manager,
                addon.Address,
                0
            );
        }

        public Vector2<short>? GetAddonPosition(string uiName) {
            var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName(uiName, 1);
            if (addon == null) {
                return null;
            }

            try {
                var x = addon.X;
                var y = addon.Y;

                return new Vector2<short>(x, y);
            } catch (KeyNotFoundException) {
                return null;
            }
        }

        public void SetAddonAlpha(string name, byte alpha) {
            var addon = this.Plugin.Interface.Framework.Gui.GetAddonByName(name, 1);
            if (addon == null) {
                return;
            }

            this._setAlpha(addon.Address, alpha);
        }
    }
}
