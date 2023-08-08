using LanguageExt;
using static LanguageExt.Prelude;

namespace BestForm.Model;

/// <summary>
/// Represents the whole project and contains the context as the application does its thing
/// </summary>
public record ProjectData(HashMap<string, File> Files, HashMap<string, string> ReadMe)
{
    public static readonly ProjectData Empty = new ProjectData(HashMap<string, File>(), HashMap<string, string>());
}
