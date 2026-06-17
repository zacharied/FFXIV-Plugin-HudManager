using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HUDManager.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct Layout
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Hud.InMemoryLayoutElements)]
    public RawElement[] elements;

    public Dictionary<ElementKind, Element> ToDictionary()
    {
        // NOTE: not using ToDictionary here because duplicate keys are possible with old broken layouts
        var dict = new Dictionary<ElementKind, Element>();
        foreach (var elem in elements) {
            if (elem.id == 0) {
                continue;
            }

            dict[elem.id] = new Element(elem);
        }

        return dict;
    }
}
