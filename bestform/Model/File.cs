using LanguageExt;

namespace BestForm.Model;

/// <summary>
/// Represents a source file
/// </summary>
public record File(Seq<Member> Members);
