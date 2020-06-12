using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LibX4.Lang;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    public class WareExporter : IExporter
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
        public WareExporter(XDocument waresXml, LangageResolver resolver)
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
                    var wareID = x.Attribute("id")?.Value;
                    if (string.IsNullOrEmpty(wareID)) return null;

                    var wareGroupID = x.Attribute("group")?.Value;
                    if (string.IsNullOrEmpty(wareGroupID)) return null;

                    var transportTypeID = x.Attribute("transport")?.Value;
                    if (string.IsNullOrEmpty(transportTypeID)) return null;

                    var name = _Resolver.Resolve(x.Attribute("name")?.Value ?? "");
                    if (string.IsNullOrEmpty(name)) return null;

                    var description = _Resolver.Resolve(x.Attribute("description")?.Value ?? "");
                    var factoryName = _Resolver.Resolve(x.Attribute("factoryname")?.Value ?? "");
                    var volume = int.Parse(x.Attribute("volume")?.Value ?? "0");

                    var price = x.Element("price");
                    var minPrice = int.Parse(price.Attribute("min")?.Value ?? "0");
                    var avgPrice = int.Parse(price.Attribute("average")?.Value ?? "0");
                    var maxPrice = int.Parse(price.Attribute("max")?.Value ?? "0");

                    return new Ware(wareID, wareGroupID, transportTypeID, name, description, factoryName, volume, minPrice, avgPrice, maxPrice);
                })
                .Where
                (
                    x => x != null
                );

                cmd.CommandText = "INSERT INTO Ware (WareID, WareGroupID, TransportTypeID, Name, Description, FactoryName, Volume, MinPrice, AvgPrice, MaxPrice) VALUES(@wareID, @wareGroupID, @transportTypeID, @name, @description, @factoryName, @volume, @minPrice, @avgPrice, @maxPrice)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();

                    cmd.Parameters.AddWithValue("@wareID",          item.WareID);
                    cmd.Parameters.AddWithValue("@wareGroupID",     item.WareGroupID);
                    cmd.Parameters.AddWithValue("@transportTypeID", item.TransportTypeID);
                    cmd.Parameters.AddWithValue("@name",            item.Name);
                    cmd.Parameters.AddWithValue("@description",     item.Description);
                    cmd.Parameters.AddWithValue("@factoryName",     item.FactoryName);
                    cmd.Parameters.AddWithValue("@volume",          item.Volume);
                    cmd.Parameters.AddWithValue("@minPrice",        item.MinPrice);
                    cmd.Parameters.AddWithValue("@avgPrice",        item.AvgPrice);
                    cmd.Parameters.AddWithValue("@maxPrice",        item.MaxPrice);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
