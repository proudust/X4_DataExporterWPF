using CommandLine;

namespace X4_DataExporterWPF
{
    class CommandlineOptions
    {
        /// <summary>
        /// 入力元フォルダ初期値
        /// </summary>
        [Option('i', Required = false, HelpText = "Initial value of the input source folder")]
        public string? InputDirectory { get; set; }


        /// <summary>
        /// 出力先ファイルパス初期値
        /// </summary>
        [Option('o', Required = false, HelpText = "Initial value of the output file path")]
        public string? OutputFilePath { get; set; }
    }
}
