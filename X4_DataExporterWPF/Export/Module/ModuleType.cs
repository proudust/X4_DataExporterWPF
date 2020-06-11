using System.Data.SQLite;
using LibX4.Lang;


namespace X4_DataExporterWPF.Export.Module
{
    /// <summary>
    /// モジュール種別抽出用クラス
    /// </summary>
    public class ModuleType : IExport
    {

        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver _Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public ModuleType(LangageResolver resolver)
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
                (string, string)[] items = {
                    ("buildmodule",         "{20104,  69901}"),
                    ("connectionmodule",    "{20104,  59901}"),
                    ("defencemodule",       "{20104,  49901}"),
                    ("dockarea",            "{20104,  70001}"),
                    ("habitation",          "{20104,  39901}"),
                    ("pier",                "{20104,  71101}"),
                    ("production",          "{20104,  19901}"),
                    ("storage",             "{20104,  29901}"),
                    ("ventureplatform",     "{20104, 101901}")
                };

                cmd.CommandText = "INSERT INTO ModuleType(ModuleTypeID, Name) values(@moduleTypeID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@moduleTypeID", item.Item1);
                    cmd.Parameters.AddWithValue("@name",         _Resolver.Resolve(item.Item2));

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
