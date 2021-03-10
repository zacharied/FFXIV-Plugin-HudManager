using System.Runtime.InteropServices;

namespace HUD_Manager.Structs {
    [StructLayout(LayoutKind.Sequential)]
    public struct RawElement {
        public ElementKind id;

        public float x;

        public float y;

        public float scale;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] unknown4;

        public Visibility visibility;

        public byte unknown6;

        public byte opacity;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] unknown8;

        public RawElement(Element element) {
            this.id = element.Id;
            this.x = element.X;
            this.y = element.Y;
            this.scale = element.Scale;
            this.unknown4 = element.Unknown4;
            this.visibility = element.Visibility;
            this.unknown6 = element.Unknown6;
            this.opacity = element.Opacity;
            this.unknown8 = element.Unknown8;
        }
    }

    public class Element {
        public ElementKind Id { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Scale { get; set; }

        public byte[] Unknown4 { get; set; }

        public Visibility Visibility { get; set; }

        public byte Unknown6 { get; set; }

        public byte Opacity { get; set; }

        public byte[] Unknown8 { get; set; }

        public Element(RawElement raw) {
            this.Id = raw.id;
            this.X = raw.x;
            this.Y = raw.y;
            this.Scale = raw.scale;
            this.Unknown4 = raw.unknown4;
            this.Visibility = raw.visibility;
            this.Unknown6 = raw.unknown6;
            this.Opacity = raw.opacity;
            this.Unknown8 = raw.unknown8;
        }
    }
}
