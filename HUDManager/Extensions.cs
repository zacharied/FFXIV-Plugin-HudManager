using System;
using System.Collections.Generic;
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


  extension<TK, TV>(OrderedDictionary<TK, TV> self) where TK : notnull {
      public bool SlideToStart(TK target) {
          if (!self.TryGetValue(target, out var value))
              throw new KeyNotFoundException($"Key '{target}' was not found.");

          var currentIndex = self.IndexOf(target);
          if (currentIndex == 0)
              return false;

          self.Remove(target);
          self.Insert(0, target, value);

          return true;
      }

      public bool SlideToEnd(TK target) {
          if (!self.TryGetValue(target, out var value)) throw new KeyNotFoundException($"Key '{target}' was not found.");

          var currentIndex = self.IndexOf(target);
          if (currentIndex == self.Count - 1)
              return false;

          self.Remove(target);
          self.Add(target, value);

          return true;
      }

      public bool SlideBefore(TK target, TK sibling) {
          return self.SlideTo(target, sibling, placeBefore: true);
      }

      public bool SlideAfter(TK target, TK sibling) {
          return self.SlideTo(target, sibling, placeBefore: false);
      }

      private bool SlideTo(TK target, TK sibling, bool placeBefore = false) {
          if (EqualityComparer<TK>.Default.Equals(target, sibling))
              return false;

          if (!self.TryGetValue(target, out var value))
              throw new KeyNotFoundException($"Target key '{target}' was not found.");

          if (!self.ContainsKey(sibling))
              throw new KeyNotFoundException($"Sibling key '{sibling}' was not found.");

          var targetIndex = self.IndexOf(target);
          var siblingIndex = self.IndexOf(sibling);
          var desiredIndex = placeBefore ? siblingIndex : siblingIndex + 1;

          if (targetIndex < desiredIndex)
              desiredIndex--;

          if (targetIndex == desiredIndex)
              return false;

          self.Remove(target);
          self.Insert(desiredIndex, target, value);

          return true;
      }
  }
}
