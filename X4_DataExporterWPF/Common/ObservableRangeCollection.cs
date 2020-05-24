using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace X4_DataExporterWPF.MainWindow
{
    /// <summary>
    /// ObservableCollectionの拡張(一括追加をサポート)
    /// </summary>
    /// <typeparam name="T">任意のデータ型</typeparam>
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        #region コンストラクタ
        public ObservableRangeCollection() : base()
        {
        }

        public ObservableRangeCollection(IEnumerable<T> collection) : base(collection)
        {
        }

        public ObservableRangeCollection(List<T> list) : base(list)
        {
        }
        #endregion


        /// <summary>
        /// コレクションを追加
        /// </summary>
        /// <param name="range">追加するコレクション</param>
        public virtual void AddRange(IEnumerable<T> range)
        {
            CheckReentrancy();

            foreach (var itm in range)
            {
                Add(itm);
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }


        /// <summary>
        /// クリアしてコレクションを追加
        /// </summary>
        /// <param name="range">追加するコレクション</param>
        public virtual void Reset(IEnumerable<T> range)
        {
            Items.Clear();
            AddRange(range);
        }
    }
}
