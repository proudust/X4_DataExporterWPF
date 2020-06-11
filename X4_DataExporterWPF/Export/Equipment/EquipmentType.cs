using LibX4.FileSystem;
using LibX4.Lang;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace X4_DataExporterWPF.Export.Equipment
{
    /// <summary>
    /// 装備種別情報抽出用クラス
    /// </summary>
    public class EquipmentType : IExport
    {
        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public EquipmentType(LangageResolver resolver)
        {
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
CREATE TABLE IF NOT EXISTS EquipmentType
(
    EquipmentTypeID TEXT    NOT NULL PRIMARY KEY,
    Name            TEXT    NOT NULL
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                // TODO: 可能ならファイルから抽出する
                (string, string)[] items =
                {
                    ("countermeasures", "{20215, 1701}"),
                    ("drones",          "{20215, 1601}"),
                    ("engines",         "{20215, 1801}"),
                    ("missiles",        "{20215, 1901}"),
                    ("shields",         "{20215, 2001}"),
                    ("software",        "{20215, 2101}"),
                    ("thrusters",       "{20215, 2201}"),
                    ("turrets",         "{20215, 2301}"),
                    ("weapons",         "{20215, 2401}")
                };

                cmd.CommandText = "INSERT INTO EquipmentType (EquipmentTypeID, Name) values (@equipmentTypeID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@equipmentTypeID", item.Item1);
                    cmd.Parameters.AddWithValue("@name",            _Resolver.Resolve(item.Item2));

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
