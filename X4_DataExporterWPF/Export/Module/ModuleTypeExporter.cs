using System.Data.SQLite;
using LibX4.Lang;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// モジュール種別抽出用クラス
    /// </summary>
    public class ModuleTypeExporter : IExporter
    {

        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public ModuleTypeExporter(LangageResolver resolver)
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
CREATE TABLE IF NOT EXISTS ModuleType
(
    ModuleTypeID    TEXT    NOT NULL PRIMARY KEY,
    Name            TEXT    NOT NULL
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                // TODO:可能ならファイルから抽出する
                ModuleType[] items = {
                    new ModuleType("buildmodule",         "{20104,  69901}"),
                    new ModuleType("connectionmodule",    "{20104,  59901}"),
                    new ModuleType("defencemodule",       "{20104,  49901}"),
                    new ModuleType("dockarea",            "{20104,  70001}"),
                    new ModuleType("habitation",          "{20104,  39901}"),
                    new ModuleType("pier",                "{20104,  71101}"),
                    new ModuleType("production",          "{20104,  19901}"),
                    new ModuleType("storage",             "{20104,  29901}"),
                    new ModuleType("ventureplatform",     "{20104, 101901}")
                };

                cmd.CommandText = "INSERT INTO ModuleType(ModuleTypeID, Name) values(@moduleTypeID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@moduleTypeID", item.ModuleTypeID);
                    cmd.Parameters.AddWithValue("@name",         _Resolver.Resolve(item.Name));

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
