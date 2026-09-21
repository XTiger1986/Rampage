using System.IO;
using System.IO.Compression;

namespace Rampage.Utilities
{
    /// <summary>
    /// Zip 压缩/解压工具（基于 System.IO.Compression） /
    /// Zip compression/decompression utility (based on System.IO.Compression)
    /// </summary>
    public static class ZipTool
    {
        /// <summary>
        /// 将字节数据压缩为单条目 zip 字节数组（Optimal 压缩级别） /
        /// Compress a byte array into a single-entry zip archive (Optimal compression level)
        /// </summary>
        /// <param name="data">待压缩数据 / The data to compress</param>
        /// <param name="entryName">压缩包内的条目名 / The entry name inside the archive</param>
        /// <returns>zip 字节数组 / The zip archive bytes</returns>
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
        /// 解压单条目 zip 字节数组：取第一个条目的内容，无条目时返回空数组 /
        /// Decompress a single-entry zip byte array: reads the first entry's content, an empty array when the archive has no entries
        /// </summary>
        /// <param name="zipData">zip 字节数组 / The zip archive bytes</param>
        /// <returns>条目内容字节数组 / The entry's decompressed bytes</returns>
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
        /// 压缩单个文件到 zip 文件（条目名为源文件名，Optimal 级别） /
        /// Compress a single file into a zip file (the entry name is the source file name, Optimal level)
        /// </summary>
        /// <param name="sourceFile">源文件路径 / The source file path</param>
        /// <param name="zipFile">目标 zip 文件路径 / The target zip file path</param>
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
        /// 压缩整个目录（含子目录）到 zip 文件：条目保留相对目录结构，分隔符统一为 / /
        /// Compress a whole directory (including subdirectories) into a zip file: entries keep the relative path structure with / separators
        /// </summary>
        /// <param name="sourceDir">源目录 / The source directory</param>
        /// <param name="zipFile">目标 zip 文件路径 / The target zip file path</param>
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
        /// 解压 zip 文件到目标目录（目标目录自动创建；条目路径已做 Zip Slip 越界防护） /
        /// Extract a zip file into a target directory (created as needed; entry paths are guarded against Zip Slip)
        /// </summary>
        /// <remarks>
        /// 每个条目先解析为基于目标目录的绝对路径，必须落在目标目录内，否则抛
        /// <see cref="InvalidOperationException"/>；已存在的同名文件会被覆盖；Name 为空的目录条目跳过。
        /// Each entry is resolved to an absolute path under the target directory and must stay inside it,
        /// otherwise an <see cref="InvalidOperationException"/> is thrown; existing files are overwritten;
        /// directory entries (empty Name) are skipped.
        /// </remarks>
        /// <param name="zipFile">zip 文件路径 / The zip file path</param>
        /// <param name="targetDir">目标目录 / The target directory</param>
        public static void DecompressToDirectory(string zipFile, string targetDir)
        {
            string root = Path.GetFullPath(targetDir);
            // 必须以尾分隔符比较，否则 "C:\data" 会放过 "C:\data_evil\x" / must compare with a trailing separator, otherwise "C:\data" would wrongly admit "C:\data_evil\x"
            string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) || root.EndsWith(Path.AltDirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Name 为空的是目录条目，跳过 / entries with an empty Name are directories, skip them
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
