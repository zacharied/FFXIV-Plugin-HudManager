namespace HUD_Manager.Structs
{
    public enum WindowKind : uint
    {
        FreeCompany = 3769291431,
    }

    public static class WindowKindExt
    {
        public static readonly string[] All = {
            "AreaMap",
            "ChatLog",
            "ChatLogPanel_0",
            "ChatLogPanel_1",
            "ChatLogPanel_2",
            "ChatLogPanel_3",
            "InventoryExpansion",
            "InventoryLarge",
            "Inventory",
            "InventoryBuddy", // chocobo saddlebag
            "ArmouryBoard", // armoury chest
            "FreeCompany",
            "Character",
            "Currency",
            "ContentsInfo", // timers
            "ContentsFinder", // duty finder
            "RaidFinder", // raid finder
            "LookingForGroup", // party finder
            "Macro",
            "RecipeNote", // crafting log
            "GatheringNote", // gathering log
            "ActionMenu", // actions & traits
            "Achievement",
            "MountNoteBook", // mount guide
            "MinionNoteBook", // minion guide
            "OrnamentNoteBook", // fashion accessories
            "AOZNotebook", // blue magic spellbook
            "SystemMenu",
            "PvpProfile", // PvP Profile
            "GoldSaucerInfo",
            "Journal",
            "Teleport",
        };
    }
}
