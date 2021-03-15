using System;

namespace HUD_Manager.Structs.Options {
    public class HotbarOptions {
        private readonly Element _element;
        private readonly byte[] _options;

        public byte Index {
            get => this._options[0];
            set => this._options[0] = value;
        }

        public HotbarLayout Layout {
            get => (HotbarLayout) this._options[1];
            set {
                this._options[1] = (byte) value;
                var size = value.Size();
                this._element.Width = size.X;
                this._element.Height = size.Y;
            }
        }

        public HotbarOptions(Element element) {
            this._element = element;
            this._options = element.Options;
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

    public static class HotbarLayoutExt {
        public static string Name(this HotbarLayout layout) {
            return layout switch {
                HotbarLayout.TwelveByOne => "12x1",
                HotbarLayout.SixByTwo => "6x2",
                HotbarLayout.FourByThree => "4x3",
                HotbarLayout.ThreeByFour => "3x4",
                HotbarLayout.TwoBySix => "2x6",
                HotbarLayout.OneByTwelve => "1x12",
                _ => layout.ToString(),
            };
        }

        public static Vector2<ushort> Size(this HotbarLayout layout) {
            return layout switch {
                HotbarLayout.TwelveByOne => new Vector2<ushort>(624, 72),
                HotbarLayout.SixByTwo => new Vector2<ushort>(331, 121),
                HotbarLayout.FourByThree => new Vector2<ushort>(241, 170),
                HotbarLayout.ThreeByFour => new Vector2<ushort>(162, 260),
                HotbarLayout.TwoBySix => new Vector2<ushort>(117, 358),
                HotbarLayout.OneByTwelve => new Vector2<ushort>(72, 618),
                _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null),
            };
        }
    }
}
