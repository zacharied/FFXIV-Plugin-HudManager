using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace HUDManager.Structs;

// Enum values are calculated by the game using CRC32($"{addonName}_a")
public enum ElementKind : uint
{
    // @formatter:off
    Hotbar1                           = 0xC48D3605, // _ActionBar_a
    Hotbar2                           = 0xFB7B6E1E, // _ActionBar01_a
    Hotbar3                           = 0xF93DD047, // _ActionBar02_a
    Hotbar4                           = 0xF8FFBA70, // _ActionBar03_a
    Hotbar5                           = 0xFDB0ACF5, // _ActionBar04_a
    Hotbar6                           = 0xFC72C6C2, // _ActionBar05_a
    Hotbar7                           = 0xFE34789B, // _ActionBar06_a
    Hotbar8                           = 0xFFF612AC, // _ActionBar07_a
    Hotbar9                           = 0xF4AA5591, // _ActionBar08_a
    Hotbar10                          = 0xF5683FA6, // _ActionBar09_a
    PetHotbar                         = 0xD8D188FF, // _ActionBarEx_a
    CrossHotbar                       = 0xBA81E8D1, // _ActionCross_a
    LeftWCrossHotbar                  = 0x6665735D, // _ActionDoubleCrossL_a
    RightWCrossHotbar                 = 0x70DDFD27, // _ActionDoubleCrossR_a
    ProgressBar                       = 0xECB29811, // _CastBar_a
    TargetBar                         = 0x913EC97D, // _TargetInfo_a
    TargetInfoHp                      = 0xBD128377, // _TargetInfoMainTarget_a
    TargetInfoProgressBar             = 0xCB54A2EF, // _TargetInfoCastBar_a
    TargetInfoStatus                  = 0x076F596B, // _TargetInfoBuffDebuff_a
    FocusTargetBar                    = 0xC292F05F, // _FocusTargetInfo_a
    ScenarioGuide                     = 0x88EE6357, // ScenarioTree_a
    PartyList                         = 0x3D425039, // _PartyList_a
    AllianceList1                     = 0x2943729A, // _AllianceList1_a
    AllianceList2                     = 0x2B05CCC3, // _AllianceList2_a
    EnemyList                         = 0xB8BD6685, // _EnemyList_a
    ParameterBar                      = 0x981EC49E, // _ParameterWidget_a
    ExperienceBar                     = 0x21E53CCE, // _Exp_a
    StatusEffects                     = 0x4A569616, // _Status_a
    StatusInfoEnhancements            = 0x1F4230B4, // _StatusCustom0_a
    StatusInfoConditionalEnhancements = 0x1D048EED, // _StatusCustom3_a
    StatusInfoEnfeeblements           = 0x1E805A83, // _StatusCustom1_a
    StatusInfoOther                   = 0x1CC6E4DA, // _StatusCustom2_a
    Minimap                           = 0x7159021B, // _NaviMap_a
    Notices                           = 0xDF217364, // _Notification_a
    MainMenu                          = 0x8AF95A70, // _MainCommand_a
    DutyList                          = 0xA29100D2, // _ToDoList_a
    ServerInfo                        = 0xCDA89776, // _DTR_a
    Gil                               = 0x43161AA2, // _Money_a
    InventoryGrid                     = 0x1C15E20F, // _BagWidget_a
    ItemHelp                          = 0x42CBE75F, // ItemDetail_a
    ActionHelp                        = 0x4661EACA, // ActionDetail_a
    LimitGauge                        = 0xC79F450A, // _LimitBreak_a
    DutyGauge                         = 0x81394395, // _ContentGauge_a
    DutyAction                        = 0x54B8C68A, // _ActionContents_a
    OathGauge                         = 0xEFBAFE40, // JobHudPLD0_a (PLD)
    MastersGauge                      = 0x7251AC33, // JobHudMNK0_a (MNK)
    ChakraGauge                       = 0x7393C604, // JobHudMNK1_a (MNK)
    BeastGauge                        = 0x7F5D020A, // JobHudWAR0_a (WAR)
    DragonGauge                       = 0xBA9838C0, // JobHudDRG0_a (DRG)
    SongGauge                         = 0x7E747433, // JobHudBRD0_a (BRD)
    HealingGauge                      = 0x7A3727B2, // JobHudWHM0_a (WHM)
    ElementalGauge                    = 0xDCAC125A, // JobHudBLM0_a (BLM)
    AstralGauge                       = 0xDD6E786D, // JobHudBLM1_a (BLM)
    AetherflowGaugeSch                = 0xCADD58CB, // JobHudACN0_a (SCH)
    AetherflowGaugeSmn                = 0x3BF3453A, // JobHudSMN0_a (SMN)
    TranceGauge                       = 0x3A312F0D, // JobHudSMN1_a (SMN)
    FaerieGauge                       = 0xA1A8A487, // JobHudSCH0_a (SCH)
    HutonGauge                        = 0x70F99888, // JobHudNIN1_a (NIN) (removed in 7.0)
    Kazematoi                         = 0x6CD4313E, // JobHudNIN1v70_a (NIN)
    NinkiGauge                        = 0x713BF2BF, // JobHudNIN0_a (NIN)
    HeatGauge                         = 0x9874C76C, // JobHudMCH0_a (MCH)
    BloodGauge                        = 0xF04E8778, // JobHudDRK0_a (DRK)
    DarksideGauge                     = 0xF18CED4F, // JobHudDRK1_a (DRK)
    ArcanaGauge                       = 0x959978B2, // JobHudAST0_a (AST)
    KenkiGauge                        = 0xECB607D5, // JobHudSAM0_a (SAM)
    SenGauge                          = 0xED746DE2, // JobHudSAM1_a (SAM)
    BalanceGauge                      = 0xEF0A5B00, // JobHudRDM0_a (RDM)
    PowderGauge                       = 0xAEC2C0DF, // JobHudGNB0_a (GNB)
    StepGauge                         = 0x90EAD514, // JobHudDNC0_a (DNC)
    FourfoldFeathers                  = 0x9128BF23, // JobHudDNC1_a (DNC)
    SoulGauge                         = 0xA2D9B660, // JobHudRRP0_a (RPR)
    DeathGauge                        = 0xA31BDC57, // JobHudRRP1_a (RPR)
    EukrasiaGauge                     = 0x11D01C49, // JobHudGFF0_a (SGE)
    AddersgallGauge                   = 0x1012767E, // JobHudGFF1_a (SGE)
    Vipersight                        = 0xB7694B56, // JobHudRDB0_a (VIP)
    SerpentOfferingsGauge             = 0xB6AB2161, // JobHudRDB1_a (VIP)
    Canvases                          = 0x7A6A6A42, // JobHudRPM0_a (PCT)
    PaletteGauge                      = 0x7BA80075, // JobHudRPM1_a (PCT)
    TheFeastScore                     = 0xD7F058DF, // PvPColosseumHeader_a
    TheFeastEnemyInfo                 = 0x366A4D0B, // PvPColosseumPartyList1_a
    TheFeastAllyInfo                  = 0x37A8273C, // PvPColosseumPartyList0_a
    CrystallineConflictProgressGauge  = 0x30748231, // PvPMKSHeader_a
    CrystallineConflictAllyInfo       = 0xE155E172, // PvPMKSPartyList1_a
    CrystallineConflictEnemyInfo      = 0xE2D1351C, // PvPMKSPartyList3_a
    CrystallineConflictBattleLog      = 0xF347F7E3, // PvPMKSBattleLog_a
    CrystallineConflictMap            = 0xC3ACB5D2, // PvPMap_a
    FrontlineScoreInfo                = 0x2D327D8E, // PvPFrontlineHeader_a
    BattleHighGauge                   = 0x34BF98AF, // PvPFrontlineGauge_a
    RivalWingsGauges                  = 0x1047F0E1, // ManeuversHeader_a
    RivalWingsAllianceList            = 0xE69D30D2, // ManeuversAllianceList_a
    RivalWingsTeamInfo                = 0x465E2306, // ManeuversTeamInfo_a
    RivalWingsStationInfo             = 0xA1173246, // ManeuversStation_a
    RivalWingsMercenaryInfo           = 0x79E67C08, // ManeuversGoblinSoldier_a
    CompressedAether                  = 0xC4B6FB74, // HWDAetherGauge_a
    NewGamePlusGuide                  = 0xDA29B46A, // QuestRedoHud_a
    OceanFishingVoyageMissions        = 0xB6D09C70, // IKDMission_a
    BlundervilleObjective             = 0x3B26DB7A, // FGSHudGoal_a
    BlundervilleScore                 = 0xDBC09DEA, // FGSHudScore_a
    BlundervilleStatus                = 0x9F0BB04E, // FGSHudStatus_a
    BlundervilleShowLog               = 0xD83AAFFA, // FGSHudRaceLog_a
    Timers                            = 0x99B6AD5B, // ??? (removed in ???)
    // @formatter:on
}

