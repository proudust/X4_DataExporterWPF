using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// 装備生産に必要なウェア情報抽出用クラス
    /// </summary>
    class EquipmentResource : IExport
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        public EquipmentResource(XDocument waresXml)
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
                            (
                                equipment.Attribute("id")?.Value ?? "",
                                prod.Attribute("method")?.Value ?? "",
                                ware.Attribute("ware")?.Value ?? "",
                                int.Parse(ware.Attribute("amount").Value ?? "0")
                            )
                        )
                    )
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2) &&
                         !string.IsNullOrEmpty(x.Item3)
                );


                cmd.CommandText = "INSERT INTO EquipmentResource (EquipmentID, Method, NeedWareID, Amount) values (@equipmentID, @method, @needWareID, @amount)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@equipmentID", item.Item1);
                    cmd.Parameters.AddWithValue("@method",      item.Item2);
                    cmd.Parameters.AddWithValue("@needWareID",  item.Item3);
                    cmd.Parameters.AddWithValue("@amount",      item.Item4);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
