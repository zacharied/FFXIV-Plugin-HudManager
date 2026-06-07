using Dalamud.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HUDManager;

public static class ClassJobCategoryIdExtensions
{
    private static readonly ClassJobCategoryId[] ClassJobCombos =
    [
        ClassJobCategoryId.GLA_PLD,
        ClassJobCategoryId.PGL_MNK,
        ClassJobCategoryId.MRD_WAR,
        ClassJobCategoryId.LNC_DRG,
        ClassJobCategoryId.ARC_BRD,
        ClassJobCategoryId.CNJ_WHM,
        ClassJobCategoryId.THM_BLM,
        ClassJobCategoryId.ACN_SMN,
        ClassJobCategoryId.ROG_NIN,
    ];
    private static readonly ClassJobCategoryId[] ParenthesesNameCategories =
    [
        ClassJobCategoryId.Tank,
        ClassJobCategoryId.Healer,
        ClassJobCategoryId.MeleeDps,
        ClassJobCategoryId.PhysicalRdps,
        ClassJobCategoryId.MagicalRdps,
    ];
    private static Dictionary<ClassJobCategoryId, Dictionary<uint, bool>>? _activationConditions;
    private static Dictionary<ClassJobCategoryId, string>? _displayNames;

    private static bool Initialized => _activationConditions is not null && _displayNames is not null;

    public static void Initialize(Plugin plugin)
    {
        var sheet = plugin.DataManager.GetExcelSheet<RawRow>(name: "ClassJobCategory");

        _activationConditions = new Dictionary<ClassJobCategoryId, Dictionary<uint, bool>>();
        _displayNames = new Dictionary<ClassJobCategoryId, string>();

        var classJobSheet = plugin.DataManager.GetExcelSheet<ClassJob>();
        var classJobIds = classJobSheet.Select(j => j.RowId).ToList();

        foreach (var cat in Enum.GetValues<ClassJobCategoryId>()) {
            // Display name
            _displayNames[cat] = cat.DisplayName(plugin);

            // Activation conditions
            _activationConditions[cat] = cat.IsActivatedAll(sheet, classJobIds);
        }

        foreach (var classJobId in classJobIds) {
            var classJob = classJobSheet.GetRow(classJobId);

            // Handle special category for base classes
            if (classJob.JobIndex == 0) {
                _activationConditions[ClassJobCategoryId.BaseClasses][classJobId] = true;
            }

            // Add base classes to roles
            switch (classJob.LimitBreak1.RowId) {
                case 197:
                    _activationConditions[ClassJobCategoryId.Tank][classJobId] = true;
                    break;
                case 206:
                    _activationConditions[ClassJobCategoryId.Healer][classJobId] = true;
                    break;
                case 200:
                    _activationConditions[ClassJobCategoryId.MeleeDps][classJobId] = true;
                    break;
                case 4238:
                    _activationConditions[ClassJobCategoryId.PhysicalRdps][classJobId] = true;
                    break;
                case 203:
                    _activationConditions[ClassJobCategoryId.MagicalRdps][classJobId] = true;
                    break;
            }
        }

        // Sanity check, make sure the groupings list has all the categories.
        if (!ClassJobCategoryGroupings.SelectMany(x => x).ToList().ToHashSet().SetEquals(
                new HashSet<ClassJobCategoryId>(Enum.GetValues<ClassJobCategoryId>()))) {
            throw new ApplicationException("Job category lists do not match");
        }
    }

    public static string DisplayName(this ClassJobCategoryId cat, Plugin plugin)
    {
        if (!Initialized)
            throw new InvalidOperationException("call `Initialize` first");

        if (Initialized && _displayNames!.TryGetValue(cat, out var name))
            return name;

        if (cat < 0) {
            return cat switch
            {
                ClassJobCategoryId.BaseClasses => "Base classes",
                _ => nameof(cat),
            };
        }

        var row = plugin.DataManager.GetExcelSheet<ClassJobCategory>()!.GetRow((uint)cat)!;

        if (ClassJobCombos.Contains(cat)) {
            var nameSplit = row.Name.ToString().Split(' ');
            if (nameSplit.Length != 2)
                // IDK what langauge, bail out
                return row.Name.ExtractText();
            return $"{nameSplit[1]}/{nameSplit[0]}";
        } else if (cat is ClassJobCategoryId.MIN_BTN) {
            var nameSplit = row.Name.ToString().Split(", ");
            if (nameSplit.Length != 2)
                return row.Name.ExtractText();
            return $"{nameSplit[0]}/{nameSplit[1]}";
        }

        // Try to remove parenthesized text, as in "(excluding limited jobs)"
        if (ParenthesesNameCategories.Contains(cat)) {
            var nameSplit = row.Name.ToString().Split("(");
            if (nameSplit.Length != 2)
                return row.Name.ExtractText();
            return nameSplit[0].Trim();
        }

        // Apply custom corrections for English clients
        if (plugin.ClientState.ClientLanguage is ClientLanguage.English) {
            return cat switch
            {
                ClassJobCategoryId.DoW => "DoW",
                ClassJobCategoryId.DoM => "DoM",
                ClassJobCategoryId.DoL => "DoL",
                ClassJobCategoryId.DoH => "DoH",
                ClassJobCategoryId.CombatJobs => "DoW/DoM",
                ClassJobCategoryId.NonCombatJobs => "DoH/DoL",
                _ => row.Name.ExtractText(),
            };
        }

        return row.Name.ExtractText();
    }

