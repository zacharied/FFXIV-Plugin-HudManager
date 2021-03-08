using System.Runtime.InteropServices;

namespace HUD_Manager.Structs {
    [StructLayout(LayoutKind.Sequential)]
    public struct Element {
        // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        // public byte[] unknown0;

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
    }
}
