using LibX4.FileSystem;
using LibX4.Lang;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// 従業員用生産情報抽出用クラス
    /// </summary>
    class WorkUnitProduction : IExport
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        public WorkUnitProduction(XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS WorkUnitProduction
(
    WorkUnitID  TEXT    NOT NULL,
    Time        INTEGER NOT NULL,
    Amount      INTEGER NOT NULL,
    Method      TEXT    NOT NULL,
    PRIMARY KEY (WorkUnitID, Method)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@transport='workunit']").SelectMany
                (
                    workUnit => workUnit.XPathSelectElements("production").Select
                    (
                        prod => 
                        (
                            workUnit.Attribute("id")?.Value,
                            int.Parse(prod.Attribute("time")?.Value ?? "0"),
                            int.Parse(prod.Attribute("amount")?.Value ?? "0"),
                            prod.Attribute("method")?.Value
                        )
                    )
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item4)
                );

                cmd.CommandText = "INSERT INTO WorkUnitProduction (WorkUnitID, Time, Amount, Method) values (@workUnitID, @time, @amount, @method)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@workUnitID",  item.Item1);
                    cmd.Parameters.AddWithValue("@time",        item.Item2);
                    cmd.Parameters.AddWithValue("@amount",      item.Item3);
                    cmd.Parameters.AddWithValue("@method",      item.Item4);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
