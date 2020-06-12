using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// 従業員が必要とするウェア情報抽出用クラス
    /// </summary>
    class WorkUnitResourceExporter : IExporter
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        public WorkUnitResourceExporter(XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS WorkUnitResource
(
    WorkUnitID  TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    WareID      TEXT    NOT NULL,
    Amount      INTEGER NOT NULL,
    PRIMARY KEY (WorkUnitID, Method, WareID),
    FOREIGN KEY (WareID)   REFERENCES Ware(WareID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@transport='workunit']").SelectMany
                (
                    workUnit => workUnit.XPathSelectElements("production").SelectMany
                    (
                        prod => prod.XPathSelectElements("primary/ware").Select
                        (
                            ware =>
                            {
                                var workUnitID = workUnit.Attribute("id")?.Value;
                                if (string.IsNullOrEmpty(workUnitID)) return null;

                                var method = prod.Attribute("method")?.Value;
                                if (string.IsNullOrEmpty(method)) return null;

                                var wareID = ware.Attribute("ware")?.Value;
                                if (string.IsNullOrEmpty(wareID)) return null;

                                var amount = int.Parse(ware.Attribute("amount")?.Value ?? "0");

                                return new WorkUnitResource(workUnitID, method, wareID, amount);
                            }
                        )
                    )
                )
                .Where
                (
                    x => x != null
                );

                cmd.CommandText = "INSERT INTO WorkUnitResource (WorkUnitID, Method, WareID, Amount) values (@workUnitID, @method, @wareID, @amount)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@workUnitID",  item.WorkUnitID);
                    cmd.Parameters.AddWithValue("@method",      item.Method);
                    cmd.Parameters.AddWithValue("@wareID",      item.WareID);
                    cmd.Parameters.AddWithValue("@amount",      item.Amount);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
