using System.Data.SQLite;
using X4_DataExporterWPF.Entity;

namespace X4_DataExporterWPF.Export
{
    /// <summary>
    /// ウェア生産時の追加効果抽出用クラス
    /// </summary>
    public class EffectExporter : IExporter
    {
        public void Export(SQLiteCommand cmd)
        {
            //////////////////
            // テーブル作成 //
            //////////////////
            {
                // テーブル作成
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Effect
(
    EffectID    TEXT    NOT NULL PRIMARY KEY,
    Name        TEXT    NOT NULL
) WITHOUT ROWID";
                cmd.ExecuteNonQuery();
            }


            ////////////////
            // データ抽出 //
            ////////////////
            {
                // TODO: 可能ならファイルから抽出する
                Effect[] items = { new Effect("work", "work") };

                // レコード追加
                cmd.CommandText = "INSERT INTO Effect (EffectID, Name) VALUES(@effectID, @name)";
                foreach (var item in items)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("effectID", item.EffectID);
                    cmd.Parameters.AddWithValue("name", item.Name);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
