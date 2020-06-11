using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LibX4.FileSystem;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// モジュールの保管容量情報抽出用クラス
    /// </summary>
    class ModuleStorage : IExport
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
        public ModuleStorage(CatFile catFile, XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS ModuleStorage
(
    ModuleID        TEXT    NOT NULL,
    TransportTypeID TEXT    NOT NULL,
    Amount          INTEGER NOT NULL,
    PRIMARY KEY (ModuleID, TransportTypeID),
    FOREIGN KEY (ModuleID)          REFERENCES Module(ModuleID),
    FOREIGN KEY (TransportTypeID)   REFERENCES TransportType(TransportTypeID)
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
                         !string.IsNullOrEmpty(x.Item2)
                );

                cmd.CommandText = "INSERT INTO ModuleStorage (ModuleID, TransportTypeID, Amount) values (@moduleID, @transportTypeID, @amount)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@moduleID",        item.Item1);
                    cmd.Parameters.AddWithValue("@transportTypeID", item.Item2);
                    cmd.Parameters.AddWithValue("@amount",          item.Item3);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// 1レコード分の情報抽出
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private (string, string, int) GetRecord(XElement module)
        {
            try
            {
                var macroName = module.XPathSelectElement("component").Attribute("ref").Value;
                var macroXml = _CatFile.OpenIndexXml("index/macros.xml", macroName);

                // 容量が記載されている箇所を抽出
                var cargo = macroXml.Root.XPathSelectElement("macro/properties/cargo");

                // 総合保管庫は飛ばす
                var tags = cargo?.Attribute("tags")?.Value;
                if (tags?.Contains(' ') == true)
                {
                    return ("", "", 0);
                }

                return (
                    module.Attribute("id").Value,
                    tags,
                    int.Parse(cargo?.Attribute("max").Value)
                );
            }
            catch
            {
                return ("", "", 0);
            }
        }
    }
}
