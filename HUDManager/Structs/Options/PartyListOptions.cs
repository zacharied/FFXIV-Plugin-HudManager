namespace HUDManager.Structs.Options;

public class PartyListOptions {
    private readonly EnumBitField<PartyListAlignment> alignment;

    public PartyListAlignment Alignment {
        get => alignment.Value;
        set => alignment.Value = value;
    }

    public PartyListOptions(byte[] options) {
        alignment = new EnumBitField<PartyListAlignment>(options, 0);
    }
}

public enum PartyListAlignment : byte {
    Top = 0,
    Bottom = 1,
}
