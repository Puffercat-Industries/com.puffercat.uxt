#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace Puffercat.Uxt.Editor
{
    public class FileUtility
    {
        /// <summary>
        /// Compare the content with the file at path with newContent, if they are different,
        /// write the new content to the file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newContent"></param>
        /// <returns>Whether <paramref name="newContent"/> has been written</returns>
        public static bool CreateFileAndImportIfChanged(string path, string newContent)
        {
            var oldContent = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            if (string.Equals(oldContent, newContent, StringComparison.Ordinal)) return false;
            File.WriteAllText(path, newContent, Encoding.UTF8);
            AssetDatabase.ImportAsset(path);
            return true;
        }
    }
}
#endif