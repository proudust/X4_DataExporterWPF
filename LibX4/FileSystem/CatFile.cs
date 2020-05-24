using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using X4_DataExporterWPF.Common;

namespace LibX4.FileSystem
{
    /// <summary>
    /// catファイル用ユーティリティクラス
    /// </summary>
    public class CatFile
    {
        /// <summary>
        /// バニラのファイル
        /// </summary>
        private readonly CatFileLoader _VanillaFile;

        /// <summary>
        /// Modのファイル
        /// </summary>
        private readonly Dictionary<string, CatFileLoader> _ModFiles = new Dictionary<string, CatFileLoader>();

        /// <summary>
        /// セレクタ名を分割する正規表現
        /// </summary>
        private readonly Regex _SplitSelectorRegex = new Regex(@"(.*\/)?(\..*?|.*?)$");


        /// <summary>
        /// Indexファイル
        /// </summary>
        private Dictionary<string, XDocument> _IndexFiles = new Dictionary<string, XDocument>();


        /// <summary>
        /// XML差分適用用ユーティリティクラス
        /// </summary>
        private readonly XMLPatcher _XMLPatcher = new XMLPatcher();


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="gameRoot">X4インストール先ディレクトリパス</param>
        public CatFile(string gameRoot)
        {
            _VanillaFile = new CatFileLoader(gameRoot);

            // Modのフォルダを読み込み
            if (Directory.Exists(Path.Combine(gameRoot, "extensions")))
            {
                foreach (var path in Directory.GetDirectories(Path.Combine(gameRoot, "extensions")))
                {
                    _ModFiles.Add($"extensions/{Path.GetFileName(path)}", new CatFileLoader(path));
                }
            }
        }


        /// <summary>
        /// 指定したファイルを開く
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>ファイルの内容</returns>
        public MemoryStream OpenFile(string filePath)
        {
            {
                using var ret = _VanillaFile.OpenFile(filePath);

                // バニラのデータに見つかればそちらを開く
                if (ret != null)
                {
                    return ret;
                }
            }
            

            foreach(var catFile in _ModFiles.Values)
            {
                using var ret = catFile.OpenFile(filePath);

                if (ret != null)
                {
                    return ret;
                }
            }

            throw new ArgumentException("Empty path", nameof(filePath));
        }


        /// <summary>
        /// xmlファイルを開く
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>xmlオブジェクト</returns>
        public XDocument OpenXml(string filePath)
        {
            XDocument ret = null;

            filePath = filePath.Replace('\\', '/');

            // バニラのxmlを読み込み
            {
                using var ms = _VanillaFile.OpenFile(filePath);
                if (ms != null)
                {
                    ret = XDocument.Load(ms);
                }
            }

            // Modのxmlを連結
            foreach(var (modPath, catFile) in _ModFiles)
            {
                var targetPath = filePath;

                // Modフォルダから指定された場合
                if (targetPath.StartsWith(modPath))
                {
                    targetPath = targetPath.Substring(modPath.Length + 1);
                }

                using var ms = catFile.OpenFile(targetPath);
                if (ms == null)
                {
                    continue;
                }

                if (ret == null)
                {
                    ret = XDocument.Load(ms);
                }
                else
                {
                    _XMLPatcher.MergeXML(ret, XDocument.Load(ms));
                }
            }

            return ret;
        }

        

        /// <summary>
        /// ノードを追加
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="element"></param>
        private void AddNode(XDocument dst, XElement element)
        {
            var type = element.Attribute("type")?.Value;
            var pos = element.Attribute("pos")?.Value;

            dst.XPathSelectElement(element.Attribute("sel").Value).Add(element.Elements());
        }




        /// <summary>
        /// 言語ファイルを開く
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>xmlオブジェクト</returns>
        public XDocument OpenLangXml(string filePath)
        {
            XDocument ret = null;

            // バニラのxmlを読み込み
            {
                using var ms = _VanillaFile.OpenFile(filePath);
                if (ms != null)
                {
                    ret = XDocument.Load(ms);
                }
            }

            // Modのxmlを連結
            foreach (var catFile in _ModFiles.Values)
            {
                using var ms = catFile.OpenFile(filePath);
                if (ms == null)
                {
                    continue;
                }

                if (ret == null)
                {
                    ret = XDocument.Load(ms);
                }
                else
                {
                    var src = XDocument.Load(ms);

                    foreach (var elm in src.XPathSelectElements("language/page"))
                    {
                        var addTarget = ret.XPathSelectElement($"/language/page[@id='{elm.Attribute("id").Value}']");

                        if (addTarget != null)
                        {
                            addTarget.Add(elm.Elements());
                        }
                        else
                        {
                            ret.XPathSelectElement("/language").Add(elm);
                        }
                    }
                }
            }

            return ret;
        }


        /// <summary>
        /// indexファイルに記載されているxmlを開く
        /// </summary>
        /// <param name="filePath">indexファイルパス</param>
        /// <param name="name">マクロ名等</param>
        /// <returns>解決結果先のファイル</returns>
        public XDocument OpenIndexXml(string filePath, string name)
        {
            XDocument ret = null;

            if (!_IndexFiles.ContainsKey(filePath))
            {
                _IndexFiles.Add(filePath, OpenXml(filePath));
            }

            var path = _IndexFiles[filePath].XPathSelectElement($"/index/entry[@name='{name}']")?.Attribute("value").Value;

            if (path != null)
            {
                if (path.StartsWith("extensions"))
                {

                }
                ret = OpenXml($"{path}.xml");
            }

            return ret;
        }
    }
}
