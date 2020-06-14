using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace X4_DataExporterWPF.MainWindow
{
    class ViewModel : BindingBase
    {
        #region メンバ
        /// <summary>
        /// モデル
        /// </summary>
        private readonly Model _Model = new Model();
        #endregion


        #region プロパティ
        /// <summary>
        /// 入力元フォルダパス
        /// </summary>
        public ReactiveProperty<string> InDirPath { get; }


        /// <summary>
        /// 出力先ファイルパス
        /// </summary>
        public ReactiveProperty<string> OutFilePath { get; }


        /// <summary>
        /// 言語一覧
        /// </summary>
        public ReactiveCollection<LangComboboxItem> Langages { get; }


        /// <summary>
        /// 選択された言語
        /// </summary>
        public ReactiveProperty<LangComboboxItem?> SelectedLangage { get; }


        /// <summary>
        /// 進捗最大
        /// </summary>
        public ReactiveProperty<int> MaxSteps { get; }


        /// <summary>
        /// 現在の進捗
        /// </summary>
        public ReactiveProperty<int> CurrentStep { get; }


        /// <summary>
        /// ユーザが操作可能か
        /// </summary>
        public ReactiveProperty<bool> CanOperation { get; }


        /// <summary>
        /// 入力元フォルダ参照
        /// </summary>
        public ReactiveCommand SelectInDirCommand { get; }


        /// <summary>
        /// 出力先ファイルパス参照
        /// </summary>
        public ReactiveCommand SelectOutPathCommand { get; }


        /// <summary>
        /// 抽出実行
        /// </summary>
        public AsyncReactiveCommand ExportCommand { get; }


        /// <summary>
        /// ウィンドウを閉じる
        /// </summary>
        public ReactiveCommand<Window> CloseCommand { get; }
        #endregion



        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ViewModel()
        {
            var options = _Model.GetCommandlineOptions();

            InDirPath = new ReactiveProperty<string>(options.InputDirectory);
            OutFilePath = new ReactiveProperty<string>(options.OutputFilePath);

            Langages = new ReactiveCollection<LangComboboxItem>();
            SelectedLangage = new ReactiveProperty<LangComboboxItem?>();

            MaxSteps = new ReactiveProperty<int>(1);
            CurrentStep = new ReactiveProperty<int>(0);

            CanOperation = new ReactiveProperty<bool>(true);

            // 操作可能かつ入力項目に不備がない場合に true にする
            var canExport = new []{
                CanOperation,
                InDirPath.Select(p => !string.IsNullOrEmpty(p)),
                OutFilePath.Select(p => !string.IsNullOrEmpty(p)),
                SelectedLangage.Select(l => l != null),
            }.CombineLatestValuesAreAllTrue();

            SelectInDirCommand = new ReactiveCommand(CanOperation).WithSubscribe(SelectInDir);
            SelectOutPathCommand = new ReactiveCommand(CanOperation).WithSubscribe(SelectOutPath);
            ExportCommand = new AsyncReactiveCommand(canExport, CanOperation).WithSubscribe(Export);
            CloseCommand = new ReactiveCommand<Window>(CanOperation).WithSubscribe(Close);

            // 入力元フォルダパスが変更された時、言語一覧を更新する
            InDirPath.Subscribe(path =>
            {
                Langages.ClearOnScheduler();
                Langages.AddRangeOnScheduler(_Model.GetLangages(path));
            });
        }


        /// <summary>
        /// 入力元フォルダを選択
        /// </summary>
        private void SelectInDir()
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = Path.GetDirectoryName(InDirPath.Value);

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                InDirPath.Value = dlg.FileName;
            }
        }


        /// <summary>
        /// 出力先ファイルパスを選択
        /// </summary>
        private void SelectOutPath()
        {
            var dlg = new CommonSaveFileDialog();
            dlg.InitialDirectory = Path.GetDirectoryName(OutFilePath.Value);
            dlg.Filters.Add(new CommonFileDialogFilter("Database file", "*.db"));
            dlg.Filters.Add(new CommonFileDialogFilter("SQLite3 file", "*.sqlite"));
            dlg.Filters.Add(new CommonFileDialogFilter("All", "*.*"));
            dlg.DefaultFileName = Path.GetFileName(OutFilePath.Value);

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                OutFilePath.Value = dlg.FileName;
            }
        }


        private async Task Export()
        {
            await Task.Run(() =>
            {
                foreach (var (currentStep, maxSteps) in _Model.Export(InDirPath.Value, OutFilePath.Value, SelectedLangage.Value))
                {
                    CurrentStep.Value = currentStep;
                    MaxSteps.Value = maxSteps;
                }
            });
            CurrentStep.Value = 0;
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
