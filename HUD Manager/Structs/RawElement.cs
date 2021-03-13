using System.Runtime.InteropServices;

namespace HUD_Manager.Structs {
    [StructLayout(LayoutKind.Sequential)]
    public struct RawElement {
        public ElementKind id;

        public float x;

        public float y;

        public float scale;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] options;

        public ushort width;

        public ushort height;

        public MeasuredFrom measuredFrom;

        public VisibilityFlags visibility;

        public byte unknown6;

        public byte opacity;

        // last two bytes are padding
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] unknown8;

        public RawElement(Element element) {
            this.id = element.Id;
            this.x = element.X;
            this.y = element.Y;
            this.scale = element.Scale;
            this.options = element.Options;
            this.width = element.Width;
            this.height = element.Height;
            this.measuredFrom = element.MeasuredFrom;
            this.visibility = element.Visibility;
            this.unknown6 = element.Unknown6;
            this.opacity = element.Opacity;
            this.unknown8 = element.Unknown8;
        }

        public void UpdateEnabled(Element element) {
            if (element[ElementComponent.X]) {
                this.x = element.X;
            }

            if (element[ElementComponent.Y]) {
                this.y = element.Y;
            }

            if (element[ElementComponent.Scale]) {
                this.scale = element.Scale;
            }

            if (element[ElementComponent.Visibility]) {
                this.visibility = element.Visibility;
            }

            if (element[ElementComponent.Opacity]) {
                this.opacity = element.Opacity;
            }

            if (element[ElementComponent.Options]) {
                this.options = element.Options;
            }
        }
    }
}
