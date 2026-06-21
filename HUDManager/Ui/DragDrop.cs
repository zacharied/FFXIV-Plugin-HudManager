using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace HUDManager.Ui;

public sealed class DragDropState<T>(string payloadId) {
    private readonly string payloadId = $"HUDMAN_{payloadId}";

    public T? SourceId { get; set; }
    public T? HoverId { get; set; }

    private bool sawSourceThisFrame;
    private bool sawTargetThisFrame;

    public DragDisposable Drag(T sourceId) {
        var inner = ImRaii.DragDropSource();
        if (inner) {
            sawSourceThisFrame = true;
            SourceId = sourceId;
            ImGui.SetDragDropPayload(payloadId, ReadOnlySpan<byte>.Empty);
        }

        return new DragDisposable(inner);
    }

    public DropBuilderDisposable<T> Drop(T hoverId) {
        return Drop(ImRaii.DragDropTarget(), hoverId);
    }

    public DropBuilderDisposable<T> Drop(ImRaii.DragDropTargetDisposable disposable, T hoverId) {
        if (disposable.Success && !HasPayload()) {
            return new DropBuilderDisposable<T>(disposable, this, hoverId) {
                AutoReject = true
            };
        }

        return new DropBuilderDisposable<T>(disposable, this, hoverId);
    }

    public bool CheckDrop(T hoverId, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None) {
        sawTargetThisFrame = true;
        HoverId = hoverId;

        var payload = ImGui.AcceptDragDropPayload(payloadId, flags);
        return !payload.IsNull;
    }

    public bool IsSource(T id) => EqualityComparer<T>.Default.Equals(SourceId, id);

    public bool IsHovered(T id) => EqualityComparer<T>.Default.Equals(HoverId, id);

    public DragState GetDragState(T id) {
        return IsSource(id) ? DragState.Source : IsHovered(id) ? DragState.Target : DragState.None;
    }

    public bool HasPayload() {
        var payload = ImGui.GetDragDropPayload();
        if (payload.IsNull)
            return false;

        return payload.IsDataType(payloadId);
    }

    public bool CanDrop() {
        if (!sawTargetThisFrame) {
            HoverId = default;
            return false;
        }

        return true;
    }

    public bool IsActive() {
        var hasPayload = HasPayload();
        var sawSource = sawSourceThisFrame;

        sawSourceThisFrame = false;
        sawTargetThisFrame = false;

        if (!hasPayload) {
            SourceId = default;
            return false;
        }

        if (!sawSource) {
            using (new ImRaii.ColorDisposable()
                       .Push(ImGuiCol.PopupBg, Vector4.Zero)
                       .Push(ImGuiCol.Text, Vector4.Zero))
            using (ImRaii.Tooltip()) {
                ImGui.Text("(hide)");
            }

            return false;
        }

        return true;
    }
}

public ref struct DragDisposable(ImRaii.DragDropSourceDisposable inner) : IDisposable {
    private ImRaii.DragDropSourceDisposable inner = inner;

    public bool Success => inner.Success;

    public void Dispose() {
        inner.Dispose();
    }

    public static implicit operator bool(DragDisposable value) => value.inner.Success;
    public static bool operator true(DragDisposable i) => i.inner.Success;
    public static bool operator false(DragDisposable i) => !i.inner.Success;
    public static bool operator !(DragDisposable i) => !i.inner.Success;
    public static bool operator &(DragDisposable i, bool value) => i.inner.Success && value;
    public static bool operator |(DragDisposable i, bool value) => i.inner.Success || value;
}

public ref struct DropBuilderDisposable<T>(ImRaii.DragDropTargetDisposable inner, DragDropState<T> state, T hoverId) : IDisposable {
    private ImRaii.DragDropTargetDisposable inner = inner;
    private bool alive = true;

    public bool AutoReject { get; init; } = false;

    public bool Success => !AutoReject && inner.Success;

    public DropDisposable Reject() {
        alive = false;
        return new DropDisposable(inner) {
            Hovered = false,
            Dropped = false,
        };
    }

    public DropDisposable Accept(ImGuiDragDropFlags flags = ImGuiDragDropFlags.None) {
        if (AutoReject)
            return Reject();

        var dropped = state.CheckDrop(hoverId, flags);

        alive = false;
        return new DropDisposable(inner) {
            Hovered = !dropped,
            Dropped = dropped,
        };
    }

    public void Dispose() {
        if (alive)
            inner.Dispose();
    }

    public static implicit operator bool(DropBuilderDisposable<T> value) => value.inner.Success;
    public static bool operator true(DropBuilderDisposable<T> i) => i.inner.Success;
    public static bool operator false(DropBuilderDisposable<T> i) => !i.inner.Success;
    public static bool operator !(DropBuilderDisposable<T> i) => !i.inner.Success;
    public static bool operator &(DropBuilderDisposable<T> i, bool value) => i.inner.Success && value;
    public static bool operator |(DropBuilderDisposable<T> i, bool value) => i.inner.Success || value;
}

public ref struct DropDisposable(ImRaii.DragDropTargetDisposable inner) : IDisposable {
    private ImRaii.DragDropTargetDisposable inner = inner;

    public bool Any => Hovered || Dropped;
    public bool Hovered { get; init; }
    public bool Dropped { get; init; }
    public void Dispose() => inner.Dispose();

    public static implicit operator bool(DropDisposable value) => value.Dropped;
    public static bool operator true(DropDisposable i) => i.Dropped;
    public static bool operator false(DropDisposable i) => !i.Dropped;
    public static bool operator !(DropDisposable i) => !i.Dropped;
    public static bool operator &(DropDisposable i, bool value) => i.Dropped && value;
    public static bool operator |(DropDisposable i, bool value) => i.Dropped || value;
}

public enum DragState {
    None,
    Source,
    Target,
}
