using System.Data.SQLite;
using LibX4.Lang;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// 装備種別情報抽出用クラス
    /// </summary>
    public class EquipmentTypeExporter : IExporter
    {
        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public EquipmentTypeExporter(LangageResolver resolver)
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
                EquipmentType[] items =
                {
                    new EquipmentType("countermeasures", "{20215, 1701}"),
                    new EquipmentType("drones",          "{20215, 1601}"),
                    new EquipmentType("engines",         "{20215, 1801}"),
                    new EquipmentType("missiles",        "{20215, 1901}"),
                    new EquipmentType("shields",         "{20215, 2001}"),
                    new EquipmentType("software",        "{20215, 2101}"),
                    new EquipmentType("thrusters",       "{20215, 2201}"),
                    new EquipmentType("turrets",         "{20215, 2301}"),
                    new EquipmentType("weapons",         "{20215, 2401}")
                };

                cmd.CommandText = "INSERT INTO EquipmentType (EquipmentTypeID, Name) values (@equipmentTypeID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@equipmentTypeID", item.EquipmentTypeID);
                    cmd.Parameters.AddWithValue("@name",            _Resolver.Resolve(item.Name));

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
