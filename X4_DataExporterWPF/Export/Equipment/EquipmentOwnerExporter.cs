using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// 装備保有派閥抽出用クラス
    /// </summary>
    class EquipmentOwnerExporter : IExporter
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="xml">ウェア情報xml</param>
        public EquipmentOwnerExporter(XDocument xml)
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
                        {
                            var equipmentID = equipment.Attribute("id")?.Value;
                            if (string.IsNullOrEmpty(equipmentID)) return null;
                            var factionID = owner.Attribute("faction")?.Value;
                            if (string.IsNullOrEmpty(factionID)) return null;
                            return new EquipmentOwner(equipmentID,factionID);
                        }
                    )
                )
                .Where
                (
                    x => x != null
                );

                cmd.CommandText = "INSERT INTO EquipmentOwner (EquipmentID, FactionID) values (@equipmentID, @factionID)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@equipmentID", item.EquipmentID);
                    cmd.Parameters.AddWithValue("@factionID", item.FactionID);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