    public static bool IsActivated(this ClassJobCategoryId cat, ClassJob classJob)
        => cat != 0 && (_activationConditions?[cat][classJob.RowId]
            ?? throw new InvalidOperationException("call `Initialize` first"));

    public static ClassJobCategoryId CategoryForClassJob(ClassJob classJob)
    {
        if (!Initialized)
            throw new InvalidOperationException("call `Initialize` first");

        if (classJob.ClassJobCategory.Value!.RowId is (uint)ClassJobCategoryId.DoH)
            return ClassJobCategoryId.DoH;
        if (classJob.RowId == Util.EnglishAbbreviationToJobId["BTN"] || classJob.RowId == Util.EnglishAbbreviationToJobId["MIN"])
            return ClassJobCategoryId.MIN_BTN;

        ClassJobCategoryId best = 0;

        var categoryOnly = false;
        foreach (var cat in Enum.GetValues<ClassJobCategoryId>()) {
            if (_activationConditions![cat][classJob.RowId])
                if (ClassJobCombos.Contains(cat)) {
                    // This is a class-job combo
                    best = cat;
                    break;
                } else if (_activationConditions[cat].Count(kv => kv.Value) == 1) {
                    // This is the only true condition in the category
                    // if (categoryOnly)
                    //     PluginLog.Warning($"too many fitting categories discovered for {classJob.Abbreviation}");
                    best = cat;
                    categoryOnly = true;
                }

            // Prioritize the above matching style
            if (categoryOnly)
                continue;
        }

        if (best > 0)
            return best;

        throw new InvalidOperationException($"Unable to find ClassJobCategory for {classJob.Abbreviation}");
    }

    private static Dictionary<uint, bool> IsActivatedAll(
        this ClassJobCategoryId cat,
        ExcelSheet<RawRow> sheet,
        List<uint> classJobIds)
    {
        cat = cat switch
        {
            ClassJobCategoryId.BaseClasses => 0,
            _ => cat
        };

        Dictionary<uint, bool> res = new();

        foreach (var id in classJobIds) {
            res[id] = sheet.GetRow((uint)cat).ReadBool(sheet.Columns[(int)id + 1].Offset);
        }

        return res;
    }


    public static readonly List<List<ClassJobCategoryId>> ClassJobCategoryGroupings =
    [
        [
            ClassJobCategoryId.GLA_PLD,
            ClassJobCategoryId.MRD_WAR,
            ClassJobCategoryId.DRK,
            ClassJobCategoryId.GNB,
            ClassJobCategoryId.CNJ_WHM,
            ClassJobCategoryId.SCH,
            ClassJobCategoryId.AST,
            ClassJobCategoryId.SGE,
            ClassJobCategoryId.PGL_MNK,
            ClassJobCategoryId.LNC_DRG,
            ClassJobCategoryId.ROG_NIN,
            ClassJobCategoryId.SAM,
            ClassJobCategoryId.RPR,
            ClassJobCategoryId.VPR,
            ClassJobCategoryId.ARC_BRD,
            ClassJobCategoryId.MCH,
            ClassJobCategoryId.DNC,
            ClassJobCategoryId.THM_BLM,
            ClassJobCategoryId.ACN_SMN,
            ClassJobCategoryId.RDM,
            ClassJobCategoryId.PCT,
            ClassJobCategoryId.BLU,
        ],
        [
            ClassJobCategoryId.MIN_BTN,
            ClassJobCategoryId.FSH,
        ],
        [
            ClassJobCategoryId.DoW,
            ClassJobCategoryId.DoM,
            ClassJobCategoryId.DoL,
            ClassJobCategoryId.DoH,
            ClassJobCategoryId.CombatJobs,
            ClassJobCategoryId.NonCombatJobs,
            ClassJobCategoryId.Tank,
            ClassJobCategoryId.Healer,
            ClassJobCategoryId.MeleeDps,
            ClassJobCategoryId.PhysicalRdps,
            ClassJobCategoryId.MagicalRdps,
            ClassJobCategoryId.BaseClasses,
        ],
    ];
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ClassJobCategoryId
{
    GLA_PLD = 38,
    PGL_MNK = 41,
    MRD_WAR = 44,
    LNC_DRG = 47,
    ARC_BRD = 50,
    CNJ_WHM = 53,
    THM_BLM = 55,
    ACN_SMN = 69,
    ROG_NIN = 93,
    SCH = 29,
    MCH = 96,
    DRK = 98,
    AST = 99,
    SAM = 111,
    RDM = 112,
    GNB = 149,
    DNC = 150,
    RPR = 180,
    SGE = 181,
    VPR = 196,
    PCT = 197,
    BLU = 129,

    MIN_BTN = 154,
    FSH = 155,

    DoW = 30,
    DoM = 31,
    DoH = 33,
    DoL = 32,
    CombatJobs = 34,
    NonCombatJobs = 35,
    Tank = 156,
    Healer = 157,
    MeleeDps = 188,
    PhysicalRdps = 189,
    MagicalRdps = 159,

    BaseClasses = -1,
}
