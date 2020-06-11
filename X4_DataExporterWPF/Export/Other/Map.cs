using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LibX4.FileSystem;
using LibX4.Lang;

namespace X4_DataExporterWPF.Export
{
    class Map : IExport
    {
        /// <summary>
        /// マップ情報xml
        /// </summary>
        private readonly XDocument _MapXml;


        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="catFile">catファイル</param>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public Map(CatFile catFile, LangageResolver resolver)
        {
            _MapXml = catFile.OpenXml("libraries/mapdefaults.xml");
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
CREATE TABLE IF NOT EXISTS Map
(
    Macro       TEXT    NOT NULL PRIMARY KEY,
    Name        TEXT    NOT NULL,
    Description TEXT    NOT NULL
)";
                cmd.ExecuteNonQuery();
            }



            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _MapXml.Root.XPathSelectElements("dataset[not(starts-with(@macro, 'demo'))]/properties/identification/../..").Select
                (
                    dataset =>
                    {
                        var macro = dataset.Attribute("macro").Value;

                        var id = dataset.XPathSelectElement("properties/identification");

                        return (
                            macro,
                            _Resolver.Resolve(id.Attribute("name").Value),
                            _Resolver.Resolve(id.Attribute("description").Value)
                        );
                    }
                );


                cmd.CommandText = "INSERT INTO Map (Macro, Name, Description) VALUES(@macro, @name, @description)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@macro", item.Item1);
                    cmd.Parameters.AddWithValue("@name", item.Item2);
                    cmd.Parameters.AddWithValue("@description", item.Item3);
                    cmd.ExecuteNonQuery();
                }
            }


            ///////////////
            // Index作成 //
            ///////////////
            {
                cmd.CommandText = "CREATE INDEX MapIndex ON Ware(Macro)";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
