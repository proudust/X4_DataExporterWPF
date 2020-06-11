using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;


namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// モジュール建造に関する情報抽出用クラス
    /// </summary>
    class ModuleProductionExporter : IExporter
    {
        /// <summary>
        /// ウェア情報xml
        /// </summary>
        private readonly XDocument _WaresXml;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="waresXml">ウェア情報xml</param>
        public ModuleProductionExporter(XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS ModuleProduction
(
    ModuleID    TEXT    NOT NULL,
    Method      TEXT    NOT NULL,
    Time        REAL    NOT NULL,
    PRIMARY KEY (ModuleID, Method),
    FOREIGN KEY (ModuleID)  REFERENCES Module(ModuleID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@tags='module']").SelectMany
                (
                    module => module.XPathSelectElements("production").Select
                    (
                        prod =>
                        (
                            module.Attribute("id")?.Value,
                            prod.Attribute("method")?.Value,
                            double.Parse(prod.Attribute("time")?.Value ?? "0.0")
                        )
                    )
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2)
                );


                cmd.CommandText = "INSERT INTO ModuleProduction (ModuleID, Method, Time) values (@moduleID, @method, @time)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@moduleID", item.Item1);
                    cmd.Parameters.AddWithValue("@method",   item.Item2);
                    cmd.Parameters.AddWithValue("@time",     item.Item3);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
