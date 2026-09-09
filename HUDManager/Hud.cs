using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HUDManager.Configuration;
using HUDManager.Structs;
using HUDManager.Structs.External;
using HUDManager.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HUDManager;

public sealed class Hud : IDisposable {
    public const int InMemoryLayoutElements = 112;              // Updated 7.5
    private const int LayoutSize = InMemoryLayoutElements * 36; // Updated 7.5 (same since 5.45). Each element is 32 bytes in ADDON.DAT, but 36 bytes in memory.
    private const int DataSlotOffset = 0xDC48;                  // Updated 7.56
    private const int DataBaseLayoutOffset = 0x9D20;            // Updated 7.5

    private Plugin Plugin { get; }

    public Hud(Plugin plugin) {
        Plugin = plugin;
    }

    public static unsafe nint GetDataPointer() {
        return (nint)AddonConfig.Instance()->ActiveDataSet;
    }

    internal static unsafe nint GetLayoutPointer(HudSlot slot) {
        // return (nint)AddonConfig.Instance()->ActiveDataSet + DataBaseLayoutOffset + (int)slot * LayoutSize;
        return (nint)AddonConfig.Instance()->ActiveDataSet->HudLayoutConfigEntries.GetPointer((int)slot * InMemoryLayoutElements);
    }

    public static HudSlot GetActiveHudSlot() {
        return GetActiveHudSlotManual(); // FIXME: use CS after Dalamud updates
    }

    public static HudSlot GetActiveHudSlotManual() {
        return (HudSlot)Marshal.ReadInt32(GetDataPointer() + DataSlotOffset);
    }

    public static unsafe HudSlot GetActiveHudSlotCs() {
        // var slotVal = Marshal.ReadInt32(GetDataPointer() + DataSlotOffset);
        // if (!Enum.IsDefined(typeof(HudSlot), slotVal))
        //     throw new System.IO.IOException($"invalid hud slot in FFXIV memory of ${slotVal}");
        // return (HudSlot)slotVal;

        var addonConfig = AddonConfig.Instance();
        if (addonConfig is null)
            return HudSlot.One;

        return (HudSlot)addonConfig->ActiveDataSet->CurrentHudLayout;
    }

    public static Layout ReadLayout(HudSlot slot) {
        var slotPtr = GetLayoutPointer(slot);
        return Marshal.PtrToStructure<Layout>(slotPtr);
    }

    public static unsafe void ApplyHudLayout() {
        var addonConfig = AddonConfig.Instance();
        if (addonConfig is null)
            return;

        addonConfig->ApplyHudLayout();
    }

    public static unsafe void ChangeSlot(HudSlot slot) {
        var addonConfig = AddonConfig.Instance();
        if (addonConfig is null)
            return;

        if (addonConfig->ActiveDataSet->CurrentHudLayout != (uint)slot)
            addonConfig->ChangeHudLayout((uint)slot);
    }

    public static unsafe bool IsEditingHudLayout() {
        var rapture = RaptureAtkUnitManager.Instance();
        if (rapture is null)
            return false;

        return rapture->IsEditingHudLayout;
    }

    private static void WriteLayout(HudSlot slot, IReadOnlyDictionary<ElementKind, Element> dict) {
#if READONLY
        return;
#endif
        var slotPtr = GetLayoutPointer(slot);

        // update existing elements with saved data instead of wholesale overwriting
        var rawLayout = ReadLayout(slot);
        for (var i = 0; i < rawLayout.elements.Length; i++) {
            ref var rawElement = ref rawLayout.elements[i];
            if (!rawElement.id.IsRealElement())
                continue;

            if (!dict.TryGetValue(rawElement.id, out var element))
                continue;

            if (element.Id is ElementKind.Minimap) {
                // Minimap: Don't load zoom/rotation from HUD settings but use current UI state instead
                element = element.Clone();
                element.Options = rawElement.options;
            } else if (element.Id is ElementKind.Hotbar1
                       && (element.LayoutFlags & ElementLayoutFlags.ClobberTransientOptions) == 0) { // Clobber flag is unset (default)
                // Hotbar1: Keep cycling state
                element = element.Clone();
                element.Options![0] = rawElement.options![0];
            }

            if (element.Enabled == Element.AllEnabled) {
                // just replace the struct if all options are enabled
                rawElement = new RawElement(element);
            } else {
                // otherwise only replace the enabled options
                rawElement.UpdateEnabled(element);
            }
        }

        Marshal.StructureToPtr(rawLayout, slotPtr, false);
    }

