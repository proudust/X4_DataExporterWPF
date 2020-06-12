using System.Data.SQLite;
using LibX4.Lang;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// カーゴ種別抽出用クラス
    /// </summary>
    class TransportTypeExporter : IExporter
    {
        /// <summary>
        /// 言語解決用オブジェクト
        /// </summary>
        private readonly LangageResolver Resolver;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="resolver">言語解決用オブジェクト</param>
        public TransportTypeExporter(LangageResolver resolver)
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
                TransportType[] items =
                {
                    new TransportType("container", "{20205, 100}"),
                    new TransportType("liquid",    "{20205, 300}"),
                    new TransportType("solid",     "{20205, 200}")
                };

                // レコード追加
                cmd.CommandText = "INSERT INTO TransportType (TransportTypeID, Name) VALUES(@transportTypeID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("transportTypeID", item.TransportTypeID);
                    cmd.Parameters.AddWithValue("name", Resolver.Resolve(item.Name));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
