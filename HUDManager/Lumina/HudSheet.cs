using Lumina.Data;
using Lumina.Excel;
using Lumina.Text;

namespace HUD_Manager.Lumina
{
    [Sheet("Hud")]
    public class HudSheet : ExcelRow
    {
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string ShorterName { get; set; } = null!;

        public override void PopulateData(RowParser parser, global::Lumina.GameData lumina, Language language)
        {
            this.RowId = parser.RowId;
            this.SubRowId = parser.SubRowId;
            this.Name = parser.ReadColumn<SeString>(0);
            this.ShortName = parser.ReadColumn<SeString>(1);
            this.ShorterName = parser.ReadColumn<SeString>(2);

            this.SheetLanguage = language;
            this.SheetName = parser.Sheet.Name;
        }
    }
}
