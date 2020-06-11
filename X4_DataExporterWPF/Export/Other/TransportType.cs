using System.Data.SQLite;
using LibX4.Lang;

namespace X4_DataExporterWPF.Export.Other
{
    /// <summary>
    /// カーゴ種別抽出用クラス
    /// </summary>
    class TransportType : IExport
    {
        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public TransportType(LangageResolver resolver)
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
CREATE TABLE IF NOT EXISTS TransportType
(
    TransportTypeID TEXT    NOT NULL PRIMARY KEY,
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
                    ("container", "{20205, 100}"),
                    ("liquid",    "{20205, 300}"),
                    ("solid",     "{20205, 200}")
                };

                // レコード追加
                cmd.CommandText = "INSERT INTO TransportType (TransportTypeID, Name) VALUES(@transportTypeID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("transportTypeID", item.Item1);
                    cmd.Parameters.AddWithValue("name", Resolver.Resolve(item.Item2));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
