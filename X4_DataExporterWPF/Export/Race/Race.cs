using LibX4.FileSystem;
using LibX4.Lang;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.Data.SQLite;

namespace X4_DataExporterWPF.Export.Race
{
    /// <summary>
    /// 種族情報抽出用クラス
    /// </summary>
    public class Race : IExport
    {
        /// <summary>
        /// 種族情報xml
        /// </summary>
        private readonly XDocument _RaceXml;


        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="catFile">catファイル</param>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public Race(CatFile catFile, LangageResolver resolver)
        {
            _RaceXml = catFile.OpenXml("libraries/races.xml");

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
CREATE TABLE IF NOT EXISTS Race
(
    RaceID      TEXT    NOT NULL PRIMARY KEY,
    Name        TEXT    NOT NULL,
    ShortName   TEXT    NOT NULL
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _RaceXml.Root.Elements().Select
                (
                    x =>
                    (
                        x.Attribute("id")?.Value ?? "",
                        _Resolver.Resolve(x.Attribute("name")?.Value ?? ""),
                        _Resolver.Resolve(x.Attribute("shortname")?.Value ?? "")
                    )
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2)
                );

                cmd.CommandText = "INSERT INTO Race (RaceID, Name, ShortName) VALUES(@racdID, @name, @shortName)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@racdID",      item.Item1);
                    cmd.Parameters.AddWithValue("@name",        item.Item2);
                    cmd.Parameters.AddWithValue("@shortName",   item.Item3);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
