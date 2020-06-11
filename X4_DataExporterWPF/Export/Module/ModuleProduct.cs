using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LibX4.FileSystem;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// モジュールの生産品情報抽出用クラス
    /// </summary>
    public class ModuleProduct : IExport
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
        /// コンストラクタ
        /// </summary>
        /// <param name="catFile">catファイルオブジェクト</param>
        /// <param name="waresXml">ウェア情報xml</param>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public ModuleProduct(CatFile catFile, XDocument waresXml)
        {
            _CatFile = catFile;
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
CREATE TABLE IF NOT EXISTS ModuleProduct
(
    ModuleID    TEXT    NOT NULL,
    WareID      TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    PRIMARY KEY (ModuleID, WareID, Method),
    FOREIGN KEY (ModuleID)  REFERENCES Module(ModuleID),
    FOREIGN KEY (WareID)    REFERENCES Ware(WareID)
)WITHOUT ROWID";
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
                         !string.IsNullOrEmpty(x.Item3)
                );

                cmd.CommandText = "INSERT INTO ModuleProduct (ModuleID, WareID, Method) values (@moduleID, @wareID, @method)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@moduleID", item.Item1);
                    cmd.Parameters.AddWithValue("@wareID",   item.Item2);
                    cmd.Parameters.AddWithValue("@method",   item.Item3);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// 1レコード分の情報抽出
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private (string, string, string) GetRecord(XElement module)
        {
            try
            {
                var macroName = module.XPathSelectElement("component").Attribute("ref").Value;
                var macroXml = _CatFile.OpenIndexXml("index/macros.xml", macroName);

                var prod = macroXml.Root.XPathSelectElement("macro/properties/production/queue");

                return (
                    module.Attribute("id").Value,
                    prod?.Attribute("ware").Value,
                    prod?.Attribute("method")?.Value ?? "default"
                );
            }
            catch
            {
                return ("", "", "");
            }
        }
    }
}
