using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LibX4.FileSystem;
using LibX4.Lang;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// 派閥情報抽出用クラス
    /// </summary>
    public class FactionExporter : IExporter
    {
        /// <summary>
        /// 派閥情報xml
        /// </summary>
        private readonly XDocument _FactionsXml;


        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="catFile">catファイル</param>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public FactionExporter(CatFile catFile, LangageResolver resolver)
        {
            _FactionsXml = catFile.OpenXml("libraries/factions.xml");

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
CREATE TABLE IF NOT EXISTS Faction
(
    FactionID   TEXT    NOT NULL PRIMARY KEY,
    Name        TEXT    NOT NULL,
    RaceID      TEXT    NOT NULL,
    ShortName   TEXT    NOT NULL,
    FOREIGN KEY (RaceID)   REFERENCES Race(RaceID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _FactionsXml.Root.XPathSelectElements("faction[@name]").Select
                (
                    x =>
                    (
                        x.Attribute("id")?.Value,
                        _Resolver.Resolve(x.Attribute("name")?.Value ?? ""),
                        x.Attribute("primaryrace")?.Value ?? "",
                        _Resolver.Resolve(x.Attribute("shortname")?.Value ?? "")
                    )
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2)
                );

                cmd.CommandText = "INSERT INTO Faction (FactionID, Name, RaceID, ShortName) values (@factionID, @name, @raceID, @shortName)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@factionID",   item.Item1);
                    cmd.Parameters.AddWithValue("@name",        item.Item2);
                    cmd.Parameters.AddWithValue("@raceID",      item.Item3);
                    cmd.Parameters.AddWithValue("@shortName",   item.Item4);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
