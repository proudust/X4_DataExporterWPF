using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LibX4.FileSystem;
using LibX4.Lang;

namespace X4_DataExporterWPF.Export.Equipment
{
    /// <summary>
    /// 装備情報抽出用クラス
    /// </summary>
    class Equipment : IExport
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
        public Equipment(CatFile catFile, XDocument waresXml, LangageResolver resolver)
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
CREATE TABLE IF NOT EXISTS Equipment
(
    EquipmentID     TEXT    NOT NULL PRIMARY KEY,
    MacroName       TEXT    NOT NULL,
    EquipmentTypeID TEXT    NOT NULL,
    SizeID          TEXT    NOT NULL,
    Name            TEXT    NOT NULL,
    FOREIGN KEY (EquipmentTypeID)   REFERENCES EquipmentType(EquipmentTypeID),
    FOREIGN KEY (SizeID)            REFERENCES Size(SizeID)
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                var items = _WaresXml.Root.XPathSelectElements("ware[@transport='equipment']").Select
                (
                    equipment => GetRecord(equipment)
                )
                .Where
                (
                    x => !string.IsNullOrEmpty(x.Item1) &&
                         !string.IsNullOrEmpty(x.Item2) &&
                         !string.IsNullOrEmpty(x.Item3) &&
                         !string.IsNullOrEmpty(x.Item4)
                );


                cmd.CommandText = "INSERT INTO Equipment (EquipmentID, MacroName, EquipmentTypeID, SizeID, Name) values (@equipmentID, @macroName, @equipmentTypeID, @sizeID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@equipmentID",     item.Item1);
                    cmd.Parameters.AddWithValue("@macroName",       item.Item2);
                    cmd.Parameters.AddWithValue("@equipmentTypeID", item.Item3);
                    cmd.Parameters.AddWithValue("@sizeID",          item.Item4);
                    cmd.Parameters.AddWithValue("@name",            item.Item5);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// 1レコード分の情報を抽出する
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        private (string, string, string, string, string) GetRecord(XElement equipment)
        {
            try
            {
                var macroName = equipment.XPathSelectElement("component").Attribute("ref")?.Value;
                var macroXml = _CatFile.OpenIndexXml("index/macros.xml", macroName);
                var componentXml = _CatFile.OpenIndexXml("index/components.xml", macroXml.Root.XPathSelectElement("macro/component").Attribute("ref").Value);

                // 装備が記載されているタグを取得する
                var component = componentXml.Root.XPathSelectElement("component/connections/connection[contains(@tags, 'component')]");

                // サイズ一覧
                string[] sizes = { "extrasmall", "small", "medium", "large", "extralarge" };

                // 一致するサイズを探す
                var tags = component?.Attribute("tags").Value.Split(" ");
                var size = sizes.Where(x => tags?.Contains(x) == true).FirstOrDefault();
                if (string.IsNullOrEmpty(size))
                {
                    // 一致するサイズがなかった場合
                    return ("", "", "", "", "");
                }

                var name = _Resolver.Resolve(equipment.Attribute("name").Value);

                return (
                    equipment.Attribute("id").Value,
                    macroName,
                    equipment.Attribute("group").Value,
                    size,
                    string.IsNullOrEmpty(name) ? macroName : name
                );
            }
            catch
            {
                return ("", "", "", "", "");
            }
        }
    }
}
