using System.ComponentModel.DataAnnotations;

namespace HUDManager.Structs.Options;

public class StatusEffectsOptions {
    private readonly EnumBitField<StatusGrouping> grouping;
    private readonly EnumBitField<StatusAlignment> alignment;

    public StatusAlignment Alignment {
        get => alignment.Value;
        set => alignment.Value = value;
    }

    public StatusGrouping Grouping {
        get => grouping.Value;
        set => grouping.Value = value;
    }

    public StatusEffectsOptions(byte[] options) {
        grouping = new EnumBitField<StatusGrouping>(options, 0, 0, 4);
        alignment = new EnumBitField<StatusAlignment>(options, 0, 4, 4);
    }
}

public enum StatusGrouping : byte {
    [Display(Name = "Split Element into 3 Groups", Order = 1)]
    ThreeGroups = 0,
    [Display(Name = "Display as Single Element", Order = 0)]
    Normal = 1,
    [Display(Name = "Split Element into 4 Groups", Order = 2)]
    FourGroups = 2,
}

public enum StatusAlignment : byte {
    [Display(Name = "Normal")]
    Normal = 0,
    [Display(Name = "Left-justified I")]
    LeftJustified1 = 1,
    [Display(Name = "Left-justified II")]
    LeftJustified2 = 2,
    [Display(Name = "Left-justified III")]
    LeftJustified3 = 3,
}
