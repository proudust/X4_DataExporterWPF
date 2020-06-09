using System.Data.SQLite;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// ウェア生産時の追加効果抽出用クラス
    /// </summary>
    public class Common : IExport
    {
        public void Export(SQLiteCommand cmd)
        {
            //////////////////
            // テーブル作成 //
            //////////////////
            {
                // テーブル作成
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Common
(
    Item    TEXT    NOT NULL PRIMARY KEY,
    Value   INTEGER
)";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                cmd.CommandText = "INSERT INTO Common (Item, Value) VALUES('FormatVersion', 1)";
                cmd.ExecuteNonQuery();
            }


            ///////////////
            // Index作成 //
            ///////////////
            {
                cmd.CommandText = "CREATE INDEX CommonIndex ON Common(Item)";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
