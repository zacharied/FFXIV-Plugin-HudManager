using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HUD_Manager.Structs;
using ImGuiNET;

namespace HUD_Manager.Ui {
    #if DEBUG
    public class Debug {
        private Plugin Plugin { get; }

        private Layout? PreviousLayout { get; set; }

        public Debug(Plugin plugin) {
            this.Plugin = plugin;
        }

        internal void Draw() {
            if (!ImGui.BeginTabItem("Debug")) {
                return;
            }

            ImGui.TextUnformatted("Print layout pointer address");

            if (ImGui.Button("1")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("2")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Two);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("3")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Three);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("4")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.Four);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            ImGui.SameLine();

            if (ImGui.Button("Default")) {
                var ptr = this.Plugin.Hud.GetDefaultLayoutPointer();
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            if (ImGui.Button("File pointer 0")) {
                var ptr = this.Plugin.Hud.GetFilePointer(0);
                this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            }

            if (ImGui.Button("Save layout")) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                var layout = Marshal.PtrToStructure<Layout>(ptr);
                this.PreviousLayout = layout;
            }

            ImGui.SameLine();

            if (ImGui.Button("Find difference") && this.PreviousLayout != null) {
                var ptr = this.Plugin.Hud.GetLayoutPointer(HudSlot.One);
                var layout = Marshal.PtrToStructure<Layout>(ptr);

                foreach (var prevElem in this.PreviousLayout.Value.elements) {
                    var currElem = layout.elements.FirstOrDefault(el => el.id == prevElem.id);
                    if (currElem.visibility == prevElem.visibility && !(Math.Abs(currElem.x - prevElem.x) > .01)) {
                        continue;
                    }

                    PluginLog.Log(currElem.id.ToString());
                    this.Plugin.ChatGui.Print(currElem.id.ToString());
                }
            }

            if (ImGui.Button("Print current slot")) {
                var slot = this.Plugin.Hud.GetActiveHudSlot();
                this.Plugin.ChatGui.Print($"{slot}");
            }

            if (ImGui.Button("Print player status address"))
            {
                this.Plugin.ChatGui.Print($"{this.Plugin.ClientState.LocalPlayer:x}");
            }

            if (ImGui.Button("Print Config"))
            {
                unsafe
                {
                    this.Plugin.ChatGui.Print($"{(IntPtr)Framework.Instance()->SystemConfig.CommonSystemConfig.ConfigBase.ConfigEntry:x}");
                }
            }
            ImGui.Separator();

            // var layoutPtr = this.Plugin.Hud.GetDefaultLayoutPointer() + 8;
            //
            // for (var i = 0; i < 291; i++) {
            //     var rawElement = Marshal.PtrToStructure<RawElement>(layoutPtr + i * Marshal.SizeOf<RawElement>());
            //     var element = new Element(rawElement);
            //
            //     if ((WindowKind) rawElement.id != WindowKind.FreeCompany) {
            //         continue;
            //     }
            //
            //     ImGui.TextUnformatted($"{(WindowKind) rawElement.id}");
            //     ImGui.TextUnformatted($"Measured from: {rawElement.measuredFrom.Name()}");
            //     ImGui.TextUnformatted($"Width: {rawElement.width}");
            //     ImGui.TextUnformatted($"Height: {rawElement.height}");
            //
            //     var screen = ImGui.GetIO().DisplaySize;
            //     var (pos, _) = CalcPosAndSize(element);
            //
            //     var x = pos.X;
            //     if (ImGui.DragFloat($"X##addon-{rawElement.id}", ref x, this._dragSpeed)) {
            //         this.Plugin.GameFunctions.SetAddonPosition("FreeCompany", (short) x, (short) pos.Y);
            //     }
            //
            //     var y = pos.Y;
            //     if (ImGui.DragFloat($"Y##addon-{rawElement.id}", ref y, this._dragSpeed)) {
            //         this.Plugin.GameFunctions.SetAddonPosition("FreeCompany", (short) pos.X, (short) y);
            //     }
            //
            //     ImGui.TextUnformatted($"X: {rawElement.x}/{(short) Math.Round(rawElement.x * screen.X / 100)}");
            //     ImGui.TextUnformatted($"Y: {rawElement.y}/{(short) Math.Round(rawElement.y * screen.Y / 100)}");
            //
            //     var opacity = (int) rawElement.opacity;
            //     if (ImGui.InputInt($"Opacity##addon-{rawElement.id}", ref opacity)) {
            //         rawElement.opacity = (byte) Math.Max(0, Math.Min(255, opacity));
            //         Marshal.StructureToPtr(rawElement, layoutPtr + i * Marshal.SizeOf<RawElement>(), false);
            //         this.Plugin.GameFunctions.SetAddonAlpha("FreeCompany", rawElement.opacity);
            //     }
            //
            //     if (ImGui.Button("Print addon address")) {
            //         var ptr = this.Plugin.GameGui.GetAddonByName("FreeCompany", 1).Address;
            //         this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            //     }
            //
            //     if (ImGui.Button("Print base UI object address")) {
            //         var ptr = this.Plugin.GameGui.GetBaseUIObject();
            //         this.Plugin.ChatGui.Print($"{ptr.ToInt64():x}");
            //     }
            //
            //     ImGui.Separator();
            // }

            ImGui.EndTabItem();
        }
    }
    #endif
}
