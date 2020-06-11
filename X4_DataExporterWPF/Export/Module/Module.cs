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
    /// モジュール情報抽出用クラス
    /// </summary>
    public class Module : IExport
    {
        /// <summary>
        /// catファイルオブジェクト
        /// </summary>
        private readonly CatFile _CatFile;

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
        /// <param name="catFile">catファイルオブジェクト</param>
        /// <param name="waresXml">ウェア情報xml</param>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public Module(CatFile catFile, XDocument waresXml, LangageResolver resolver)
        {
            _CatFile = catFile;
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
CREATE TABLE IF NOT EXISTS Module
(
    ModuleID        TEXT    NOT NULL PRIMARY KEY,
    ModuleTypeID    TEXT    NOT NULL,
    Name            TEXT    NOT NULL,
    Macro           TEXT    NOT NULL,
    MaxWorkers      INTEGER NOT NULL,
    WorkersCapacity INTEGER NOT NULL,
    FOREIGN KEY (ModuleTypeID)  REFERENCES ModuleType(ModuleTypeID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@tags='module']").Select
                (
                    module => GetRecord(module)
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2) &&
                         !string.IsNullOrEmpty(x.Item3) &&
                         !string.IsNullOrEmpty(x.Item4)
                );

                cmd.CommandText = "INSERT INTO Module (ModuleID, ModuleTypeID, Name, Macro, MaxWorkers, WorkersCapacity) values (@moduleID, @moduleTypeID, @name, @macro, @maxWorkers, @workersCapacity)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@moduleID",        item.Item1);
                    cmd.Parameters.AddWithValue("@moduleTypeID",    item.Item2);
                    cmd.Parameters.AddWithValue("@name",            item.Item3);
                    cmd.Parameters.AddWithValue("@macro",           item.Item4);
                    cmd.Parameters.AddWithValue("@maxWorkers",      item.Item5);
                    cmd.Parameters.AddWithValue("@workersCapacity", item.Item6);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// 1レコード分の情報抽出
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private (string, string, string, string, int, int) GetRecord(XElement module)
        {
            try
            {
                var macroName = module.XPathSelectElement("component").Attribute("ref").Value;
                var macroXml = _CatFile.OpenIndexXml("index/macros.xml", macroName);


                // 従業員数/最大収容人数取得
                var workForce = macroXml?.Root?.XPathSelectElement("macro/properties/workforce");
                var maxWorkers = int.Parse(workForce?.Attribute("max")?.Value ?? "0");
                var capacity = int.Parse(workForce?.Attribute("capacity")?.Value ?? "0");

                var name = _Resolver.Resolve(module.Attribute("name")?.Value ?? "");

                return (
                    module.Attribute("id").Value,
                    macroXml.Root.XPathSelectElement("macro").Attribute("class").Value,
                    string.IsNullOrEmpty(name) ? macroName : name,
                    macroName,
                    maxWorkers,
                    capacity
                );
            }
            catch
            {
                return ("", "", "", "", 0, 0);
            }
        }
    }
}
