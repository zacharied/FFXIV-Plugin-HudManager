using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HUD_Manager.Configuration;
using HUD_Manager.Structs;
using HUD_Manager.Tree;

namespace HUD_Manager {
    public class Hud {
        // Updated 5.45
        public const int InMemoryLayoutElements = 81;
        // Updated 5.45
        // Each element is 32 bytes in ADDON.DAT, but they're 36 bytes when loaded into memory.
        private const int LayoutSize = InMemoryLayoutElements * 36;
        // Updated 5.4
        private const int SlotOffset = 0x59e8;

        private delegate IntPtr GetFilePointerDelegate(byte index);

        private delegate uint SetHudLayoutDelegate(IntPtr filePtr, uint hudLayout, byte unk0, byte unk1);

        private readonly GetFilePointerDelegate? _getFilePointer;
        private readonly SetHudLayoutDelegate? _setHudLayout;

        private Plugin Plugin { get; }

        public Hud(Plugin plugin) {
            this.Plugin = plugin;
            var getFilePointerPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 14 83 7B 44 00");
            var setHudLayoutPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 33 C0 EB 15");
            if (getFilePointerPtr != IntPtr.Zero) {
                this._getFilePointer = Marshal.GetDelegateForFunctionPointer<GetFilePointerDelegate>(getFilePointerPtr);
            }

            if (setHudLayoutPtr != IntPtr.Zero) {
                this._setHudLayout = Marshal.GetDelegateForFunctionPointer<SetHudLayoutDelegate>(setHudLayoutPtr);
            }
        }

        private IntPtr GetFilePointer(byte index) {
            return this._getFilePointer?.Invoke(index) ?? IntPtr.Zero;
        }

        public void SelectSlot(HudSlot slot, bool force = false) {
            if (this._setHudLayout == null) {
                return;
            }

            var file = this.GetFilePointer(0);
            // change the current slot so the game lets us pick one that's currently in use
            if (!force) {
                goto Return;
            }

            var currentSlotPtr = this.GetDataPointer() + SlotOffset;
            // read the current slot
            var currentSlot = (uint) Marshal.ReadInt32(currentSlotPtr);
            // change it to a different slot
            if (currentSlot == (uint) slot) {
                if (currentSlot < 3) {
                    currentSlot += 1;
                } else {
                    currentSlot = 0;
                }

                // back up this different slot
                var backup = this.ReadLayout((HudSlot) currentSlot);
                // change the current slot in memory
                Marshal.WriteInt32(currentSlotPtr, (int) currentSlot);
                // ask the game to change slot to our desired slot
                // for some reason, this overwrites the current slot, so this is why we back up
                this._setHudLayout.Invoke(file, (uint) slot, 0, 1);
                // restore the backup
                this.WriteLayout((HudSlot) currentSlot, backup);
                return;
            }

            Return:
            this._setHudLayout.Invoke(file, (uint) slot, 0, 1);
        }

        private IntPtr GetDataPointer() {
            var dataPtr = this.GetFilePointer(0) + 0x50;
            return Marshal.ReadIntPtr(dataPtr);
        }

        internal IntPtr GetLayoutPointer(HudSlot slot) {
            var slotNum = (int) slot;
            return this.GetDataPointer() + 0x2c58 + slotNum * LayoutSize;
        }

        public HudSlot GetActiveHudSlot() {
            var slotVal = Marshal.ReadInt32(this.GetDataPointer() + SlotOffset);

            if (!Enum.IsDefined(typeof(HudSlot), slotVal)) {
                throw new IOException($"invalid hud slot in FFXIV memory of ${slotVal}");
            }

            return (HudSlot) slotVal;
        }

        public Layout ReadLayout(HudSlot slot) {
            var slotPtr = this.GetLayoutPointer(slot);
            return Marshal.PtrToStructure<Layout>(slotPtr);
        }

        public void WriteLayout(HudSlot slot, Layout layout) {
            var slotPtr = this.GetLayoutPointer(slot);

            var dict = layout.ToDictionary();

            // update existing elements with saved data instead of wholesale overwriting
            var slotLayout = this.ReadLayout(slot);
            for (var i = 0; i < slotLayout.elements.Length; i++) {
                if (dict.TryGetValue(slotLayout.elements[i].id, out var element)) {
                    slotLayout.elements[i] = new RawElement(element);
                }
            }

            Marshal.StructureToPtr(slotLayout, slotPtr, false);

            // copy directly over
            // Marshal.StructureToPtr(layout, slotPtr, false);

            var currentSlot = this.GetActiveHudSlot();
            if (currentSlot == slot) {
                this.SelectSlot(currentSlot, true);
            }
        }

        public void WriteEffectiveLayout(HudSlot slot, Guid id) {
            // find the node for this id
            var nodes = Node<SavedLayout>.BuildTree(this.Plugin.Config.Layouts);
            var node = nodes.Find(id);
            if (node == null) {
                return;
            }

            var elements = new Dictionary<ElementKind, Element>();

            // get the ancestors and their elements for this node
            foreach (var ancestor in node.Ancestors().Reverse()) {
                foreach (var element in ancestor.Value.Elements) {
                    elements[element.Key] = element.Value;
                }
            }

            // apply this node's elements
            foreach (var element in node.Value.Elements) {
                elements[element.Key] = element.Value;
            }

            var elemList = elements.Values.ToList();

            while (elemList.Count < InMemoryLayoutElements) {
                elemList.Add(new Element(new RawElement()));
            }

            var effective = new Layout {
                elements = elemList.Select(elem => new RawElement(elem)).ToArray(),
            };

            this.WriteLayout(slot, effective);
        }
    }

    public enum HudSlot {
        One = 0,
        Two = 1,
        Three = 2,
        Four = 3,
    }

    public class Vector2<T> {
        public T X { get; }
        public T Y { get; }

        public Vector2(T x, T y) {
            this.X = x;
            this.Y = y;
        }
    }
}
