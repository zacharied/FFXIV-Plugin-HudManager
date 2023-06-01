﻿using Dalamud.Data;
using Dalamud.Logging;
using HUD_Manager.Lumina;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HUD_Manager.Structs
{
    public enum ElementKind : uint
    {
        FocusTargetBar = 3264409695,
        StatusInfoEnfeeblements = 511728259,
        StatusInfoEnhancements = 524431540,
        StatusInfoConditionalEnhancements = 486837997,
        StatusInfoOther = 482796762,
        TargetInfoHp = 3172107127,
        TargetInfoProgressBar = 3411321583,
        TargetInfoStatus = 124737899,
        PartyList = 1027756089,
        EnemyList = 3099420293,
        ScenarioGuide = 2297324375,
        ExperienceBar = 568671438,
        PetHotbar = 3637610751,
        Hotbar10 = 4117249958,
        Hotbar9 = 4104803729,
        Hotbar8 = 4294316716,
        Hotbar7 = 4264851611,
        Hotbar6 = 4235380418,
        Hotbar5 = 4256214261,
        Hotbar4 = 4177508976,
        Hotbar3 = 4181577799,
        Hotbar2 = 4219170334,
        Hotbar1 = 3297588741,
        CrossHotbar = 3129075921,
        ProgressBar = 3971127313,
        Minimap = 1901658651,
        BloodGauge = 4031678328, // DRK
        DarksideGauge = 4052544847, // DRK
        OathGauge = 4022009408, // PLD
        BeastGauge = 2136801802, // WAR
        PowderGauge = 2931998943, // GNB
        ArcanaGauge = 2509863090, // AST
        AetherflowGaugeSch = 3403503819, // SCH
        FaerieGauge = 2712183943, // SCH
        HealingGauge = 2050434994, // WHM
        DragonGauge = 3130538176, // DRG
        MastersGauge = 1917955123, // MNK
        ChakraGauge = 1939064324, // MNK
        HutonGauge = 1895405704, // NIN
        NinkiGauge = 1899754175, // NIN
        SenGauge = 3983830498, // SAM
        KenkiGauge = 3971352533, // SAM
        SongGauge = 2121561139, // BRD
        HeatGauge = 2557790060, // MCH
        StepGauge = 2431309076, // DNC
        FourfoldFeathers = 2435366691, // DNC
        ElementalGauge = 3702264410, // BLM
        BalanceGauge = 4010433280, // RDM
        TranceGauge = 976301837, // SMN
        AetherflowGaugeSmn = 1005798714, // SMN
        AddersgallGauge = 269645438, // SGE
        EukrasiaGauge = 298851401, // SGE
        SoulGauge = 2732176992, // RPR
        DeathGauge = 2736512087, // RPR
        ItemHelp = 1120659295,
        ActionHelp = 1180822218,
        Gil = 1125522082,
        InventoryGrid = 471196175,
        MainMenu = 2331597424,
        Notices = 3743511396,
        ParameterBar = 2552153246,
        LimitGauge = 3349103882,
        DutyList = 2727411922,
        ServerInfo = 3450378102,
        AllianceList1 = 692286106,
        AllianceList2 = 721800387,
        NewGamePlusGuide = 3660166250,
        TargetBar = 2436811133,
        StatusEffects = 1247188502,
        DutyGauge = 2168013717,
        DutyAction = 1421395594,
        CompressedAether = 3300326260,
        RivalWingsMercenaryInfo = 2045148168,
        RivalWingsTeamInfo = 1180574470,
        RivalWingsStationInfo = 2702651974,
        RivalWingsAllianceList = 3869061330,
        RivalWingsGauges = 273150177,
        TheFeastEnemyInfo = 912936203,
        TheFeastAllyInfo = 933766972,
        TheFeastScore = 3622852831,
        BattleHighGauge = 884971695,
        LeftWCrossHotbar = 1717924701,
        RightWCrossHotbar = 1893596455,
        OceanFishingVoyageMissions = 1, // TODO Fix this or something
        Timers = 2578885979,
        CrystallineConflictAllyInfo = 3780501874,
        CrystallineConflictBattleLog = 4081580003,
        CrystallineConflictMap = 3282875858,
        CrystallineConflictEnemyInfo = 3805361436,
        CrystallineConflictProgressGauge = 812941873,
        FrontlineScoreInfo = 758283662
    }

    public static class ElementKindExt
    {
        private static ReadOnlyDictionary<ClassJob, List<ElementKind>>? BarsPerJob = null;
        private static ReadOnlyDictionary<ElementKind, ClassJob>? GaugeJobs = null;
        private static readonly ReadOnlyDictionary<uint, string> JobGaugeAddonNameOverrides = new(new Dictionary<uint, string>()
        {
            [19] = "RRP", // RPR lmao
            [20] = "GFF"  // SGE
        });

        private static bool Initialized => GaugeJobs is not null && BarsPerJob is not null;

        public static void Initialize(DataManager data)
        {
            InitializeBarsPerJob(data);

            Dictionary<ElementKind, ClassJob> gaugeJobs = new();
            foreach (var e in All().Where(e => e.IsJobGauge()))
                gaugeJobs[e] = e.ClassJob(data)!;
            GaugeJobs = new(gaugeJobs);
        }

        public static readonly ElementKind[] Immutable = {
            // cannot be moved with the current method the plugin is using
            ElementKind.OceanFishingVoyageMissions,

            // don't actually know if this is immutable, but idk what it is
            ElementKind.Timers,
        };

        public static IEnumerable<ElementKind> All() => Enum.GetValues(typeof(ElementKind))
            .Cast<ElementKind>()
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
                // ElementKind.DutyList => 32, // listed twice?
                ElementKind.Timers => 33,
                // 34-37 empty
                ElementKind.LeftWCrossHotbar => 38,
                ElementKind.RightWCrossHotbar => 39,
                ElementKind.OathGauge => 40,
                // 41 is "LightningGauge" - guessing that's for GL
                ElementKind.BeastGauge => 42,
                ElementKind.DragonGauge => 43,
                ElementKind.SongGauge => 44,
                ElementKind.HealingGauge => 45,
                ElementKind.ElementalGauge => 46,
                ElementKind.AetherflowGaugeSch => 47, // order? - same name, so not sure which key is for which
                ElementKind.AetherflowGaugeSmn => 48, // order?
                ElementKind.TranceGauge => 49,
                ElementKind.FaerieGauge => 50,
                ElementKind.NinkiGauge => 51,
                ElementKind.HeatGauge => 52,
                // 53 is empty
                ElementKind.BloodGauge => 54,
                ElementKind.ArcanaGauge => 55,
                ElementKind.KenkiGauge => 56,
                ElementKind.SenGauge => 57,
                ElementKind.BalanceGauge => 58,
                ElementKind.DutyGauge => 59,
                ElementKind.DutyAction => 60,
                ElementKind.ChakraGauge => 61,
                ElementKind.HutonGauge => 62,
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
                //ElementKind.OceanFishingVoyageMissions => 85,
                ElementKind.StatusInfoConditionalEnhancements => 86,
                ElementKind.SoulGauge => 87,
                ElementKind.DeathGauge => 88,
                ElementKind.EukrasiaGauge => 89,
                ElementKind.AddersgallGauge => 90,
                ElementKind.MastersGauge => 91,
                ElementKind.CrystallineConflictProgressGauge => 92,
                ElementKind.CrystallineConflictAllyInfo => 93,
                ElementKind.CrystallineConflictEnemyInfo => 95,
                ElementKind.CrystallineConflictBattleLog => 96,
                ElementKind.CrystallineConflictMap => 97,
                ElementKind.FrontlineScoreInfo => 98,
                _ => -1
            };
        }

        public static bool IsRealElement(this ElementKind kind)
        {
            return kind.ElementKindRowId() >= 0;
        }

        public static string LocalisedName(this ElementKind kind, DataManager data)
        {
            int id = kind.ElementKindRowId();

            if (id < 0) {
                return kind.ToString();
            }

            var name = data.GetExcelSheet<HudSheet>().GetRow((uint)id).Name;

            uint? jobId = kind switch
            {
                ElementKind.AetherflowGaugeSmn => 27,
                ElementKind.AetherflowGaugeSch => 28,
                _ => null,
            };

            if (jobId != null) {
                var abbr = data.GetExcelSheet<ClassJob>().GetRow(jobId.Value).Abbreviation;
                name += $" ({abbr})";
            }

            return name;
        }

        public static bool IsJobGauge(this ElementKind kind)
        {
            switch (kind) {
                case ElementKind.AetherflowGaugeSch:
                case ElementKind.AetherflowGaugeSmn:
                case ElementKind.ArcanaGauge:
                case ElementKind.BalanceGauge:
                case ElementKind.BeastGauge:
                case ElementKind.BloodGauge:
                case ElementKind.ChakraGauge:
                case ElementKind.DarksideGauge:
                case ElementKind.DragonGauge:
                case ElementKind.ElementalGauge:
                case ElementKind.FaerieGauge:
                case ElementKind.FourfoldFeathers:
                case ElementKind.HealingGauge:
                case ElementKind.HeatGauge:
                case ElementKind.HutonGauge:
                case ElementKind.KenkiGauge:
                case ElementKind.NinkiGauge:
                case ElementKind.OathGauge:
                case ElementKind.PowderGauge:
                case ElementKind.SenGauge:
                case ElementKind.SongGauge:
                case ElementKind.StepGauge:
                case ElementKind.TranceGauge:
                case ElementKind.AddersgallGauge:
                case ElementKind.EukrasiaGauge:
                case ElementKind.SoulGauge:
                case ElementKind.DeathGauge:
                case ElementKind.MastersGauge:
                    return true;
                default:
                    return false;
            }
        }

        public static ClassJob? ClassJob(this ElementKind kind)
        {
            if (GaugeJobs is null)
                throw new InvalidOperationException("call `Initialize` first");
            return GaugeJobs!.GetValueOrDefault(kind);
        }

        private static ClassJob? ClassJob(this ElementKind kind, DataManager data)
        {
            if (Initialized)
                throw new InvalidOperationException("already initialized");

            ClassJob FindClassJob(int id)
            {
                var sheet = data.GetExcelSheet<ClassJob>()!;
                return sheet.First(job => job.JobIndex == id);
            }

            switch (kind) {
                case ElementKind.AetherflowGaugeSch:
                case ElementKind.FaerieGauge:
                    return FindClassJob(9);
                case ElementKind.AetherflowGaugeSmn:
                case ElementKind.TranceGauge:
                    return FindClassJob(8);
                case ElementKind.ArcanaGauge:
                    return FindClassJob(13);
                case ElementKind.BalanceGauge:
                    return FindClassJob(15);
                case ElementKind.BeastGauge:
                    return FindClassJob(3);
                case ElementKind.BloodGauge:
                case ElementKind.DarksideGauge:
                    return FindClassJob(12);
                case ElementKind.ChakraGauge:
                case ElementKind.MastersGauge:
                    return FindClassJob(2);
                case ElementKind.DragonGauge:
                    return FindClassJob(4);
                case ElementKind.ElementalGauge:
                    return FindClassJob(7);
                case ElementKind.FourfoldFeathers:
                case ElementKind.StepGauge:
                    return FindClassJob(18);
                case ElementKind.HealingGauge:
                    return FindClassJob(6);
                case ElementKind.HeatGauge:
                    return FindClassJob(11);
                case ElementKind.HutonGauge:
                case ElementKind.NinkiGauge:
                    return FindClassJob(10);
                case ElementKind.KenkiGauge:
                case ElementKind.SenGauge:
                    return FindClassJob(14);
                case ElementKind.OathGauge:
                    return FindClassJob(1);
                case ElementKind.PowderGauge:
                    return FindClassJob(17);
                case ElementKind.SongGauge:
                    return FindClassJob(5);
                case ElementKind.AddersgallGauge:
                case ElementKind.EukrasiaGauge:
                    return FindClassJob(20);
                case ElementKind.SoulGauge:
                case ElementKind.DeathGauge:
                    return FindClassJob(19);
            }

            return null;
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

        public static string? GetJobGaugeAtkName(this ElementKind kind)
        {
            if (!Initialized)
                throw new InvalidOperationException("call `Initialize` first");

            if (!kind.IsJobGauge())
                return null;

            var job = kind.ClassJob();
            var index = (BarsPerJob![job!].IndexOf(kind) + (AtkElementReverseOrderedJobs.Contains(job!.JobIndex) ? 1 : 0)) % 2;

            // SCH is just weird, EW jobs are spelled differently.
            var gaugeName = kind is ElementKind.AetherflowGaugeSch ? "ACN0"
                : JobGaugeAddonNameOverrides.ContainsKey(job.JobIndex) ? $"{JobGaugeAddonNameOverrides[job.JobIndex]}{index}"
                : $"{Util.JobIdToEnglishAbbreviation[job.RowId].ToUpper()}{index}";

            return $"JobHud{gaugeName}";
        }

        private static void InitializeBarsPerJob(DataManager data)
        {
            if (Initialized)
                throw new InvalidOperationException("already initialized");

            Dictionary<ClassJob, List<ElementKind>> barsPerJob = new();

            foreach (var e in All().Where(e => e.IsJobGauge())) {
                var j = e.ClassJob(data);
                if (!barsPerJob.ContainsKey(j))
                    barsPerJob[j] = new List<ElementKind>();
                barsPerJob[j].Add(e);
            }

            BarsPerJob = new(barsPerJob);
        }

        // TODO Solution that works with more than 2 job gauges (idk though)
        private static readonly uint[] AtkElementReverseOrderedJobs = {
            10, // NIN
            8, // SMN
            19, // RPR
            20 // SGE
        };
    }
}
