using LanguageExt;

namespace BestForm.Model;

/// <summary>
/// Folder of files
/// </summary>
public record Folder(string Path, Map<string, File> Files, Map<string, Folder> Folders, string ReadMe, int FileCount)
{
    public static readonly Folder Empty = new("", default, default, "", 0);
}
