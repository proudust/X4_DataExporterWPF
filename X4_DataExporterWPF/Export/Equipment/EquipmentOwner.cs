using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace X4_DataExporterWPF.Export.Equipment
{
    /// <summary>
    /// 装備保有派閥抽出用クラス
    /// </summary>
    class EquipmentOwner : IExport
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="xml">ウェア情報xml</param>
        public EquipmentOwner(XDocument xml)
        {
            _WaresXml = xml;
        }


        /// <summary>
        /// 抽出処理
        /// </summary>
        /// <param name="cmd"></param>
        public void Export(SQLiteCommand cmd)
        {
            //////////////////
            // テーブル作成 //
            //////////////////
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS EquipmentOwner
(
    EquipmentID TEXT    NOT NULL,
    FactionID   TEXT    NOT NULL,
    PRIMARY KEY (EquipmentID, FactionID),
    FOREIGN KEY (EquipmentID)   REFERENCES Equipment(EquipmentID),
    FOREIGN KEY (FactionID)     REFERENCES Faction(FactionID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@transport='equipment']").SelectMany
                (
                    equipment => equipment.XPathSelectElements("owner").Select
                    (
                        owner =>
                        (
                            equipment.Attribute("id")?.Value,
                            owner.Attribute("faction")?.Value
                        )
                    )
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2)
                );

                cmd.CommandText = "INSERT INTO EquipmentOwner (EquipmentID, FactionID) values (@equipmentID, @factionID)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@equipmentID", item.Item1);
                    cmd.Parameters.AddWithValue("@factionID", item.Item2);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
