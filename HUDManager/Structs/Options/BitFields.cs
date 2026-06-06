using InteropGenerator.Runtime;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace HUDManager.Structs.Options;

public readonly struct BitField<TStorage>(byte[] buffer, int index, int shift = 0, int width = 8)
    where TStorage : unmanaged, IBinaryInteger<TStorage> {
    // ReSharper disable once ReplaceWithFieldKeyword // false positive due to value and mask being the same type
    private readonly TStorage mask = BitOps.CreateLowBitMask<TStorage>(width);

    public TStorage Value {
        get => BitOps.GetBits(TStorage.CreateTruncating(buffer[index]), shift, mask);
        set => buffer[index] = byte.CreateTruncating(BitOps.SetBits(TStorage.CreateTruncating(buffer[index]), shift, mask, value));
    }
}

public readonly struct EnumBitField<TEnum>(byte[] buffer, int index, int shift = 0, int width = 8)
    where TEnum : struct, Enum {
    private readonly BitField<byte> bitField = new(buffer, index, shift, width);

    public TEnum Value {
        get => Unsafe.BitCast<byte, TEnum>(bitField.Value);
        set => bitField.Value = Unsafe.BitCast<TEnum, byte>(value);
    }
}

public readonly struct BooleanBitField(byte[] buffer, int index, int shift = 0, int width = 8) {
    private readonly BitField<byte> bitField = new(buffer, index, shift, width);

    public bool Value {
        get => bitField.Value != 0;
        set => bitField.Value = value ? (byte)1 : (byte)0;
    }
}