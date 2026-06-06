namespace HUDManager.Structs.Options;

public class GaugeOptions {
    private readonly EnumBitField<GaugeStyle> style;

    public GaugeStyle Style {
        get => style.Value;
        set => style.Value = value;
    }

    public GaugeOptions(byte[] options) {
        style = new EnumBitField<GaugeStyle>(options, 0);
    }
}

public enum GaugeStyle : byte {
    Normal = 0,
    Simple = 1,
}