public static class ElementKindExt
{
    private static FrozenDictionary<ElementKind, ClassJob> _gaugeJobs = null!;

    public static void Initialize(IDataManager data)
    {
        Dictionary<ElementKind, ClassJob> gaugeJobs = new();

        var sheet = data.GetExcelSheet<ClassJob>()!;
        foreach (var e in All()) {
            if (e.ClassJob(sheet) is { } classJob) {
                gaugeJobs[e] = classJob;
            }
        }
        _gaugeJobs = new Dictionary<ElementKind, ClassJob>(gaugeJobs).ToFrozenDictionary();
    }

    public static readonly ElementKind[] Immutable =
    [
        // don't actually know if this is immutable, but idk what it is
        ElementKind.Timers,
    ];

    public static IEnumerable<ElementKind> All() => Enum.GetValues<ElementKind>()
        .Where(kind => !Immutable.Contains(kind));

    private static int ElementKindRowId(this ElementKind kind)
    {
        return kind switch
        {
            ElementKind.Hotbar1 => 0,
            ElementKind.Hotbar2 => 1,
            ElementKind.Hotbar3 => 2,
            ElementKind.Hotbar4 => 3,
            ElementKind.Hotbar5 => 4,
            ElementKind.Hotbar6 => 5,
            ElementKind.Hotbar7 => 6,
            ElementKind.Hotbar8 => 7,
            ElementKind.Hotbar9 => 8,
            ElementKind.Hotbar10 => 9,
            ElementKind.PetHotbar => 10,
            ElementKind.CrossHotbar => 11,
            ElementKind.ProgressBar => 12,
            ElementKind.TargetBar => 13,
            ElementKind.FocusTargetBar => 14,
            ElementKind.PartyList => 15,
            ElementKind.EnemyList => 16,
            ElementKind.ParameterBar => 17,
            ElementKind.Notices => 18,
            ElementKind.Minimap => 19,
            ElementKind.MainMenu => 20,
            ElementKind.ServerInfo => 21,
            ElementKind.Gil => 22,
            ElementKind.InventoryGrid => 23,
            ElementKind.DutyList => 24,
            ElementKind.ItemHelp => 25,
            ElementKind.ActionHelp => 26,
            ElementKind.LimitGauge => 27,
            ElementKind.ExperienceBar => 28,
            ElementKind.StatusEffects => 29,
            ElementKind.AllianceList1 => 30,
            ElementKind.AllianceList2 => 31,
            // ElementKind.DutyList => 32, // Listed twice, here and at 24. Not sure if this one is ever used?
            ElementKind.Timers => 33, // Doesn't seem to be used
            // 34-37 empty
            ElementKind.LeftWCrossHotbar => 38,
            ElementKind.RightWCrossHotbar => 39,
            ElementKind.OathGauge => 40,
            // ElementKind.LightningGauge => 41, // Discontinued monk gauge
            ElementKind.BeastGauge => 42,
            ElementKind.DragonGauge => 43,
            ElementKind.SongGauge => 44,
            ElementKind.HealingGauge => 45,
            ElementKind.ElementalGauge => 46,
            ElementKind.AetherflowGaugeSch => 47,
            ElementKind.AetherflowGaugeSmn => 48,
            ElementKind.TranceGauge => 49,
            ElementKind.FaerieGauge => 50,
            ElementKind.NinkiGauge => 51,
            ElementKind.HeatGauge => 52,
            // 53 empty
            ElementKind.BloodGauge => 54,
            ElementKind.ArcanaGauge => 55,
            ElementKind.KenkiGauge => 56,
            ElementKind.SenGauge => 57,
            ElementKind.BalanceGauge => 58,
            ElementKind.DutyGauge => 59,
            ElementKind.DutyAction => 60,
            ElementKind.ChakraGauge => 61,
            ElementKind.Kazematoi => 62, // Was Huton prior to 7.0
            ElementKind.ScenarioGuide => 63,
            ElementKind.RivalWingsGauges => 64,
            ElementKind.RivalWingsAllianceList => 65,
            ElementKind.RivalWingsTeamInfo => 66,
            ElementKind.StatusInfoEnhancements => 67,
            ElementKind.StatusInfoEnfeeblements => 68,
            ElementKind.StatusInfoOther => 69,
            ElementKind.TargetInfoStatus => 70,
            ElementKind.TargetInfoProgressBar => 71,
            ElementKind.TargetInfoHp => 72,
            ElementKind.TheFeastScore => 73,
            ElementKind.TheFeastAllyInfo => 74,
            ElementKind.TheFeastEnemyInfo => 75,
            ElementKind.RivalWingsStationInfo => 76,
            ElementKind.RivalWingsMercenaryInfo => 77,
            ElementKind.DarksideGauge => 78,
            ElementKind.PowderGauge => 79,
            ElementKind.StepGauge => 80,
            ElementKind.FourfoldFeathers => 81,
            ElementKind.BattleHighGauge => 82,
            ElementKind.NewGamePlusGuide => 83,
            ElementKind.CompressedAether => 84,
            ElementKind.OceanFishingVoyageMissions => 85,
            ElementKind.StatusInfoConditionalEnhancements => 86,
            ElementKind.SoulGauge => 87,
            ElementKind.DeathGauge => 88,
            ElementKind.EukrasiaGauge => 89,
            ElementKind.AddersgallGauge => 90,
            ElementKind.MastersGauge => 91,
            ElementKind.CrystallineConflictProgressGauge => 92,
            ElementKind.CrystallineConflictAllyInfo => 93,
            // 94 empty
            ElementKind.CrystallineConflictEnemyInfo => 95,
            ElementKind.CrystallineConflictBattleLog => 96,
            ElementKind.CrystallineConflictMap => 97,
            ElementKind.FrontlineScoreInfo => 98,
            ElementKind.BlundervilleObjective => 99,
            ElementKind.BlundervilleScore => 100,
            ElementKind.BlundervilleStatus => 101,
            ElementKind.BlundervilleShowLog => 102,
            ElementKind.AstralGauge => 103,
            ElementKind.Vipersight => 104,
            ElementKind.SerpentOfferingsGauge => 105,
            ElementKind.Canvases => 106,
            ElementKind.PaletteGauge => 107,
            _ => -1,
        };
    }

