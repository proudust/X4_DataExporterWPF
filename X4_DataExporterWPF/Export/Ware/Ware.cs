using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LibX4.Lang;

namespace X4_DataExporterWPF.Export.Ware
{
    public class Ware : IExport
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
        public Ware(XDocument waresXml, LangageResolver resolver)
        {
            _WaresXml = waresXml;
            _Resolver = resolver;
        }


        /// <summary>
        /// データ抽出
        /// </summary>
        /// <param name="cmd"></param>
        public void Export(SQLiteCommand cmd)
        {
            //////////////////
            // テーブル作成 //
            //////////////////
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Ware
(
    WareID          TEXT    NOT NULL PRIMARY KEY,
    WareGroupID     TEXT    NOT NULL,
    TransportTypeID TEXT    NOT NULL,
    Name            TEXT    NOT NULL,
    Description     TEXT    NOT NULL,
    FactoryName     TEXT    NOT NULL,
    Volume          INTEGER NOT NULL,
    MinPrice        INTEGER NOT NULL,
    AvgPrice        INTEGER NOT NULL,
    MaxPrice        INTEGER NOT NULL,
    FOREIGN KEY (WareGroupID)       REFERENCES WareGroup(WareGroupID),
    FOREIGN KEY (TransportTypeID)   REFERENCES TransportType(TransportTypeID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }

            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[contains(@tags, 'economy')]").Select
                (x =>
                {
                    var price = x.Element("price");

                    return (
                        x.Attribute("id")?.Value,
                        x.Attribute("group")?.Value,
                        x.Attribute("transport")?.Value,
                        _Resolver.Resolve(x.Attribute("name")?.Value ?? ""),
                        _Resolver.Resolve(x.Attribute("description")?.Value ?? ""),
                        _Resolver.Resolve(x.Attribute("factoryname")?.Value ?? ""),
                        int.Parse(x.Attribute("volume")?.Value ?? "0"),
                        int.Parse(price.Attribute("min")?.Value ?? "0"),
                        int.Parse(price.Attribute("average")?.Value ?? "0"),
                        int.Parse(price.Attribute("max")?.Value ?? "0")
                    );
                })
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2) &&
                         !string.IsNullOrEmpty(x.Item3) &&
                         !string.IsNullOrEmpty(x.Item4)
                );

                cmd.CommandText = "INSERT INTO Ware (WareID, WareGroupID, TransportTypeID, Name, Description, FactoryName, Volume, MinPrice, AvgPrice, MaxPrice) VALUES(@wareID, @wareGroupID, @transportTypeID, @name, @description, @factoryName, @volume, @minPrice, @avgPrice, @maxPrice)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue("@wareID",          item.Item1);
                    cmd.Parameters.AddWithValue("@wareGroupID",     item.Item2);
                    cmd.Parameters.AddWithValue("@transportTypeID", item.Item3);
                    cmd.Parameters.AddWithValue("@name",            item.Item4);
                    cmd.Parameters.AddWithValue("@description",     item.Item5);
                    cmd.Parameters.AddWithValue("@factoryName",     item.Item6);
                    cmd.Parameters.AddWithValue("@volume",          item.Item7);
                    cmd.Parameters.AddWithValue("@minPrice",        item.Item8);
                    cmd.Parameters.AddWithValue("@avgPrice",        item.Item9);
                    cmd.Parameters.AddWithValue("@maxPrice",        item.Item10);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
