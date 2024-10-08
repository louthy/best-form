using System;
using System.Web;
using LanguageExt;
using System.Collections.Generic;
using static LanguageExt.Prelude;

namespace BestForm.Model;

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
    public static Html ul(params Html[] inner) => tag("ul", inner);
    public static Html li(params Html[] inner) => tag("li", inner);
    public static Html title(string t) => tag("title", text(t));
    public static Html many(params Html[] inner) => new HtmlMany(inner.ToSeq());
    public static Html many(Seq<Html> inner) => new HtmlMany(inner);
    public static Html code(params Html[] inner) => tag("code", [Attr.@class("language-csharp")], inner);
    public static Html codeBlock(params Html[] inner) =>
        tag("pre", [Attr.@class("language-csharp")],
            tag("code", [Attr.@class("language-csharp")], inner));
    
    public static Html script(string src, string integrity, string crossorigin) => 
        tag("script", 
            Seq(Attr.src(src), 
                Attr.integrity(integrity), 
                Attr.crossorigin(crossorigin), 
                Attr.referrerpolicy("no-referrer")),
            text("") /* force the tag to be open */);
    public static Html script(string src, string crossorigin) => 
        tag("script", 
            Seq(Attr.src(src), 
                Attr.crossorigin(crossorigin), 
                Attr.referrerpolicy("no-referrer")),
            text("") /* force the tag to be open */);

    public static Html nowrap(params Html[] inner) =>
        div2("nowrap", inner);
    
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
