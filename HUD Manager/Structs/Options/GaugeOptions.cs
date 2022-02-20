namespace HUD_Manager.Structs.Options
{
    public class GaugeOptions
    {
        private readonly byte[] _options;

        public GaugeStyle Style
        {
            get => (GaugeStyle)this._options[0];
            set => this._options[0] = (byte)value;
        }

        public GaugeOptions(byte[] options)
        {
            this._options = options;
        }
    }

    public enum GaugeStyle : byte
    {
        Normal = 0,
        Simple = 1,
    }
}
