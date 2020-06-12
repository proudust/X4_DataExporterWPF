using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// 装備作成時の情報抽出用クラス
    /// </summary>
    class EquipmentProductionExporter : IExporter
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        public EquipmentProductionExporter(XDocument waresXml)
        {
            _WaresXml = waresXml;
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
CREATE TABLE IF NOT EXISTS EquipmentProduction
(
    EquipmentID TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    Time        REAL    NOT NULL,
    PRIMARY KEY (EquipmentID, Method),
    FOREIGN KEY (EquipmentID)   REFERENCES Equipment(EquipmentID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@transport='equipment']").SelectMany
                (
                    equipment => equipment.XPathSelectElements("production").Select
                    (
                        prod =>
                        {
                            var equipmentID = equipment.Attribute("id")?.Value;
                            if (string.IsNullOrEmpty(equipmentID)) return null;
                            var method = prod.Attribute("method")?.Value;
                            if (string.IsNullOrEmpty(method)) return null;
                            var time = double.Parse(prod.Attribute("time")?.Value ?? "0.0");
                            return new EquipmentProduction(equipmentID, method, time);
                        }
                    )
                )
                .Where
                (
                    x => x != null
                );

                cmd.CommandText = "INSERT INTO EquipmentProduction (EquipmentID, Method, Time) values (@equipmentID, @method, @time)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@equipmentID", item.EquipmentID);
                    cmd.Parameters.AddWithValue("@method",      item.Method);
                    cmd.Parameters.AddWithValue("@time",        item.Time);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
