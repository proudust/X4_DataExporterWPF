using System.ComponentModel;

namespace X4_DataExporterWPF.MainWindow
{
    /// <summary>
    /// INotifyPropertyChangedを空実装し、WPFでメモリリークを防ぐ
    /// </summary>
    public class BindingBase : INotifyPropertyChanged
    {
        /// <summary>
        /// プロパティ変更時のイベントハンドラ
        /// </summary>
#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    }
}
