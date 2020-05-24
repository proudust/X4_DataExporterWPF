using LibX4.FileSystem;
using System;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Xml.XPath;
using X4_ComplexCalculator.Common;
using X4_DataExporterWPF.Common;
using X4_DataExporterWPF.Export;
using X4_DataExporterWPF.Export.Other;
using X4_DataExporterWPF.Export.Equipment;
using X4_DataExporterWPF.Export.Module;
using X4_DataExporterWPF.Export.Race;
using X4_DataExporterWPF.Export.Ware;

namespace X4_DataExporterWPF.MainWindow
{
    class Model : INotifyPropertyChangedBace
    {
        #region メンバ
        /// <summary>
        /// 入力元フォルダパス
        /// </summary>
        private string _InDirPath;


        /// <summary>
        /// ユーザが操作可能か
        /// </summary>
        private bool _CanOperation = true;


        /// <summary>
        /// 進捗最大
        /// </summary>
        private int _MaxSteps = 100;


        /// <summary>
        /// 現在の進捗
        /// </summary>
        private int _CurrentStep = 0;
        #endregion

        #region プロパティ
        /// <summary>
        /// 言語一覧
        /// </summary>
        public ObservableRangeCollection<LangComboboxItem> Langages = new ObservableRangeCollection<LangComboboxItem>();


        /// <summary>
        /// 選択された言語
        /// </summary>
        public LangComboboxItem SelectedLangage { get; set; }


        /// <summary>
        /// 入力元フォルダパス
        /// </summary>
        public string InDirPath
        {
            get
            {
                return _InDirPath;
            }
            set
            {
                _InDirPath = value;
                UpdateLangages();
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// 出力先ファイルパス
        /// </summary>
        public string OutFilePath { get; set; }


        /// <summary>
        /// 進捗最大値
        /// </summary>
        public int MaxSteps
        {
            get
            {
                return _MaxSteps;
            }
            private set
            {
                if (value != _MaxSteps)
                {
                    _MaxSteps = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 現在の進捗
        /// </summary>
        public int CurrentStep
        {
            get
            {
                return _CurrentStep;
            }
            set
            {
                if (value != _CurrentStep)
                {
                    _CurrentStep = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// ユーザが操作可能か
        /// </summary>
        public bool CanOperation
        {
            get
            {
                return _CanOperation;
            }
            private set
            {
                if (_CanOperation != value)
                {
                    _CanOperation = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Model()
        {
            var option = CommandLine.Parser.Default.ParseArguments<CommandlineOptions>(System.Environment.GetCommandLineArgs());
            if (option.Tag == CommandLine.ParserResultType.Parsed)
            {
                var parsed = (CommandLine.Parsed<CommandlineOptions>)option;

                InDirPath = parsed.Value.InputDirectory;
                OutFilePath = parsed.Value.OutputFilePath;
            }
        }


        /// <summary>
        /// 言語一覧を更新
        /// </summary>
        private void UpdateLangages()
        {
            try
            {
                var catFiles = new CatFile(_InDirPath);

                var xml = catFiles.OpenXml("libraries/languages.xml");

                var langages = xml.XPathSelectElements("/languages/language").Select(x => new LangComboboxItem(int.Parse(x.Attribute("id").Value), x.Attribute("name").Value))
                                                                             .OrderBy(x => x.ID);

                Langages.Reset(langages);
            }
            catch(Exception)
            {
                Langages.Clear();
            }
        }


        /// <summary>
        /// 抽出実行
        /// </summary>
        public void Export()
        {
#if !DEBUG
            try
#endif
            {
                // ユーザ操作禁止
                CanOperation = false;

                if (System.IO.File.Exists(OutFilePath))
                {
                    System.IO.File.Delete(OutFilePath);
                }

                var catFile = new CatFile(_InDirPath);

                var consb = new SQLiteConnectionStringBuilder { DataSource = OutFilePath };
                using var conn = new SQLiteConnection(consb.ToString());
                conn.Open();

                using var trans = conn.BeginTransaction();
                using var cmd = conn.CreateCommand();

                var resolver = new LibX4.Lang.LangageResolver(catFile);

                // 英語をデフォルトにする
                resolver.LoadLangFile(44);
                resolver.LoadLangFile(SelectedLangage.ID);


                var waresXml = catFile.OpenXml("libraries/wares.xml");

                IExport[] exports =
                {
                    // 共通
                    new Export.Common(),                        // 共通情報
                    new Effect(),                               // 追加効果情報
                    new Export.Other.Size(resolver),            // サイズ情報
                    new TransportType(resolver),                // カーゴ種別情報
                    new Race(catFile, resolver),                // 種族情報
                    new Faction(catFile, resolver),             // 派閥情報
                    //new Map(catFile, resolver),                 // マップ

                    // ウェア関連
                    new WareGroup(catFile, resolver),           // ウェア種別情報
                    new Ware(waresXml, resolver),               // ウェア情報
                    new WareResource(waresXml),                 // ウェア生産時に必要な情報
                    new WareProduction(waresXml, resolver),     // ウェア生産に必要な情報
                    new WareEffect(waresXml),                   // ウェア生産時の追加効果情報

                    // モジュール関連
                    new ModuleType(resolver),                   // モジュール種別情報
                    new Module(catFile, waresXml, resolver),    // モジュール情報
                    new ModuleOwner(waresXml),                  // モジュール所有派閥情報
                    new ModuleProduction(waresXml),             // モジュール建造情報
                    new ModuleResource(waresXml),               // モジュール建造に必要なウェア情報
                    new ModuleProduct(catFile, waresXml),       // モジュールの生産品情報
                    new ModuleShield(catFile, waresXml),        // モジュールのシールド情報
                    new ModuleTurret(catFile, waresXml),        // モジュールのタレット情報
                    new ModuleStorage(catFile, waresXml),       // モジュールの保管容量情報

                    // 装備関連
                    new EquipmentType(resolver),                // 装備種別情報
                    new Equipment(catFile, waresXml, resolver), // 装備情報
                    new EquipmentOwner(waresXml),               // 装備保有派閥情報
                    new EquipmentResource(waresXml),            // 装備生産に必要なウェア情報
                    new EquipmentProduction(waresXml),          // 装備生産に関する情報

                    // 従業員関連
                    new WorkUnitProduction(waresXml),           // 従業員用生産情報
                    new WorkUnitResource(waresXml)              // 従業員用必要ウェア情報
                };

                // 進捗初期化
                MaxSteps = exports.Length;
                CurrentStep = 0;
                foreach (var export in exports)
                {
                    export.Export(cmd);
                    CurrentStep++;
                    DoEvents();
                }

                trans.Commit();

                MessageBox.Show("Data export completed.", "X4 DataExporter", MessageBoxButton.OK, MessageBoxImage.Information);
            }
#if !DEBUG
            catch (Exception e)
            {
                var msg = $"■Message\r\n{e.Message}\r\n\r\n■StackTrace\r\n{e.StackTrace}";
                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
#endif
            {
                // 操作禁止解除
                CanOperation = true;

                // 進捗初期化
                CurrentStep = 0;
            }
        }


        /// <summary>
        /// 画面更新
        /// </summary>
        private void DoEvents()
        {
            var frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback((obj) => { ((DispatcherFrame)obj).Continue = false; return null; });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }
    }
}
