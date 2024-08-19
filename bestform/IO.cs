using System;
using System.Linq;
using System.Threading.Tasks;
using F = System.IO.File;
using D = System.IO.Directory;
using P = System.IO.Path;

namespace BestForm;

public class IO
{
    public static string SafePath(string path) =>
        P.DirectorySeparatorChar switch
        {
            '\\' => path.Replace('/', '\\'),
            '/' => path.Replace('\\', '/'),
            var ps => throw new Exception($"Path separator not supported: {ps}")
        };
    
    
    public static void CreateDirectory(string path) =>
        D.CreateDirectory(SafePath(path));
    
    public static string[] DirectoryGetFiles(string path, string pattern) =>
        D.GetFiles(SafePath(path), pattern);
    
    public static string[] DirectoryGetDirectories(string path) =>
        D.GetDirectories(SafePath(path));
    
    public static bool FileExists(string path) =>
        F.Exists(SafePath(path));

    public static Task<string> ReadAllTextAsync(string path) =>
        F.ReadAllTextAsync(SafePath(path));

    public static string GetFileName(string path) =>
        P.GetFileName(SafePath(path));

    public static string Combine(params string[] parts) =>
        P.Combine(parts.Select(SafePath).ToArray());
    
    public static void WriteAllText(string path, string content) =>
        F.WriteAllText(SafePath(path), content);
    
    public static void WriteAllBytes(string path, byte[] bytes) =>
        F.WriteAllBytes(SafePath(path), bytes);
}