    public static bool IsRealElement(this ElementKind kind)
    {
        return kind.ElementKindRowId() >= 0;
    }

    public static string LocalisedName(this ElementKind kind, IDataManager data)
    {
        var id = kind.ElementKindRowId();
        if (id < 0) {
            return kind.ToString();
        }

        var name = data.GetExcelSheet<Lumina.Excel.Sheets.Hud>().GetRowOrDefault((uint)id)?.Unknown0.ExtractText() ?? kind.ToString();

        if (kind.ClassJob() is {} classJob) {
            name += $" ({classJob.Abbreviation})";
        }

        return name;
    }

    public static ClassJob? ClassJob(this ElementKind kind)
    {
        if (_gaugeJobs.TryGetValue(kind, out var classJob)) {
            return classJob;
        }
        return null;
    }

    private static ClassJob? ClassJob(this ElementKind kind, ExcelSheet<ClassJob> sheet)
    {
        return kind switch
        {
            ElementKind.OathGauge => FindClassJob(1),
            ElementKind.ChakraGauge or ElementKind.MastersGauge => FindClassJob(2),
            ElementKind.BeastGauge => FindClassJob(3),
            ElementKind.DragonGauge => FindClassJob(4),
            ElementKind.SongGauge => FindClassJob(5),
            ElementKind.HealingGauge => FindClassJob(6),
            ElementKind.ElementalGauge or ElementKind.AstralGauge => FindClassJob(7),
            ElementKind.AetherflowGaugeSmn or ElementKind.TranceGauge => FindClassJob(8),
            ElementKind.AetherflowGaugeSch or ElementKind.FaerieGauge => FindClassJob(9),
            ElementKind.HutonGauge or ElementKind.Kazematoi or ElementKind.NinkiGauge => FindClassJob(10),
            ElementKind.HeatGauge => FindClassJob(11),
            ElementKind.BloodGauge or ElementKind.DarksideGauge => FindClassJob(12),
            ElementKind.ArcanaGauge => FindClassJob(13),
            ElementKind.KenkiGauge or ElementKind.SenGauge => FindClassJob(14),
            ElementKind.BalanceGauge => FindClassJob(15),
            ElementKind.PowderGauge => FindClassJob(17),
            ElementKind.FourfoldFeathers or ElementKind.StepGauge => FindClassJob(18),
            ElementKind.SoulGauge or ElementKind.DeathGauge => FindClassJob(19),
            ElementKind.AddersgallGauge or ElementKind.EukrasiaGauge => FindClassJob(20),
            ElementKind.Vipersight or ElementKind.SerpentOfferingsGauge => FindClassJob(21),
            ElementKind.Canvases or ElementKind.PaletteGauge => FindClassJob(22),
            _ => null,
        };

        ClassJob FindClassJob(int id)
        {
            return sheet.First(job => job.JobIndex == id);
        }
    }

