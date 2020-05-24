using LibX4.Lang;
using System.Data.SQLite;

namespace X4_DataExporterWPF.Export.Other
{
    /// <summary>
    /// サイズ情報抽出用クラス
    /// </summary>
    class Size : IExport
    {
        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public Size(LangageResolver resolver)
        {
            Resolver = resolver;
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
CREATE TABLE IF NOT EXISTS Size
(
    SizeID  TEXT    NOT NULL PRIMARY KEY,
    Name    TEXT    NOT NULL
)";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                // TODO:可能ならファイルから抽出する
                (string, string)[] items =
                {
                    ("extrasmall",  "{1001, 52}"),
                    ("small",       "{1001, 51}"),
                    ("medium",      "{1001, 50}"),
                    ("large",       "{1001, 49}"),
                    ("extralarge",  "{1001, 48}")
                };

                cmd.CommandText = "INSERT INTO Size (SizeID, Name) values (@sizeID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.AddWithValue("sizeID", item.Item1);
                    cmd.Parameters.AddWithValue("name", Resolver.Resolve(item.Item2));

                    cmd.ExecuteNonQuery();
                }
            }


            ///////////////
            // Index作成 //
            ///////////////
            {
                cmd.CommandText = "CREATE INDEX SizeIndex ON Size(SizeID)";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
