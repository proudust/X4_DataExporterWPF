using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace X4_DataExporterWPF.Common
{
    class CommandlineOptions
    {
        /// <summary>
        /// 入力元フォルダ初期値
        /// </summary>
        [Option('i', Required = false, HelpText = "Initial value of the input source folder", Default = "")]
        public string InputDirectory { get; set; }


        /// <summary>
        /// 出力先ファイルパス初期値
        /// </summary>
        [Option('o', Required = false, HelpText = "Initial value of the output file path", Default ="")]
        public string OutputFilePath { get; set; }
    }
}
