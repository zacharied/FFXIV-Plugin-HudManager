using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HUDManager.Structs.Options;

public class HotbarOptions {
    private readonly Element element;
    private readonly BitField<byte> index;
    private readonly EnumBitField<HotbarLayout> layout;

    public byte Index {
        get => index.Value;
        set => index.Value = value;
    }

    public HotbarLayout Layout {
        get => layout.Value;
        set {
            layout.Value = value;

            var size = CalculateSize(value);
            element.Width = size.X;
            element.Height = size.Y;
        }
    }

    public HotbarOptions(Element element) {
        this.element = element;
        index = new BitField<byte>(element.Options!, 0);
        layout = new EnumBitField<HotbarLayout>(element.Options!, 1);
    }

    private static Vector2<ushort> CalculateSize(HotbarLayout layout) {
        return layout switch {
            HotbarLayout.Layout12x1 => new Vector2<ushort>(624, 72),
            HotbarLayout.Layout6x2 => new Vector2<ushort>(331, 121),
            HotbarLayout.Layout4x3 => new Vector2<ushort>(241, 170),
            HotbarLayout.Layout3x4 => new Vector2<ushort>(162, 260),
            HotbarLayout.Layout2x6 => new Vector2<ushort>(117, 358),
            HotbarLayout.Layout1x12 => new Vector2<ushort>(72, 618),
            _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null),
        };
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HotbarLayout : byte {
    [Display(Name = "12x1")]
    Layout12x1 = 1,
    [Display(Name = "6x2")]
    Layout6x2 = 2,
    [Display(Name = "4x3")]
    Layout4x3 = 3,
    [Display(Name = "3x4")]
    Layout3x4 = 4,
    [Display(Name = "2x6")]
    Layout2x6 = 5,
    [Display(Name = "1x12")]
    Layout1x12 = 6,
}
