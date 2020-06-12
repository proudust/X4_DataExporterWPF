using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// 装備生産に必要なウェア情報抽出用クラス
    /// </summary>
    class EquipmentResourceExporter : IExporter
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        public EquipmentResourceExporter(XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS EquipmentResource
(
    EquipmentID TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    NeedWareID  TEXT    NOT NULL,
    Amount      INTEGER NOT NULL,
    PRIMARY KEY (EquipmentID, Method, NeedWareID),
    FOREIGN KEY (EquipmentID)   REFERENCES Equipment(EquipmentID),
    FOREIGN KEY (NeedWareID)    REFERENCES Ware(WareID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@transport='equipment']").SelectMany
                (
                    equipment => equipment.XPathSelectElements("production").SelectMany
                    (
                        prod => prod.XPathSelectElements("primary/ware").Select
                        (
                            ware =>
                            {
                                var equipmentID = equipment.Attribute("id")?.Value;
                                if (string.IsNullOrEmpty(equipmentID)) return null;
                                var method = prod.Attribute("method")?.Value;
                                if (string.IsNullOrEmpty(method)) return null;
                                var needWareID = ware.Attribute("ware")?.Value;
                                if (string.IsNullOrEmpty(needWareID)) return null;
                                var amount = int.Parse(ware.Attribute("amount").Value ?? "0");
                                return new EquipmentResource(equipmentID, method, needWareID, amount);
                            }
                        )
                    )
                )
                .Where
                (
                    x => x != null
                );


                cmd.CommandText = "INSERT INTO EquipmentResource (EquipmentID, Method, NeedWareID, Amount) values (@equipmentID, @method, @needWareID, @amount)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@equipmentID", item.EquipmentID);
                    cmd.Parameters.AddWithValue("@method",      item.Method);
                    cmd.Parameters.AddWithValue("@needWareID",  item.NeedWareID);
                    cmd.Parameters.AddWithValue("@amount",      item.Amount);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
