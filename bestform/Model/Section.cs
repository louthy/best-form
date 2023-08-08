using LanguageExt;

namespace BestForm.Model;

/// <summary>
/// Types of the sections of documents
/// </summary>
public enum SectionType
{
    Text,
    Returns,
    Param,
    TypeParam,
    Summary,
    Paragraph,
    Remarks,
    Code,
    Code2,
    Example,
    Exception
}

/// <summary>
/// Section of a document
/// </summary>
public abstract record Section(SectionType Type);
/// <summary>
/// Tagged section of a document
/// </summary>
public record Tag(SectionType Type, Seq<Attr> Attrs, Seq<Section> Inner) : Section(Type);

/// <summary>
/// Pure text section of a document
/// </summary>
public record Text(string Value) : Section(SectionType.Text);
