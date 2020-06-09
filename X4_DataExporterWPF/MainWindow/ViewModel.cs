using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using X4_ComplexCalculator.Common;
using X4_DataExporterWPF.Common;

namespace X4_DataExporterWPF.MainWindow
{
    class ViewModel : INotifyPropertyChangedBace
    {
        #region メンバ
        /// <summary>
        /// Model
        /// </summary>
        private Model _Model = new Model();
        #endregion


        #region プロパティ
        /// <summary>
        /// 入力元フォルダパス
        /// </summary>
        public string InDirPath
        {
            get
            {
                return _Model.InDirPath;
            }
            set
            {
                if (_Model.InDirPath != value)
                {
                    _Model.InDirPath = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// 出力先ファイルパス
        /// </summary>
        public string OutFilePath
        {
            get
            {
                return _Model.OutFilePath;
            }
            set
            {
                if (_Model.OutFilePath != value)
                {
                    _Model.OutFilePath = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// 言語一覧
        /// </summary>
        public ObservableCollection<LangComboboxItem> Langages => _Model.Langages;


        /// <summary>
        /// 選択された言語
        /// </summary>
        public LangComboboxItem SelectedLangage
        {
            set
            {
                _Model.SelectedLangage = value;
                OnPropertyChanged(nameof(CanExport));
            }
        }


        /// <summary>
        /// 進捗最大
        /// </summary>
        public int MaxSteps => _Model.MaxSteps;


        /// <summary>
        /// 現在の進捗
        /// </summary>
        public int CurrentStep => _Model.CurrentStep;


        /// <summary>
        /// ユーザが操作可能か
        /// </summary>
        public bool CanOperation => _Model.CanOperation;


        /// <summary>
        /// 抽出可能か
        /// </summary>
        public bool CanExport => CanOperation && _Model.SelectedLangage != null && !string.IsNullOrEmpty(_Model.OutFilePath);


        /// <summary>
        /// 入力元フォルダ参照
        /// </summary>
        public ICommand SelectInDirCommand { get; }


        /// <summary>
        /// 出力先ファイルパス参照
        /// </summary>
        public ICommand SelectOutPathCommand { get; }


        /// <summary>
        /// 抽出実行
        /// </summary>
        public ICommand ExportCommand { get; }


        /// <summary>
        /// ウィンドウを閉じる
        /// </summary>
        public ICommand CloseCommand { get; }
        #endregion



        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ViewModel()
        {
            SelectInDirCommand = new DelegateCommand(SelectInDir);
            SelectOutPathCommand = new DelegateCommand(SelectOutPath);
            ExportCommand = new DelegateCommand(_Model.Export);
            CloseCommand = new DelegateCommand<Window>(Close);
            _Model.PropertyChanged += Model_OnPropertyChanged;
        }


        /// <summary>
        /// Modelのプロパティの値変更時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Model_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_Model.MaxSteps):
                    OnPropertyChanged(nameof(MaxSteps));
                    break;

                case nameof(_Model.CurrentStep):
                    OnPropertyChanged(nameof(CurrentStep));
                    break;

                case nameof(_Model.CanOperation):
                    OnPropertyChanged(nameof(CanOperation));
                    OnPropertyChanged(nameof(CanExport));
                    break;

                case nameof(_Model.OutFilePath):
                    OnPropertyChanged(nameof(CanExport));
                    break;

                default:
                    break;
            }
        }


        /// <summary>
        /// 入力元フォルダを選択
        /// </summary>
        private void SelectInDir()
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = System.IO.Path.GetDirectoryName(InDirPath);

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                InDirPath = dlg.FileName;
            }
        }


        /// <summary>
        /// 出力先ファイルパスを選択
        /// </summary>
        private void SelectOutPath()
        {
            var dlg = new CommonSaveFileDialog();
            dlg.InitialDirectory = System.IO.Path.GetDirectoryName(OutFilePath);
            dlg.Filters.Add(new CommonFileDialogFilter("Database file", "*.db"));
            dlg.Filters.Add(new CommonFileDialogFilter("SQLite3 file", "*.sqlite"));
            dlg.Filters.Add(new CommonFileDialogFilter("All", "*.*"));
            dlg.DefaultFileName = System.IO.Path.GetFileName(OutFilePath);

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                OutFilePath = dlg.FileName;
            }
        }


        /// <summary>
        /// ウィンドウを閉じる
        /// </summary>
        /// <param name="window"></param>
        private void Close(Window window)
        {
            window.Close();
        }
    }
}
