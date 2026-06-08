using System;
using System.ComponentModel.DataAnnotations;

namespace HUDManager.Structs;

public enum MeasuredFrom : byte
{
    [Display(Name = "Top left")]
    TopLeft = 0,
    [Display(Name = "Top middle")]
    TopMiddle = 1,
    [Display(Name = "Top right")]
    TopRight = 2,
    [Display(Name = "Middle left")]
    MiddleLeft = 3,
    [Display(Name = "Middle")]
    Middle = 4,
    [Display(Name = "Middle right")]
    MiddleRight = 5,
    [Display(Name = "Bottom left")]
    BottomLeft = 6,
    [Display(Name = "Bottom middle")]
    BottomMiddle = 7,
    [Display(Name = "Bottom right")]
    BottomRight = 8,
}

public static class MeasureFromExt
{
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
