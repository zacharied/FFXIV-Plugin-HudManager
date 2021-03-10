using Lumina.Data;
using Lumina.Excel;
using Lumina.Text;

namespace HUD_Manager.Lumina {
    [Sheet("Hud")]
    public class HudSheet : IExcelRow {
        public uint RowId { get; set; }
        public uint SubRowId { get; set; }
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string ShorterName { get; set; } = null!;

        public void PopulateData(RowParser parser, global::Lumina.Lumina lumina, Language language) {
            this.RowId = parser.Row;
            this.SubRowId = parser.SubRow;
            this.Name = parser.ReadColumn<SeString>(0);
            this.ShortName = parser.ReadColumn<SeString>(1);
            this.ShorterName = parser.ReadColumn<SeString>(2);
        }
    }
}
