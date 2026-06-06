namespace HUDManager.Structs.Options;

public class TargetBarOptions {
    private readonly BooleanBitField showIndependently;

    public bool ShowIndependently {
        get => showIndependently.Value;
        set => showIndependently.Value = value;
    }

    public TargetBarOptions(byte[] options) {
        showIndependently = new BooleanBitField(options, 0);
    }
}