    public SavedLayout? GetEffectiveLayout(Guid id, List<Guid>? layers = null) {
        // find the node for this id
        var nodes = Node<SavedLayout>.BuildTree(Plugin.Config.Layouts);
        if (nodes.Find(id) is not { } node) {
            return null;
        }

        var elements = new Dictionary<ElementKind, Element>();
        var windows = new Dictionary<string, Window>();
        var bwOverlays = new List<BrowsingwayOverlay>();
        CrossUpConfig? crossUpConfig;

        // Apply each element of a layout on top of the virtual layout we are constructing.
        void ApplyLayout(Node<SavedLayout> node) {
            foreach (var element in node.Value.Elements) {
                if (element.Value.Enabled == Element.AllEnabled || !elements.ContainsKey(element.Key)) {
                    elements[element.Key] = element.Value.Clone();
                    continue;
                }

                elements[element.Key].UpdateEnabled(element.Value);
            }

            foreach (var window in node.Value.Windows) {
                if (window.Value.Enabled == Window.AllEnabled || !windows.ContainsKey(window.Key)) {
                    windows[window.Key] = window.Value.Clone();
                    continue;
                }

                windows[window.Key].UpdateEnabled(window.Value);
            }

            foreach (var overlay in node.Value.BrowsingwayOverlays) {
                if (!bwOverlays.Exists(o => o.CommandName == overlay.CommandName)) {
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

        // Apply ancestors
        foreach (var ancestor in node.Ancestors().Reverse()) {
            ApplyLayout(ancestor);
        }

        ApplyLayout(node);

        // Apply layers
        if (Plugin.Config.AdvancedSwapMode && layers is { Count: > 0 }) {
            foreach (var layerId in layers.Reverse<Guid>()) {
                if (nodes.Find(layerId) is { } layer) {
                    ApplyLayout(layer);
                } else {
                    Plugin.Log.Error($"Unable to find layer {layerId}");
                }
            }
        }

        return new SavedLayout($"Effective {id}", elements, windows, bwOverlays, crossUpConfig, Guid.Empty);
    }

    public void WriteAll(HudSlot slot, Guid id, List<Guid> layers) {
        if (GetEffectiveLayout(id, layers) is { } layout)
            WriteAll(slot, layout);
    }

    public void WriteAll(HudSlot slot, SavedLayout layout) {
        if (!Plugin.HudLock.CanWrite()) {
            var notification = new Notification {
                Type = NotificationType.Error,
                Title = "Failed to write HUD layout",
                Content = $"Failed to write HUD layout to slot {(int)HudSlot.One + 1} ({Plugin.HudLock.WriteBlockReason})"
            };
            Plugin.NotificationManager.AddNotification(notification);
        }

        WriteLayout(Plugin.Config.StagingSlot, layout.Elements);

        if (slot == GetActiveHudSlot()) {
            ApplyHudLayout();

            // ApplyAllJobGaugeVisibility(layout);

            foreach (var window in layout.Windows) {
                Plugin.GameFunctions.SetAddonPosition(window.Key, window.Value.Position.X, window.Value.Position.Y);
            }

            foreach (var overlay in layout.BrowsingwayOverlays) {
                overlay.ApplyOverlay(Plugin);
            }

            layout.CrossUpConfig?.ApplyConfig(Plugin);
        }
    }

    internal void ImportSlot(string name, HudSlot slot, bool save = true) {
        Import(name, ReadLayout(slot), save);
    }

    private void Import(string name, Layout layout, bool save = true) {
        var guid = Plugin.Config.Layouts.FirstOrDefault(kv => kv.Value.Name == name).Key;
        guid = guid != default ? guid : Guid.NewGuid();

        Plugin.Config.Layouts[guid] = new SavedLayout(name, layout);
        if (save) {
            Plugin.Config.Save();
        }
    }

    public void ApplyAllJobGaugeVisibility(SavedLayout effectiveLayout) {
        if (Plugin.PlayerState is not { IsLoaded: true } playerState)
            return;

        var jobIndex = playerState.ClassJob.RowId;
        foreach (var (kind, element) in effectiveLayout.Elements) {
            if (kind.ClassJob() is { } classJob && classJob.RowId == jobIndex && element[ElementComponent.Visibility]) {
                ApplyJobGaugeVisibility(kind, element);
            }
        }
    }

    private unsafe void ApplyJobGaugeVisibility(ElementKind kind, Element element) {
        var unitName = kind.GetJobGaugeAtkName();
        if (unitName is null)
            return;

        var ptr = Plugin.GameGui.GetAddonByName(unitName);
        if (ptr.IsNull)
            return;

        var unit = (AtkUnitBase*)ptr.Address;

        var visibilityMask = Util.GamepadModeActive(Plugin) ? VisibilityFlags.Gamepad : VisibilityFlags.Keyboard;
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

    public void Dispose() {
    }
}

public enum HudSlot {
    One = 0,
    Two = 1,
    Three = 2,
    Four = 3,
}

public enum StructSource {
    Override,
    ClientStructs
}

public class Vector2<T>(T x, T y) {
    public T X { get; set; } = x;
    public T Y { get; set; } = y;
}
