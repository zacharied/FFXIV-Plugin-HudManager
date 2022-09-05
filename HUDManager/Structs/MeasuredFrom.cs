using System;

namespace HUD_Manager.Structs
{
    public enum MeasuredFrom : byte
    {
        TopLeft = 0,
        TopMiddle = 1,
        TopRight = 2,
        MiddleLeft = 3,
        Middle = 4,
        MiddleRight = 5,
        BottomLeft = 6,
        BottomMiddle = 7,
        BottomRight = 8,
    }

    public static class MeasureFromExt
    {
        public static string Name(this MeasuredFrom measuredFrom)
        {
            return measuredFrom switch
            {
                MeasuredFrom.TopLeft => "Top left",
                MeasuredFrom.TopMiddle => "Top middle",
                MeasuredFrom.TopRight => "Top right",
                MeasuredFrom.MiddleLeft => "Middle left",
                MeasuredFrom.Middle => "Middle",
                MeasuredFrom.MiddleRight => "Middle right",
                MeasuredFrom.BottomLeft => "Bottom left",
                MeasuredFrom.BottomMiddle => "Bottom middle",
                MeasuredFrom.BottomRight => "Bottom right",
                _ => measuredFrom.ToString(),
            };
        }

        public static Tuple<MeasuredX, MeasuredY> ToParts(this MeasuredFrom measured)
        {
            return measured switch
            {
                MeasuredFrom.TopLeft => Tuple.Create(MeasuredX.Left, MeasuredY.Top),
                MeasuredFrom.TopMiddle => Tuple.Create(MeasuredX.Middle, MeasuredY.Top),
                MeasuredFrom.TopRight => Tuple.Create(MeasuredX.Right, MeasuredY.Top),
                MeasuredFrom.MiddleLeft => Tuple.Create(MeasuredX.Left, MeasuredY.Middle),
                MeasuredFrom.Middle => Tuple.Create(MeasuredX.Middle, MeasuredY.Middle),
                MeasuredFrom.MiddleRight => Tuple.Create(MeasuredX.Right, MeasuredY.Middle),
                MeasuredFrom.BottomLeft => Tuple.Create(MeasuredX.Left, MeasuredY.Bottom),
                MeasuredFrom.BottomMiddle => Tuple.Create(MeasuredX.Middle, MeasuredY.Bottom),
                MeasuredFrom.BottomRight => Tuple.Create(MeasuredX.Right, MeasuredY.Bottom),
                _ => throw new ArgumentOutOfRangeException(nameof(measured), measured, null),
            };
        }
    }

    public enum MeasuredX
    {
        Left,
        Middle,
        Right,
    }

    public enum MeasuredY
    {
        Top,
        Middle,
        Bottom,
    }
}
