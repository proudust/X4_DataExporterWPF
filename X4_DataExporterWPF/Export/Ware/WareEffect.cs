using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace X4_DataExporterWPF.Export.Ware
{
    /// <summary>
    /// ウェア生産時の追加効果情報抽出用クラス
    /// </summary>
    public class WareEffect : IExport
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public WareEffect(XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS WareEffect
(
    WareID      TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    EffectID    TEXT    NOT NULL,
    Product     REAL    NOT NULL,
    PRIMARY KEY (WareID, Method, EffectID),
    FOREIGN KEY (WareID)    REFERENCES Ware(WareID),
    FOREIGN KEY (EffectID)  REFERENCES Effect(EffectID)
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
                        prod => prod.XPathSelectElements("effects/effect").Select
                        (
                            effect =>
                            (
                                ware.Attribute("id")?.Value,
                                prod.Attribute("method")?.Value,
                                effect.Attribute("type")?.Value,
                                double.Parse(effect.Attribute("product")?.Value ?? "0.0")
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

                cmd.CommandText = "INSERT INTO WareEffect (WareID, Method, EffectID, Product) values (@wareID, @method, @effectID, @product)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@wareID",      item.Item1);
                    cmd.Parameters.AddWithValue("@method",      item.Item2);
                    cmd.Parameters.AddWithValue("@effectID",    item.Item3);
                    cmd.Parameters.AddWithValue("@product",     item.Item4);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
