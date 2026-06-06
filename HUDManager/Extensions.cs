using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace HUDManager;

public static class Extensions {
    extension<T>(T self) where T : struct, Enum {
        public string GetDisplayName() {
            if (Enum.GetName(self) is { } name
                && typeof(T).GetField(name) is { } field
                && field.GetCustomAttribute<DisplayAttribute>() is { } displayAttribute
                && displayAttribute.GetName() is { } displayName) {
                return displayName;
            }
            return self.ToString();
        }

        public int? GetDisplayOrder() {
            if (Enum.GetName(self) is { } name
                && typeof(T).GetField(name) is { } field
                && field.GetCustomAttribute<DisplayAttribute>() is { } displayAttribute
                && displayAttribute.GetOrder() is { } displayOrder) {
                return displayOrder;
            }
            return null;
        }
    }
}
