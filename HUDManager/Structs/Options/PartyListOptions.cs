namespace HUD_Manager.Structs.Options;

public class PartyListOptions
{
    private readonly byte[] _options;

    public PartyListAlignment Alignment
    {
        get => (PartyListAlignment)this._options[0];
        set => this._options[0] = (byte)value;
    }

    public PartyListOptions(byte[] options)
    {
        this._options = options;
    }
}

public enum PartyListAlignment : byte
{
    Top = 0,
    Bottom = 1,
}
