﻿using FFXIVClientStructs.FFXIV.Component.GUI;
using HUD_Manager.Configuration;
using HUD_Manager.Structs;
using HUD_Manager.Tree;
using HUDManager.Structs.External;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace HUD_Manager
{
    public class Hud : IDisposable
    {
        public const int InMemoryLayoutElements = 103; // Updated 6.51
        // Each element is 32 bytes in ADDON.DAT, but they're 36 bytes when loaded into memory.
        private const int LayoutSize = InMemoryLayoutElements * 36; // Updated 5.45

        private const int FileDataPointerOffset = 0x50;
        private const int FileSaveMarkerOffset = 0x3E; // Unused

        private const int DataSlotOffset = 0x9E88; // Updated 6.5
        private const int DataBaseLayoutOffset = 0x6498; // Updated 6.51
        private const int DataDefaultLayoutOffset = 0x35F8; // Updated 6.51 (note: not used except in debug window, not sure of exact structure)

        private delegate IntPtr GetFilePointerDelegate(byte index);
        private delegate uint SetHudLayoutDelegate(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1);
        private readonly GetFilePointerDelegate? _getFilePointer;
        private readonly SetHudLayoutDelegate? _setHudLayout;

        private StagingState? _stagingState;

        private Plugin Plugin { get; }

        private record StagingState(uint JobId, Guid LayoutId, List<Guid> LayerIds)
        {
            public bool SameLayers(Guid layoutId, List<Guid> layerIds) => this.LayoutId == layoutId && this.LayerIds.SequenceEqual(layerIds);
            public bool SameJob(uint playerJobId) => this.JobId == playerJobId;
        }

        public Hud(Plugin plugin)
        {
            this.Plugin = plugin;

            var getFilePointerPtr = this.Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 14 83 7B 44 00");
            var setHudLayoutPtr = this.Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 EB 15");
            if (getFilePointerPtr != IntPtr.Zero) {
                this._getFilePointer = Marshal.GetDelegateForFunctionPointer<GetFilePointerDelegate>(getFilePointerPtr);
            }

            if (setHudLayoutPtr != IntPtr.Zero) {
                this._setHudLayout = Marshal.GetDelegateForFunctionPointer<SetHudLayoutDelegate>(setHudLayoutPtr);
            }
        }

        public IntPtr GetFilePointer(byte index)
        {
            return this._getFilePointer?.Invoke(index) ?? IntPtr.Zero;
        }

        public void SaveAddonData()
        {
            var saveMarker = this.GetFilePointer(0) + FileSaveMarkerOffset;
            Marshal.WriteByte(saveMarker, 1);
        }

        public void SelectSlot(HudSlot slot, bool force = false)
        {
            if (this._setHudLayout == null) {
                return;
            }

            var file = this.GetFilePointer(0);
            // change the current slot so the game lets us pick one that's currently in use
            if (!force) {
                goto Return;
            }

            unsafe {
                var currentSlotPtr = (uint*)(this.GetDataPointer() + DataSlotOffset);
                // read the current slot
                var currentSlot = *currentSlotPtr;
                // if the current slot is the slot we want to change to, we can force a reload by
                // telling the game it's on a different slot and swapping back to the desired slot
                if (currentSlot == (uint)slot) {
                    var backupSlot = currentSlot;
                    if (backupSlot < 3) {
                        backupSlot += 1;
                    } else {
                        backupSlot = 0;
                    }

                    // back up this different slot
                    var backup = this.ReadLayout((HudSlot)backupSlot);
                    // change the current slot in memory
                    *currentSlotPtr = backupSlot;

                    // ask the game to change slot to our desired slot
                    // for some reason, this overwrites the current slot, so this is why we back up
                    this._setHudLayout.Invoke(file, (uint)slot, 0, 1);
                    // restore the backup
                    this.WriteLayout((HudSlot)backupSlot, backup, false);
                    return;
                }
            }

            Return:
            this._setHudLayout.Invoke(file, (uint)slot, 0, 1);
        }

        public IntPtr GetDataPointer()
        {
            var dataPtr = this.GetFilePointer(0) + FileDataPointerOffset;
            return Marshal.ReadIntPtr(dataPtr);
        }

        internal IntPtr GetDefaultLayoutPointer()
        {
            return this.GetDataPointer() + DataDefaultLayoutOffset;
        }

        internal IntPtr GetLayoutPointer(HudSlot slot)
        {
            var slotNum = (int)slot;
            // Plugin.Log.Debug($"layoutPointer({slot}) 0x{this.GetDataPointer():X} + offset 0x{DataBaseLayoutOffset:X} + 0x{slotNum * LayoutSize:X} = 0x{this.GetDataPointer() + DataBaseLayoutOffset + slotNum * LayoutSize:X}");
            return this.GetDataPointer() + DataBaseLayoutOffset + slotNum * LayoutSize;
        }

        public HudSlot GetActiveHudSlot()
        {
            // Plugin.Log.Debug($"dataPointer(0x{this.GetDataPointer():X} + offset 0x{DataSlotOffset:X} = 0x{this.GetDataPointer() + DataSlotOffset:X}");
            var slotVal = Marshal.ReadInt32(this.GetDataPointer() + DataSlotOffset);

            if (!Enum.IsDefined(typeof(HudSlot), slotVal)) {
                throw new IOException($"invalid hud slot in FFXIV memory of ${slotVal}");
            }

            return (HudSlot)slotVal;
        }

        public Layout ReadLayout(HudSlot slot)
        {
            var slotPtr = this.GetLayoutPointer(slot);
            return Marshal.PtrToStructure<Layout>(slotPtr);
        }

        private void WriteLayout(HudSlot slot, Layout layout, bool reloadIfNecessary = true)
        {
            this.WriteLayout(slot, layout.ToDictionary(), reloadIfNecessary);
        }

        private void WriteLayout(HudSlot slot, IReadOnlyDictionary<ElementKind, Element> dict, bool reloadIfNecessary = true)
        {
            var slotPtr = this.GetLayoutPointer(slot);

            // update existing elements with saved data instead of wholesale overwriting
            var slotLayout = this.ReadLayout(slot);
#if !READONLY
            for (var i = 0; i < slotLayout.elements.Length; i++) {
                if (!slotLayout.elements[i].id.IsRealElement())
                    continue;

                if (!dict.TryGetValue(slotLayout.elements[i].id, out var element))
                    continue;

                if (reloadIfNecessary) {
                    if (element.Id is ElementKind.Minimap) {
                        // Minimap: Don't load zoom/rotation from HUD settings but use current UI state instead
                        element = element.Clone();
                        element.Options = slotLayout.elements[i].options;
                    } else if (element.Id is ElementKind.Hotbar1
                               && (element.LayoutFlags & ElementLayoutFlags.ClobberTransientOptions) == 0) { // Clobber flag is unset (default)
                        // Hotbar1: Keep cycling state
                        element = element.Clone();
                        element.Options![0] = slotLayout.elements[i].options![0];
                    }
                }

                // just replace the struct if all options are enabled
                if (element.Enabled == Element.AllEnabled) {
                    slotLayout.elements[i] = new RawElement(element);
                    continue;
                }

                // otherwise only replace the enabled options
                slotLayout.elements[i].UpdateEnabled(element);
            }

            Marshal.StructureToPtr(slotLayout, slotPtr, false);

            // copy directly over
            // Marshal.StructureToPtr(layout, slotPtr, false);

            if (!reloadIfNecessary) {
                return;
            }

            var currentSlot = this.GetActiveHudSlot();
            if (currentSlot == slot) {
                this.SelectSlot(currentSlot, true);
            }
#endif
        }

        private SavedLayout? GetEffectiveLayout(Guid id, List<Guid>? layers = null)
        {
            // find the node for this id
            var nodes = Node<SavedLayout>.BuildTree(this.Plugin.Config.Layouts);
            var node = nodes.Find(id);
            if (node == null) {
                return null;
            }

            var elements = new Dictionary<ElementKind, Element>();
            var windows = new Dictionary<string, Window>();
            var bwOverlays = new List<BrowsingwayOverlay>();
            CrossUpConfig? crossUpConfig;

            // Apply each element of a layout on top of the virtual layout we are constructing.
            void ApplyLayout(Node<SavedLayout> node)
            {
                foreach (var element in node.Value.Elements) {
                    if (element.Value.Enabled == Element.AllEnabled || !elements.ContainsKey(element.Key)) {
                        elements![element.Key] = element.Value.Clone();
                        continue;
                    }

                    elements[element.Key].UpdateEnabled(element.Value);
                }

                foreach (var window in node.Value.Windows) {
                    if (window.Value.Enabled == Window.AllEnabled || !windows.ContainsKey(window.Key)) {
                        windows![window.Key] = window.Value.Clone();
                        continue;
                    }

                    windows[window.Key].UpdateEnabled(window.Value);
                }

                foreach (var overlay in node.Value.BrowsingwayOverlays) {
                    if (!bwOverlays!.Exists(o => o.CommandName == overlay.CommandName)) {
                        bwOverlays.Add(overlay.Clone());
                        continue;
                    }

                    var findOverlay = bwOverlays.Find(o => o.CommandName == overlay.CommandName);
                    if (findOverlay is null) {
                        Plugin.Log.Error("Unable to find overlay during ancestor search");
                        continue;
                    }
                    findOverlay.UpdateEnabled(overlay);
                }

                crossUpConfig = node.Value.CrossUpConfig?.Clone();

            }

            // get the ancestors and their elements for this node
            foreach (var ancestor in node.Ancestors().Reverse()) {
                ApplyLayout(ancestor);
            }

            ApplyLayout(node);

            // If there's layers, apply them.
            if (Plugin.Config.AdvancedSwapMode && layers != null) {
                foreach (var layerId in layers.Reverse<Guid>()) {
                    var layer = nodes.Find(layerId);
                    if (layer == null) {
                        Plugin.Log.Error("unable to find layered condition by ID");
                        break;
                    }

                    ApplyLayout(layer);
                }
            }

            return new SavedLayout($"Effective {id}", elements, windows, bwOverlays, crossUpConfig, Guid.Empty);
        }

        private string GetDebugName(Guid id, List<Guid>? layers) =>
            $"{Plugin.Config.Layouts[id].Name} [{(layers == null ? "" : string.Join(", ", layers.ConvertAll(layer => Plugin.Config.Layouts[layer].Name)))}]";

        public void WriteEffectiveLayoutIfChanged(HudSlot slot, Guid id, List<Guid> layers)
        {
            if (_stagingState != null && _stagingState.SameLayers(id, layers)) {
                if (_stagingState.SameJob(Util.GetPlayerJobId(Plugin))) {
                    Plugin.Log.Debug($"Skipped layout {GetDebugName(id, layers)} (state unchanged)");
                } else {
                    Plugin.Log.Debug($"Skipped layout {GetDebugName(id, layers)} (gauge changes only)");
                    WriteEffectiveLayoutGaugesOnly(id, layers);
                }
                return;
            }

            WriteEffectiveLayout(slot, id, layers);
        }

        private void WriteEffectiveLayoutGaugesOnly(Guid id, List<Guid>? layers = null)
        {
            var effective = this.GetEffectiveLayout(id, layers);
            if (effective == null) {
                return;
            }

            ApplyAllJobGaugeVisibility(effective);

            _stagingState = new StagingState(Util.GetPlayerJobId(Plugin), id, layers ?? new List<Guid>());
        }

        public void WriteEffectiveLayout(HudSlot slot, Guid id, List<Guid>? layers = null)
        {
            var effective = this.GetEffectiveLayout(id, layers);
            if (effective == null) {
                return;
            }

            Plugin.Log.Debug($"Writing layout {GetDebugName(id, layers)}");

            this.WriteLayout(slot, effective.Elements);

            ApplyAllJobGaugeVisibility(effective);

            foreach (var window in effective.Windows) {
                this.Plugin.GameFunctions.SetAddonPosition(window.Key, window.Value.Position.X, window.Value.Position.Y);
            }

            foreach (var overlay in effective.BrowsingwayOverlays) {
                overlay.ApplyOverlay(Plugin);
            }

            effective.CrossUpConfig?.ApplyConfig(Plugin);

            _stagingState = new StagingState(Util.GetPlayerJobId(Plugin), id, layers ?? new List<Guid>());
        }

        internal void ImportSlot(string name, HudSlot slot, bool save = true)
        {
            this.Import(name, this.Plugin.Hud.ReadLayout(slot), save);
        }

        private void Import(string name, Layout layout, bool save = true)
        {
            var guid = this.Plugin.Config.Layouts.FirstOrDefault(kv => kv.Value.Name == name).Key;
            guid = guid != default ? guid : Guid.NewGuid();

            this.Plugin.Config.Layouts[guid] = new SavedLayout(name, layout);
            if (save) {
                this.Plugin.Config.Save();
            }
        }

        private void ApplyAllJobGaugeVisibility(SavedLayout effectiveLayout)
        {
            if (Plugin.ClientState.LocalPlayer is null)
                return;

            var jobIndex = Plugin.ClientState.LocalPlayer!.ClassJob.GameData?.JobIndex ?? 0;
            foreach (var (kind, element) in effectiveLayout.Elements) {
                if (kind.IsJobGauge() && kind.ClassJob()?.JobIndex == jobIndex && element[ElementComponent.Visibility]) {
                    ApplyJobGaugeVisibility(kind, element);
                }
            }
        }

        private unsafe void ApplyJobGaugeVisibility(ElementKind kind, Element element)
        {
            var unitName = kind.GetJobGaugeAtkName()!;
            var unit = (AtkUnitBase*)this.Plugin.GameGui.GetAddonByName(unitName, 1);
            if (unit is null)
                return;

            var visibilityMask = Util.GamepadModeActive(this.Plugin) ? VisibilityFlags.Gamepad : VisibilityFlags.Keyboard;
            if ((element.Visibility & visibilityMask) > 0) {
                // Reveal element.
                if (unit->UldManager.NodeListCount == 0)
                    unit->UldManager.UpdateDrawNodeList();
                unit->IsVisible = true;
            } else {
                // Hide element.
                if (unit->UldManager.NodeListCount > 0)
                    unit->UldManager.NodeListCount = 0;
                unit->IsVisible = false;
            }
        }

        public void Dispose()
        {
        }
    }

    public enum HudSlot
    {
        One = 0,
        Two = 1,
        Three = 2,
        Four = 3,
    }

    public class Vector2<T>
    {
        public T X { get; set; }
        public T Y { get; set; }

        public Vector2(T x, T y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
