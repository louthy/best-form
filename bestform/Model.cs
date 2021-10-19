using System;
using System.Web;
using LanguageExt;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using static LanguageExt.Prelude;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BestForm
{
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
            Project.Empty with {Name = name, Root = root, TargetFolder = targetFolder, Repo = repo, CSS = css, Logo = logo};
    }

    /// <summary>
    /// Represents the whole project and contains the context as the application does its thing
    /// </summary>
    public record ProjectData(HashMap<string, File> Files)
    {
        public static readonly ProjectData Empty = new ProjectData(HashMap<string, File>());
    }

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

    /// <summary>
    /// Folder of files
    /// </summary>
    public record Folder(Map<string, File> Files, Map<string, Folder> Folders, int FileCount)
    {
        public static readonly Folder Empty = new(default, default, 0);
    }

    /// <summary>
    /// Represents a source file
    /// </summary>
    public record File(Seq<Member> Members);

    /// <summary>
    /// A document - many sections
    /// </summary>
    public record Document(Location Loc, Seq<Section> Sections)
    {
        public static readonly Document Empty = new Document(Location.None, Seq<Section>());
    }

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

    /// <summary>
    /// Name / Value attribute
    /// </summary>
    public record Attr(string Name, string Value)
    {
        public static Attr id(string id) =>
            new Attr("id", id);
        
        public static Attr @class(string value) =>
            new Attr("class", value);
        
        public static Attr href(string value) =>
            new Attr("href", value);

        public static Attr colspan(int x) =>
            new Attr("colspan", x.ToString());

        public override string ToString() =>
            $"{Name}=\"{Value}\"";
    }

    /// <summary>
    /// HTML virtual dom
    /// </summary>
    public abstract record Html
    {
        static Html tag(string name, Seq<Attr> attrs, params Html[] inner) => new HtmlTag(name, attrs, Seq(inner));
        static Html tag(string name, Seq<Attr> attrs, Seq<Html> inner) => new HtmlTag(name, attrs, inner);
        static Html tag(string name, params Html[] inner) => new HtmlTag(name, Empty, Seq(inner));
        static Html tag(string name, Seq<Html> inner) => new HtmlTag(name, Empty, inner);
        public static Html text(string text) => new HtmlText(HttpUtility.HtmlEncode(text));
        public static Html div(Seq<Attr> attrs, params Html[] inner) => tag("div", attrs, inner);
        public static Html div(Seq<Attr> attrs, Seq<Html>inner) => tag("div", attrs, inner);
        public static Html div(params Html[] inner) => tag("div", Seq(inner));
        public static Html div(Seq<Html> inner) => tag("div", inner);
        public static Html div(string id, Seq<Attr> attrs, params Html[] inner) => tag("div", attrs.Add(Attr.id(id)), inner);
        public static Html div(string id, Seq<Attr> attrs, Seq<Html>inner) => tag("div", attrs.Add(Attr.id(id)), inner);
        public static Html div(string id, params Html[] inner) => tag("div", Seq1(Attr.id(id)), Seq(inner));
        public static Html div(string id, Seq<Html> inner) => tag("div", Seq1(Attr.id(id)), inner);
        public static Html div2(string @class, Seq<Attr> attrs, params Html[] inner) => tag("div", attrs.Add(Attr.@class(@class)), inner);
        public static Html div2(string @class, Seq<Attr> attrs, Seq<Html>inner) => tag("div", attrs.Add(Attr.@class(@class)), inner);
        public static Html div2(string @class, params Html[] inner) => tag("div", Seq1(Attr.@class(@class)), Seq(inner));
        public static Html div2(string @class, Seq<Html> inner) => tag("div", Seq1(Attr.@class(@class)), inner);
        public static Html span(Seq<Attr> attrs, params Html[] inner) => tag("span", attrs, inner);
        public static Html span(Seq<Attr> attrs, Seq<Html>inner) => tag("span", attrs, inner);
        public static Html span(params Html[] inner) => tag("span", Seq(inner));
        public static Html span(Seq<Html> inner) => tag("span", inner);
        public static Html span(string @class, Seq<Attr> attrs, params Html[] inner) => tag("span", attrs.Add(Attr.@class(@class)), inner);
        public static Html span(string @class, Seq<Attr> attrs, Seq<Html>inner) => tag("span", attrs.Add(Attr.@class(@class)), inner);
        public static Html span(string @class, params Html[] inner) => tag("span", Seq1(Attr.@class(@class)), Seq(inner));
        public static Html span(string @class, Seq<Html> inner) => tag("span", Seq1(Attr.@class(@class)), inner);
        public static Html h1(string value) => tag("H1", text(value));
        public static Html h2(string value) => tag("H2", text(value));
        public static Html h3(string value) => tag("H3", text(value));
        public static Html p(string @class, params Html[] inner) => tag("p", Seq1(Attr.@class(@class)), inner);
        public static Html p(params Html[] inner) => tag("p", inner);
        public static Html em(params Html[] inner) => tag("em", inner);
        public static Html def(params Html[] inner) => span("def", inner);
        public static Html def(string id, string txt) => tag("a", Seq(Attr.id(id), Attr.@class("def")), text(txt));
        public static Html table(params Html[] inner) => tag("table", inner);
        public static Html tbody(params Html[] inner) => tag("tbody", inner);
        public static Html tr(params Html[] inner) => tag("tr", inner);
        public static Html td(string @class, params Html[] inner) => tag("td", Seq1(Attr.@class(@class)), inner);
        public static Html td2(string @class, params Html[] inner) => tag("td", Seq(Attr.colspan(2), Attr.@class(@class)), inner);
        public static Html a(string label, string href) => a(href, text(label));
        public static Html a(string href, params Html[] inner) => tag("a", Seq(Attr.href(href), Attr.@class("link")), inner);
        public static Html a(string @class, string label, string href) => tag("a", Seq(Attr.href(href), Attr.@class(@class)), text(label));
        public static Html html(params Html[] inner) => tag("html", Seq1(new Attr("xmlns","http://www.w3.org/1999/xhtml")), inner);
        public static Html body(params Html[] inner) => tag("body", Seq1(Attr.@class("js-enabled")), inner);
        public static Html ul(string @class, string id, params Html[] inner) => tag("ul", Seq(Attr.@class(@class), Attr.id(id)), inner);
        public static Html li(params Html[] inner) => tag("li", inner);
        public static Html title(string t) => tag("title", text(t));
        public static Html many(params Html[] inner) => new HtmlMany(inner.ToSeq());
        public static Html many(Seq<Html> inner) => new HtmlMany(inner);
        public static Html code(params Html[] inner) => tag("code", inner);
        public readonly static Html empty = new HtmlEmpty();

        public static Html head(string titleText) =>
            tag("head",
                title(titleText),
                tag("link", Seq(new Attr("href", "style.css"), new Attr("rel", "stylesheet"), new Attr("type", "text/css"))));
        
        public static Html header(string projectName, string source, string parent, string root) =>
            div(Seq1(Attr.id("package-header")),
                ul("links", "page-menu",
                    li(a("Source", source)),
                    li(a("Contents", parent)),
                    li(a("Index", root))),
                p("caption", text(projectName)));


        public static Html moduleHeader(string name) =>
            div(Seq1(Attr.id("module-header")),
                p("caption", text(name)));

        public static Html moduleHeader(params Html[] inner) =>
            div(Seq1(Attr.id("module-header")),
                p("caption", inner));

        public static Html keyword(string kw) =>
            Html.span("keyword", Html.text(kw));

        public static string render(Html h) =>
            @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">" + "\n" +
            renderInternal(h);
        
        static string renderInternal(Seq<Html> hs) =>
            string.Join("\n", hs.Map(renderInternal));

        public static string renderInternal(Html h) =>
            h switch
            {
                HtmlTag t when t.Attrs.IsEmpty && t.Inner.IsEmpty => $"<{t.Name}/>",
                HtmlTag t when t.Attrs.IsEmpty                    => $"<{t.Name}>{renderInternal(t.Inner)}</{t.Name}>",
                HtmlTag t when t.Inner.IsEmpty                    => $"<{t.Name} {string.Join(" ", t.Attrs)}/>",
                HtmlTag t                                         => $"<{t.Name} {string.Join(" ", t.Attrs)}>{renderInternal(t.Inner)}</{t.Name}>",
                HtmlText t                                        => t.Text,
                HtmlEmpty                                         => "",
                HtmlMany xs                                       => renderInternal(xs.Items),
                HtmlRaw raw                                       => raw.Html,
                _                                                 => throw new NotSupportedException()
            };

        public static Html operator +(Html x, Html y) =>
            (x, y) switch
            {
                (HtmlEmpty _, var r)       => r,
                (var l, HtmlEmpty _)       => l,
                (HtmlMany mx, HtmlMany my) => new HtmlMany(mx.Items + my.Items),
                (HtmlMany mx, var r)       => new HtmlMany(mx.Items.Add(r)),
                (var l, HtmlMany my)       => new HtmlMany(l.Cons(my.Items)),
                var (l, r)                 => new HtmlMany(Seq(l, r))
            };
    }

    /// <summary>
    /// HTML tag virtual DOM element
    /// </summary>
    public record HtmlTag(string Name, Seq<Attr> Attrs, Seq<Html> Inner) : Html;

    /// <summary>
    /// HTML text virtual DOM element
    /// </summary>
    public record HtmlText(string Text) : Html;

    /// <summary>
    /// Zero element of a virtual DOM
    /// </summary>
    public record HtmlEmpty : Html;
    
    /// <summary>
    /// Raw HTML source text DOM element
    /// </summary>
    public record HtmlRaw(string Html) : Html;
    
    /// <summary>
    /// Many HTML DOM elements
    /// </summary>
    public record HtmlMany(Seq<Html> Items) : Html;

    /// <summary>
    /// HTML DOM extensions
    /// </summary>
    public static class HtmlExt
    {
        public static Html Join(this IEnumerable<Html> xs) =>
            new HtmlMany(xs.ToSeq());
    }
}