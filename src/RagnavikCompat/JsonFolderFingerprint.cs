using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RagnavikCompat;

internal static class JsonFolderFingerprint
{
    internal static string Compute(string folder)
    {
        using var stream = new MemoryStream();
        if (Directory.Exists(folder))
        {
            foreach (var file in Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                var relative = file.Substring(folder.TrimEnd(Path.DirectorySeparatorChar).Length + 1);
                var name = Encoding.UTF8.GetBytes(relative);
                stream.Write(BitConverter.GetBytes(name.Length), 0, sizeof(int));
                stream.Write(name, 0, name.Length);
                using var input = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                stream.Write(BitConverter.GetBytes(input.Length), 0, sizeof(long));
                input.CopyTo(stream);
            }
        }

        using var sha = SHA256.Create();
        stream.Position = 0;
        return Convert.ToBase64String(sha.ComputeHash(stream));
    }
}
