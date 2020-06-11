using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LibX4.FileSystem;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// モジュールのタレット情報抽出用クラス
    /// </summary>
    public class ModuleTurret : IExport
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
        public ModuleTurret(CatFile catFile, XDocument waresXml)
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
CREATE TABLE IF NOT EXISTS ModuleTurret
(
    ModuleID    TEXT    NOT NULL,
    SizeID      TEXT    NOT NULL,
    Amount      INTEGER NOT NULL,
    PRIMARY KEY (ModuleID, SizeID),
    FOREIGN KEY (ModuleID)  REFERENCES Module(ModuleID),
    FOREIGN KEY (SizeID)    REFERENCES Size(SizeID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@tags='module']").SelectMany
                (
                    module => GetRecords(module)
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2)
                );


                cmd.CommandText = "INSERT INTO ModuleTurret (ModuleID, SizeID, Amount) values (@moduleID, @sizeID, @amount)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@moduleID", item.Item1);
                    cmd.Parameters.AddWithValue("@sizeID",   item.Item2);
                    cmd.Parameters.AddWithValue("@amount",   item.Item3);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// 情報抽出
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        private IEnumerable<(string, string, int)> GetRecords(XElement module)
        {
            try
            {
                var macroName = module.XPathSelectElement("component").Attribute("ref").Value;
                var macroXml = _CatFile.OpenIndexXml("index/macros.xml", macroName);
                var componentXml = _CatFile.OpenIndexXml("index/components.xml", macroXml.Root.XPathSelectElement("macro/component").Attribute("ref").Value);

                // 装備集計用辞書
                var sizeDict = new Dictionary<string, int>()
                {
                    { "extrasmall", 0 },
                    { "small",      0 },
                    { "medium",     0 },
                    { "large",      0 },
                    { "extralarge", 0 }
                };


                // タレットが記載されているタグを取得する
                foreach (var connections in componentXml.Root.XPathSelectElements("component/connections/connection[contains(@tags, 'turret')]"))
                {
                    // タレットのサイズを取得
                    var attr = connections.Attribute("tags").Value;
                    var size = sizeDict.Keys.Where(x => attr.Contains(x)).FirstOrDefault();

                    if (string.IsNullOrEmpty(size))
                    {
                        continue;
                    }

                    sizeDict[size]++;
                }

                return sizeDict.Where
                (
                    x => 0 < x.Value
                )
                .Select
                (
                    x =>
                    (
                        module.Attribute("id").Value,
                        x.Key,
                        x.Value
                    )
                );
            }
            catch
            {
                return Enumerable.Empty<(string, string, int)>();
            }
        }
    }
}
