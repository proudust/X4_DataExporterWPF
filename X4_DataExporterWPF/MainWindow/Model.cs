using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Xml.XPath;
using CommandLine;
using LibX4.FileSystem;
using LibX4.Lang;
using X4_ComplexCalculator.Common;
using X4_DataExporterWPF.Common;
using X4_DataExporterWPF.Export;

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
        /// 出力先ファイルパス
        /// </summary>
        private string _OutFilePath;


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
        public string OutFilePath
        {
            get
            {
                return _OutFilePath;
            }
            set
            {
                _OutFilePath = value;
                OnPropertyChanged();
            }
        }


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
            var option = Parser.Default.ParseArguments<CommandlineOptions>(Environment.GetCommandLineArgs());
            if (option.Tag == ParserResultType.Parsed)
            {
                var parsed = (Parsed<CommandlineOptions>)option;

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
            catch (Exception)
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

                if (File.Exists(OutFilePath))
                {
                    File.Delete(OutFilePath);
                }

                var catFile = new CatFile(_InDirPath);

                var consb = new SQLiteConnectionStringBuilder { DataSource = OutFilePath };
                using var conn = new SQLiteConnection(consb.ToString());
                conn.Open();

                using var trans = conn.BeginTransaction();

                var resolver = new LangageResolver(catFile);

                // 英語をデフォルトにする
                resolver.LoadLangFile(44);
                resolver.LoadLangFile(SelectedLangage.ID);


                var waresXml = catFile.OpenXml("libraries/wares.xml");

                IExporter[] exporters =
                {
                    // 共通
                    new CommonExporter(),                               // 共通情報
                    new EffectExporter(),                               // 追加効果情報
                    new SizeExporter(resolver),                         // サイズ情報
                    new TransportTypeExporter(resolver),                // カーゴ種別情報
                    new RaceExporter(catFile, resolver),                // 種族情報
                    new FactionExporter(catFile, resolver),             // 派閥情報
                    //new MapExporter(catFile, resolver),                 // マップ

                    // ウェア関連
                    new WareGroupExporter(catFile, resolver),           // ウェア種別情報
                    new WareExporter(waresXml, resolver),               // ウェア情報
                    new WareResourceExporter(waresXml),                 // ウェア生産時に必要な情報
                    new WareProductionExporter(waresXml, resolver),     // ウェア生産に必要な情報
                    new WareEffectExporter(waresXml),                   // ウェア生産時の追加効果情報

                    // モジュール関連
                    new ModuleTypeExporter(resolver),                   // モジュール種別情報
                    new ModuleExporter(catFile, waresXml, resolver),    // モジュール情報
                    new ModuleOwnerExporter(waresXml),                  // モジュール所有派閥情報
                    new ModuleProductionExporter(waresXml),             // モジュール建造情報
                    new ModuleResourceExporter(waresXml),               // モジュール建造に必要なウェア情報
                    new ModuleProductExporter(catFile, waresXml),       // モジュールの生産品情報
                    new ModuleShieldExporter(catFile, waresXml),        // モジュールのシールド情報
                    new ModuleTurretExporter(catFile, waresXml),        // モジュールのタレット情報
                    new ModuleStorageExporter(catFile, waresXml),       // モジュールの保管容量情報

                    // 装備関連
                    new EquipmentTypeExporter(resolver),                // 装備種別情報
                    new EquipmentExporter(catFile, waresXml, resolver), // 装備情報
                    new EquipmentOwnerExporter(waresXml),               // 装備保有派閥情報
                    new EquipmentResourceExporter(waresXml),            // 装備生産に必要なウェア情報
                    new EquipmentProductionExporter(waresXml),          // 装備生産に関する情報

                    // 従業員関連
                    new WorkUnitProductionExporter(waresXml),           // 従業員用生産情報
                    new WorkUnitResourceExporter(waresXml)              // 従業員用必要ウェア情報
                };

                // 進捗初期化
                MaxSteps = exporters.Length;
                CurrentStep = 0;
                foreach (var exporter in exporters)
                {
                    exporter.Export(conn);
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
