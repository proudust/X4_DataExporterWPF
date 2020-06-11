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
    /// ウェア種別抽出用クラス
    /// </summary>
    public class WareGroup : IExport
    {
        /// <summary>
        /// ウェア種別情報xml
        /// </summary>
        private readonly XDocument _WareGroupXml;


        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="catFile">catファイル</param>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public WareGroup(CatFile catFile, LangageResolver resolver)
        {
            _WareGroupXml = catFile.OpenXml("libraries/waregroups.xml");

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
CREATE TABLE IF NOT EXISTS WareGroup
(
    WareGroupID TEXT    NOT NULL PRIMARY KEY,
    Name        TEXT    NOT NULL,
    FactoryName TEXT    NOT NULL,
    Icon        TEXT    NOT NULL,
    Tier        INTEGER NOT NULL
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }

            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WareGroupXml.Root.XPathSelectElements("group[@tags='tradable']").Select
                (
                    x =>
                    (
                        x.Attribute("id")?.Value,
                        _Resolver.Resolve(x.Attribute("name")?.Value ?? ""),
                        _Resolver.Resolve(x.Attribute("factoryname")?.Value ?? ""),
                        x.Attribute("icon")?.Value ?? "",
                        int.Parse(x.Attribute("tier")?.Value ?? "0")
                    )
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2)
                );

                cmd.CommandText = "INSERT INTO WareGroup (WareGroupID, Name, FactoryName, Icon, Tier) values (@wareGroupID, @name, @factoryName, @icon, @tier)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@wareGroupID", item.Item1);
                    cmd.Parameters.AddWithValue("@name",        item.Item2);
                    cmd.Parameters.AddWithValue("@factoryName", item.Item3);
                    cmd.Parameters.AddWithValue("@icon",        item.Item4);
                    cmd.Parameters.AddWithValue("@tier",        item.Item5);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
