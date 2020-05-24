using System.Collections.Generic;

namespace LibX4.FileSystem
{
    /// <summary>
    /// catファイル内のフォルダ階層を管理
    /// </summary>
    class DirNode
    {
        /// <summary>
        /// フォルダ内ファイル一覧
        /// </summary>
        public Dictionary<string, CatEntry> Files = new Dictionary<string, CatEntry>();

        /// <summary>
        /// フォルダ内フォルダ一覧
        /// </summary>
        public Dictionary<string, DirNode> Directories = new Dictionary<string, DirNode>();
    }
}
