using LibX4.FileSystem;
using LibX4.Lang;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;


namespace X4_DataExporterWPF.Export.Module
{
    /// <summary>
    /// モジュール建造に必要なウェア情報抽出用クラス
    /// </summary>
    public class ModuleResource : IExport
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        public ModuleResource(XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS ModuleResource
(
    ModuleID    TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    WareID      TEXT    NOT NULL,
    Amount      INTEGER NOT NULL,
    PRIMARY KEY (ModuleID, Method, WareID),
    FOREIGN KEY (ModuleID)  REFERENCES Module(ModuleID),
    FOREIGN KEY (WareID)    REFERENCES Ware(WareID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@tags='module']").SelectMany
                (
                    module => module.XPathSelectElements("production").SelectMany
                    (
                        prod => prod.XPathSelectElements("primary/ware").Select
                        (
                            ware =>
                            (
                                module.Attribute("id")?.Value,
                                prod.Attribute("method")?.Value,
                                ware.Attribute("ware")?.Value,
                                int.Parse(ware.Attribute("amount")?.Value ?? "0")
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

                cmd.CommandText = "INSERT INTO ModuleResource (ModuleID, Method, WareID, Amount) values (@moduleID, @method, @wareID, @amount)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@moduleID", item.Item1);
                    cmd.Parameters.AddWithValue("@method",   item.Item2);
                    cmd.Parameters.AddWithValue("@wareID",   item.Item3);
                    cmd.Parameters.AddWithValue("@amount",   item.Item4);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
