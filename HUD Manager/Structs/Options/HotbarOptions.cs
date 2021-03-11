namespace HUD_Manager.Structs.Options {
    public class HotbarOptions {
        private readonly byte[] _options;

        public byte Index {
            get => this._options[0];
            set => this._options[0] = value;
        }

        public HotbarLayout Layout {
            get => (HotbarLayout) this._options[1];
            set => this._options[1] = (byte) value;
        }

        public HotbarOptions(byte[] options) {
            this._options = options;
        }
    }

    public enum HotbarLayout : byte {
        TwelveByOne = 1,
        SixByTwo = 2,
        FourByThree = 3,
        ThreeByFour = 4,
        TwoBySix = 5,
        OneByTwelve = 6,
    }
}
