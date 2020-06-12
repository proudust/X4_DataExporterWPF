using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// ウェア生産に必要な情報抽出用クラス
    /// </summary>
    public class WareResourceExporter : IExporter
    {
        /// <summary>
        /// 情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        public WareResourceExporter(XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS WareResource
(
    WareID      TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    NeedWareID  TEXT    NOT NULL,
    Amount      INTEGER NOT NULL,
    PRIMARY KEY (WareID, Method, NeedWareID),
    FOREIGN KEY (WareID)        REFERENCES Ware(WareID),
    FOREIGN KEY (NeedWareID)    REFERENCES Ware(WareID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[contains(@tags, 'economy')]").SelectMany
                (
                    ware => ware.XPathSelectElements("production").SelectMany
                    (
                        prod => prod.XPathSelectElements("primary/ware").Select
                        (
                            needWare =>
                            {
                                var wareID = ware.Attribute("id")?.Value;
                                if (string.IsNullOrEmpty(wareID)) return null;

                                var method = prod.Attribute("method")?.Value;
                                if (string.IsNullOrEmpty(method)) return null;

                                var needWareID = needWare.Attribute("ware")?.Value;
                                if (string.IsNullOrEmpty(needWareID)) return null;

                                var amount = int.Parse(needWare.Attribute("amount")?.Value ?? "0");
                                return new WareResource(wareID, method, needWareID, amount);
                            }
                        )
                    )
                )
                .Where
                (
                    x => x != null
                );

                cmd.CommandText = "INSERT INTO WareResource (WareID, Method, NeedWareID, Amount) values (@wareID, @method, @needWareID, @amount)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@wareID",      item.WareID);
                    cmd.Parameters.AddWithValue("@method",      item.Method);
                    cmd.Parameters.AddWithValue("@needWareID",  item.NeedWareID);
                    cmd.Parameters.AddWithValue("@amount",      item.Amount);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
