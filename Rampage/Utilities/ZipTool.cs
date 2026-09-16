using System.IO;
using System.IO.Compression;

namespace Rampage.Utilities
{
    /// <summary>
    /// Zip 压缩/解压工具（基于 System.IO.Compression）
    /// </summary>
    public static class ZipTool
    {
        /// <summary>
        /// 压缩单个文件为 zip 字节数组
        /// </summary>
        /// <param name="data"></param>
        /// <param name="entryName">压缩包内的条目名</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data, string entryName)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using (Stream entryStream = entry.Open())
                    {
                        entryStream.Write(data, 0, data.Length);
                    }
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 解压单条目 zip 字节数组
        /// </summary>
        /// <param name="zipData"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] zipData)
        {
            using (MemoryStream ms = new MemoryStream(zipData))
            using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                ZipArchiveEntry? entry = archive.Entries.Count > 0 ? archive.Entries[0] : null;
                if (entry == null)
                {
                    return Array.Empty<byte>();
                }
                using (MemoryStream result = new MemoryStream())
                using (Stream entryStream = entry.Open())
                {
                    entryStream.CopyTo(result);
                    return result.ToArray();
                }
            }
        }

        /// <summary>
        /// 压缩单个文件到 zip 文件
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="zipFile"></param>
        public static void CompressFile(string sourceFile, string zipFile)
        {
            string entryName = Path.GetFileName(sourceFile);
            using (FileStream zipStream = File.Create(zipFile))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(sourceFile, entryName, CompressionLevel.Optimal);
            }
        }

        /// <summary>
        /// 压缩整个目录到 zip 文件（保留相对目录结构）
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="zipFile"></param>
        public static void CompressDirectory(string sourceDir, string zipFile)
        {
            string root = Path.GetFullPath(sourceDir);
            using (FileStream zipStream = File.Create(zipFile))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
                    archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                }
            }
        }

        /// <summary>
        /// 解压 zip 文件到目标目录（自动创建；条目路径已做越界防护）
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="targetDir"></param>
        public static void DecompressToDirectory(string zipFile, string targetDir)
        {
            string root = Path.GetFullPath(targetDir);
            // 必须以尾分隔符比较，否则 "C:\data" 会放过 "C:\data_evil\x"
            string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) || root.EndsWith(Path.AltDirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }
                    string destPath = Path.GetFullPath(Path.Combine(root, entry.FullName));
                    if (!destPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Zip条目路径越界: {entry.FullName}");
                    }
                    string? destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }
        }
    }
}
