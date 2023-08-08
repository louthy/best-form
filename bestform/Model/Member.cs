using LanguageExt;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BestForm.Model;

/// <summary>
/// Single member with documentation.  May have child members in the case of classes and structs
/// </summary>
public record Member(string Name, MemberDeclarationSyntax Syntax, Document Comments, Seq<Member> Members)
{
    public string AnchorName =>
        Syntax.Parent switch
        {
            MemberDeclarationSyntax p => $"{ReadComments.NameFromDeclaration(p)}_{TypeParamsCount(p)}_{Name}_{TypeParamsCount()}",
            _ =>$"{Name}_{TypeParamsCount()}"
        };
            
    public string TypeParamsToText() =>
        Syntax switch
        {
            ClassDeclarationSyntax c     => c.TypeParameterList?.ToString() ?? "",
            InterfaceDeclarationSyntax c => c.TypeParameterList?.ToString() ?? "",
            StructDeclarationSyntax c    => c.TypeParameterList?.ToString() ?? "",
            DelegateDeclarationSyntax c  => c.TypeParameterList?.ToString() ?? "",
            RecordDeclarationSyntax c    => c.TypeParameterList?.ToString() ?? "",
            MethodDeclarationSyntax c    => c.TypeParameterList?.ToString() ?? "",
            _                            => "",
        };        
            
    public int TypeParamsCount() =>
        TypeParamsCount(Syntax);        
    
    static int TypeParamsCount(MemberDeclarationSyntax syntax) =>
        syntax switch
        {
            ClassDeclarationSyntax c     => c.TypeParameterList?.Parameters.Count ?? 0,
            InterfaceDeclarationSyntax c => c.TypeParameterList?.Parameters.Count ?? 0,
            StructDeclarationSyntax c    => c.TypeParameterList?.Parameters.Count ?? 0,
            DelegateDeclarationSyntax c  => c.TypeParameterList?.Parameters.Count ?? 0,
            RecordDeclarationSyntax c    => c.TypeParameterList?.Parameters.Count ?? 0,
            MethodDeclarationSyntax c    => c.TypeParameterList?.Parameters.Count ?? 0,
            _                            => 0,
        };
    
    public Html ConstraintsToHtml() =>
        Html.div2("constraints",
                  ConstraintsToText() switch
                  {
                      "" => Html.empty,

                      var cs => cs.Split("where")
                                  .Filter(x => !string.IsNullOrWhiteSpace(x))
                                  .Map(c => Html.div2("constraint",
                                                      Html.keyword("where"),
                                                      Html.span(Html.text(c))))
                                  .Join()
                  });

    public string ConstraintsToText() =>
        Syntax switch
        {
            ClassDeclarationSyntax c     => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
            InterfaceDeclarationSyntax c => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
            StructDeclarationSyntax c    => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
            DelegateDeclarationSyntax c  => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
            RecordDeclarationSyntax c    => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
            MethodDeclarationSyntax c    => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
            _                            => "",
        };
}
