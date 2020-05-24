using LibX4.Lang;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace X4_DataExporterWPF.Export.Ware
{
    /// <summary>
    /// ウェア生産時の情報抽出用クラス
    /// </summary>
    public class WareProduction : IExport
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public WareProduction(XDocument waresXml, LangageResolver resolver)
        {
            _WaresXml = waresXml;
            _Resolver = resolver;
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
CREATE TABLE IF NOT EXISTS WareProduction
(
    WareID  TEXT    NOT NULL,
    Method  TEXT    NOT NULL,
    Name    TEXT    NOT NULL,
    Amount  INTEGER NOT NULL,
    Time    REAL    NOT NULL,
    FOREIGN KEY (WareID)   REFERENCES Ware(WareID)
)";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[contains(@tags, 'economy')]").SelectMany
                (
                    ware => ware.XPathSelectElements("production").Select
                    (
                        prod =>
                        (
                            ware.Attribute("id")?.Value,
                            prod.Attribute("method")?.Value,
                            _Resolver.Resolve(prod.Attribute("name")?.Value ?? ""),
                            int.Parse(prod.Attribute("amount")?.Value ?? "0"),
                            double.Parse(prod.Attribute("time")?.Value ?? "0.0")
                        )
                    )
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2)
                );

                cmd.CommandText = "INSERT INTO WareProduction (WareID, Method, Name, Amount, Time) values (@wareID, @method, @name, @amount, @time)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@wareID",  item.Item1);
                    cmd.Parameters.AddWithValue("@method",  item.Item2);
                    cmd.Parameters.AddWithValue("@name",    item.Item3);
                    cmd.Parameters.AddWithValue("@amount",  item.Item4);
                    cmd.Parameters.AddWithValue("@time",    item.Item5);

                    cmd.ExecuteNonQuery();
                }
            }


            ///////////////
            // Index作成 //
            ///////////////
            {
                cmd.CommandText = "CREATE INDEX WareProductionIndex ON WareProduction(WareID, Method)";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
