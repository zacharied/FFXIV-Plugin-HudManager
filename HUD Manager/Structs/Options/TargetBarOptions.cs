namespace HUD_Manager.Structs.Options
{
    public class TargetBarOptions
    {
        private readonly byte[] _options;

        public bool ShowIndependently
        {
            get => this._options[0] == 1;
            set => this._options[0] = value ? (byte)1 : (byte)0;
        }

        public TargetBarOptions(byte[] options)
        {
            this._options = options;
        }
    }
}
