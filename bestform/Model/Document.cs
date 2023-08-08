using LanguageExt;
using Microsoft.CodeAnalysis;
using static LanguageExt.Prelude;

namespace BestForm.Model;

/// <summary>
/// A document - many sections
/// </summary>
public record Document(Location Loc, Seq<Section> Sections)
{
    public static readonly Document Empty = new Document(Location.None, Seq<Section>());
}
