using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibX4.FileSystem
{
    class CatFileLoader
    {
        /// <summary>
        /// ゲームのインストール先
        /// </summary>
        private readonly string _GameRoot;


        /// <summary>
        /// ロード済みファイル一覧
        /// </summary>
        private readonly HashSet<string> _Loaded = new HashSet<string>();


        /// <summary>
        /// catファイルとdatファイルのペア
        /// </summary>
        private readonly Stack<(string, string)> _DataFiles = new Stack<(string, string)>();


        /// <summary>
        /// ファイルツリー
        /// </summary>
        private readonly DirNode _FileTree = new DirNode();


        /// <summary>
        /// catファイルのレコード分割用正規表現
        /// </summary>
        private readonly Regex _CatFileParser = new Regex("(.+)? ([0-9]+)? ([0-9]+)? ([a-fA-F0-9]+)?$", RegexOptions.Compiled);


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="gameRoot">ゲームのインストール先</param>
        public CatFileLoader(string gameRoot = "./")
        {
            _GameRoot = gameRoot;
            LoadFromGameRoot();
        }


        /// <summary>
        /// catファイルを読み込む
        /// </summary>
        /// <param name="catFilePath">catファイルパス</param>
        /// <param name="datFilePath">catファイルと対応するdatファイルパス</param>
        /// <returns>読み込みに成功したか</returns>
        private bool LoadCatFile(string catFilePath, string datFilePath)
        {
            var fileOffset = 0L;

            var entries = new List<(string[], CatEntry)>();


            foreach (var line in File.ReadLines(catFilePath).Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToLower()))
            {
                var parts = _CatFileParser.Matches(line).FirstOrDefault();

                // フォーマット不正
                if (parts == null || parts.Groups.Count != 5)
                {
                    return false;
                }

                var fileSize = long.Parse(parts.Groups[2].Value);

                var offset = fileOffset;
                fileOffset += fileSize;

                // ファイルパス不正
                if (!parts.Groups[1].Value.Contains('/'))
                {
                    continue;
                }

                var fileName = Path.GetFileName(parts.Groups[1].Value);
                var directories = Path.GetDirectoryName(parts.Groups[1].Value).Split('/');

                entries.Add((directories, new CatEntry(datFilePath, fileName, fileSize, offset)));
            }

            var fileTree = _FileTree;


            // ファイルツリーにentryを追加
            foreach (var (directories, entry) in entries)
            {
                var tree = fileTree;

                // entryのファイル格納先フォルダを辿る
                foreach (var directory in directories)
                {
                    // フォルダのツリーが無ければ作成する
                    if (!tree.Directories.ContainsKey(directory))
                    {
                        tree.Directories.Add(directory, new DirNode());
                    }

                    tree = tree.Directories[directory];
                }

                // ファイルが無ければ追加する
                if (!tree.Files.ContainsKey(entry.FileName))
                {
                    tree.Files.Add(entry.FileName, entry);
                }
            }

            // ロード済みにする
            _Loaded.Add(catFilePath);

            return true;
        }


        /// <summary>
        /// 優先順位を意識して次のcatファイルを読み込む
        /// </summary>
        /// <returns>ファイルが読み込まれたか</returns>
        private bool LoadNextCatFile()
        {
            var loaded = false;

            while (_DataFiles.Any())
            {
                var (catFilePath, datFilePath) = _DataFiles.Pop();

                // catファイルが未ロードの場合、ロードを試みる
                if (!_Loaded.Contains(catFilePath))
                {
                    loaded = LoadCatFile(catFilePath, datFilePath);
                    if (loaded)
                    {
                        break;
                    }
                }
            }

            return loaded;
        }


        /// <summary>
        /// インストール先よりcatファイルを全て読み込む
        /// </summary>
        /// <returns>読み込んだcatファイルとdatファイルのペア数</returns>
        private int LoadFromGameRoot()
        {
            var loaded = _DataFiles.Count;

            var reg = new Regex(@"^(?!.*sig).+?\.cat$");

            foreach (var fileName in Directory.GetFiles(_GameRoot).Select(x => Path.GetFileName(x)).Where(x => reg.IsMatch(x)).Select(x => Path.GetFileNameWithoutExtension(x)))
            {
                var catFilePath = Path.Combine(_GameRoot, $"{fileName}.cat");
                var datFilePath = Path.Combine(_GameRoot, $"{fileName}.dat");

                if (File.Exists(catFilePath) && File.Exists(datFilePath))
                {
                    _DataFiles.Push((catFilePath, datFilePath));
                }
            }

            return _DataFiles.Count - loaded;
        }


        /// <summary>
        /// directoriesに対応するDirNodeを検索する
        /// </summary>
        /// <param name="directories">ディレクトリ名のリスト</param>
        /// <returns>directoriesに対応するDirNode</returns>
        /// <remarks>
        /// directoriesの例) "foo/bar/baz" → ["foo", "bar", "baz"]
        /// </remarks>
        private DirNode FindDirectory(IEnumerable<string> directories)
        {
            var ret = _FileTree;

            foreach(var directory in directories)
            {
                var getSucceeded = ret.Directories.TryGetValue(directory, out DirNode next);

                // ディレクトリが見つかるまでcatファイルを読み込み続ける
                while (!getSucceeded && LoadNextCatFile())
                {
                    getSucceeded = ret.Directories.TryGetValue(directory, out next);
                }

                // 取得失敗時
                if (!getSucceeded)
                {
                    return null;
                }

                ret = next;
            }

            return ret;
        }


        /// <summary>
        /// directoriesとfileNameに対応するCatEntryとDirNodeを検索する
        /// </summary>
        /// <param name="directories">ディレクトリ名のリスト</param>
        /// <param name="fileName">ファイル名</param>
        /// <returns>directoriesとfileNameに対応するタプル</returns>
        /// <remarks>
        /// directoriesの例) "foo/bar/baz" → ["foo", "bar", "baz"]
        /// </remarks>
        private (CatEntry, DirNode) FindFile(IEnumerable<string> directories, string fileName)
        {
            DirNode directory = FindDirectory(directories);
            if (directory == null)
            {
                return (null, null);
            }

            var getSucceeded = directory.Files.TryGetValue(fileName, out CatEntry entry);
            while (!getSucceeded && LoadNextCatFile())
            {
                getSucceeded = directory.Files.TryGetValue(fileName, out entry);
            }

            if (!getSucceeded)
            {
                return (null, null);
            }
            
            return (entry, directory);
        }



        /// <summary>
        /// ファイルを開く
        /// </summary>
        /// <param name="filePath">開きたいファイルのパス</param>
        /// <returns>ファイルのMemoryStream</returns>
        public MemoryStream OpenFile(string filePath)
        {
            filePath = filePath.Replace('\\', '/');

            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            var directories = Path.GetDirectoryName(filePath.ToLower()).Split('/');
            var fileName = Path.GetFileName(filePath);

            var (entry, _) = FindFile(directories, fileName);
            //var (entry, _) = fileInfo ?? throw new ArgumentException($"Path {filePath} isn't a file.", nameof(filePath));

            if (entry == null)
            {
                return null;
            }

            using var fs = new FileStream(entry.DatFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, (int)entry.FileSize);
            fs.Seek(entry.Offset, SeekOrigin.Begin);

            var buff = new byte[entry.FileSize];
            fs.Read(buff, 0, buff.Length);

            return new MemoryStream(buff, false);
        }


        /// <summary>
        /// ファイルが存在するか確認する
        /// </summary>
        /// <param name="filePath">確認対象ファイルパス</param>
        /// <returns>ファイルが存在するか</returns>
        public bool FileExists(string filePath)
        {
            filePath = filePath.Replace('\\', '/');

            var directories = Path.GetDirectoryName(filePath.ToLower()).Split('/');
            var fileName = Path.GetFileName(filePath);

            var (entry, _) = FindFile(directories, fileName);

            return entry != null;
        }
    }
}
