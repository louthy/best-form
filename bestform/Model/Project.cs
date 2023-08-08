using LanguageExt;
using static LanguageExt.Prelude;

namespace BestForm.Model;

/// <summary>
/// Represents the whole project and contains the context as the application does its thing
/// </summary>
public record Project(string Name, 
    ProjectData Data, 
    string Root, 
    Seq<string> FoldersToExclude, 
    Folder? Folder, 
    string TargetFolder, 
    string Repo,
    string CSS,
    byte[] Logo)
{
    public static readonly Project Empty = new Project(
        "",
        ProjectData.Empty, 
        "", 
        Seq("obj", "bin", "hkt", ".git", ".github", ".idea", ".vs", "packages"),
        null,
        "",
        "",
        "",
        new byte[0]);

    public static Project New(string name, string root, string targetFolder, string repo, string css, byte[] logo) =>
        Empty with {Name = name, Root = root, TargetFolder = targetFolder, Repo = repo, CSS = css, Logo = logo};
}