    public static string? GetJobGaugeAtkName(this ElementKind kind)
    {
        return kind switch
        {
            // @formatter:off
            ElementKind.OathGauge             => "JobHudPLD0",
            ElementKind.ChakraGauge           => "JobHudMNK0",
            ElementKind.MastersGauge          => "JobHudMNK0",
            ElementKind.BeastGauge            => "JobHudWAR0",
            ElementKind.DragonGauge           => "JobHudDRG0",
            ElementKind.SongGauge             => "JobHudBRD0",
            ElementKind.HealingGauge          => "JobHudWHM0",
            ElementKind.ElementalGauge        => "JobHudBLM0",
            ElementKind.AstralGauge           => "JobHudBLM1",
            ElementKind.AetherflowGaugeSmn    => "JobHudSMN0",
            ElementKind.TranceGauge           => "JobHudSMN1",
            ElementKind.AetherflowGaugeSch    => "JobHudSCH0",
            ElementKind.FaerieGauge           => "JobHudSCH1",
            ElementKind.NinkiGauge            => "JobHudNIN0",
            ElementKind.HutonGauge            => "JobHudNIN1",
            ElementKind.Kazematoi             => "JobHudNIN1v70",
            ElementKind.HeatGauge             => "JobHudMCH0",
            ElementKind.BloodGauge            => "JobHudDRK0",
            ElementKind.DarksideGauge         => "JobHudDRK1",
            ElementKind.ArcanaGauge           => "JobHudAST0",
            ElementKind.KenkiGauge            => "JobHudSAM1",
            ElementKind.SenGauge              => "JobHudSAM0",
            ElementKind.BalanceGauge          => "JobHudRDM0",
            ElementKind.PowderGauge           => "JobHudGNB0",
            ElementKind.StepGauge             => "JobHudDNC0",
            ElementKind.FourfoldFeathers      => "JobHudDNC1",
            ElementKind.DeathGauge            => "JobHudRRP0",
            ElementKind.SoulGauge             => "JobHudRRP1",
            ElementKind.EukrasiaGauge         => "JobHudGFF0",
            ElementKind.AddersgallGauge       => "JobHudGFF1",
            ElementKind.Vipersight            => "JobHudRDB0",
            ElementKind.SerpentOfferingsGauge => "JobHudRDB1",
            ElementKind.Canvases              => "JobHudRPM0",
            ElementKind.PaletteGauge          => "JobHudRPM1",
            _ => null,
            // @formatter:on
        };
    }

    public static bool IsHotbar(this ElementKind kind)
    {
        switch (kind) {
            case ElementKind.Hotbar1:
            case ElementKind.Hotbar2:
            case ElementKind.Hotbar3:
            case ElementKind.Hotbar4:
            case ElementKind.Hotbar5:
            case ElementKind.Hotbar6:
            case ElementKind.Hotbar7:
            case ElementKind.Hotbar8:
            case ElementKind.Hotbar9:
            case ElementKind.Hotbar10:
            case ElementKind.PetHotbar:
                return true;
            default:
                return false;
        }
    }
}
