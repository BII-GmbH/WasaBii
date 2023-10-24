using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BII.WasaBii.Unity {

    public static class FileUtils {

        /// <summary>
        /// Compares if two path strings refer to the same file or folder.
        /// Works, even if paths are relative / absolute or
        /// use different separator characters.
        /// </summary>
        public static bool IsSamePathAs(this string original, string other) {
            // Note DG: Since C# has no in-build way to do this, this code has been adapted from
            // https://stackoverflow.com/questions/2281531/how-can-i-compare-directory-paths-in-c

            string normalizedPath(string path) => Path
                .GetFullPath(path)
                .ToUpperInvariant()
                .Replace('\\', '/')
                .TrimEnd('/');

            var isWindowsOS = (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor);
            
            return string.Compare(
                normalizedPath(original), normalizedPath(other),
                isWindowsOS ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture
            ) == 0;
        }
        
        /// <summary>
        /// Returns the file paths of all files in the directory <paramref name="path"/>
        /// that match the <paramref name="searchPattern"/>.
        /// If the Directory does not exist, an empty Enumerable is returned.
        /// </summary>
        public static IEnumerable<string> GetFilePathsInDirectory(string path, string searchPattern) {
            if (!Directory.Exists(path))
                return Enumerable.Empty<string>();
            return Directory
                .GetFiles(path, searchPattern)
                .Select(f => f.Replace('\\', '/'));
        }

        /// <summary>
        /// Returns all sub-directories in the directory <paramref name="rootDir"/>
        /// that match the <paramref name="searchPattern"/>.
        /// If <paramref name="rootDir"/> does not exist, an empty Enumerable is returned.
        /// </summary>
        public static IEnumerable<string> GetSubDirectories(string rootDir, string searchPattern) {
            if (!Directory.Exists(rootDir))
                return Enumerable.Empty<string>();
            return Directory
                .GetDirectories(rootDir, searchPattern)
                .Select(f => f.Replace('\\', '/'));
        }

        public static IEnumerable<string> GetFileNamesInDirectory(
            string path, string searchPattern, bool includeExtension
        ) => GetFilePathsInDirectory(path, searchPattern)
            .Select(
                f => includeExtension ? Path.GetFileName(f) : Path.GetFileNameWithoutExtension(f)
            );

        // This regex is a match when the string contains any invalid file name character
        private static readonly Regex invalidFileCharacters =
            new Regex("[" + Regex.Escape(string.Join("", Path.GetInvalidFileNameChars())) + "]");

        /// <summary> Returns true if the file name contains no invalid characters (like /, \, etc.). </summary>
        public static bool IsValidFileName(string fileName) =>
            !string.IsNullOrEmpty(fileName) &&
            !invalidFileCharacters.IsMatch(fileName);

        /// <summary>
        /// Copies all files and Subdirectories form <paramref name="sourceDir"/> into <paramref name="targetDir"/>.
        /// </summary>
        public static void CopyDirectoryContents(string sourceDir, string targetDir, bool overwrite) {
            Directory.CreateDirectory(targetDir);
            var sourceDirInfo = new DirectoryInfo(sourceDir);
            foreach (var file in sourceDirInfo.GetFiles()) {
                File.Copy(
                    sourceFileName: $"{sourceDir.TrimEnd('/')}/{file.Name}",
                    destFileName: $"{targetDir.TrimEnd('/')}/{file.Name}",
                    overwrite
                );
            }

            foreach (var subdir in sourceDirInfo.GetDirectories()) {
                CopyDirectoryContents(
                    sourceDir: $"{sourceDir.TrimEnd('/')}/{subdir.Name}",
                    targetDir: $"{targetDir.TrimEnd('/')}/{subdir.Name}",
                    overwrite
                );
            }
        }

        public static bool TryOpenDirectoryInOS(string path) {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ) {
                System.Diagnostics.Process.Start(
                    "explorer.exe",
                    path.Replace(oldChar: '/', newChar: '\\')
                );

                return true;
            } else {
                Debug.LogWarning($"Opening of directories is not yet supported on OS {SystemInfo.operatingSystemFamily}");
                return false;
            }
        }
    }
    
}