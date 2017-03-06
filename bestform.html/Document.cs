using System;
using System.Linq;
using LanguageExt;
using static LanguageExt.List;
using static LanguageExt.Prelude;
using static BestForm.Html;
using static BestForm.TokenHtml;
using BestForm.Tokens;
using System.Web;

namespace BestForm
{
    public static class DocumentHtml
    {
        public static Dom fmt(string text) =>
            span(new { }, _ => List(new DomMarkup(_, HttpUtility.HtmlEncode(text)
                .Replace("[CR-LF]", "<br/><br/>")
                .Replace("[NBSP]", "&nbsp;")
                .Replace("[CODE]", "<div class='code-line'>")
                .Replace("[/CODE]", "</div>")
                ) as DomElement));

        public static Dom doc(Func<DocumentComments, Dom> Some, Func<Dom> None) =>
            map(x => ((Option<Document>)x.Document).Map(d => d.Comments),
                option(Some, None));

        public static Dom summary =>
            doc(
                Some: c  => c.Summary.Map(x => fmt(x.InnerText)).IfNone(text("")),
                None: () => text(""));

        public static Dom summaryAndTitle =>
            doc(
                Some: c => combine(
                    sectionTitle("SUMMARY"),
                    p(c.Summary.Map(x => fmt(x.InnerText)).IfNone(text("")))),
                None: () => text(""));

        public static Dom returnsAndTitle =>
            combine(
                sectionTitle("RETURNS"),
                code(
                    div(new { @class = "member-title" },
                        map<Tuple<TypeRef, Option<Document>>, TypeRef>(tup => tup.Item1, typeRef)),
                    div(new { @class = "normal" },
                        map<Tuple<TypeRef, Option<Document>>, object>(tup => new { Document = tup.Item2 },
                            doc(
                                Some: comments => comments.Returns.Match(
                                    Some: ret => fmt(ret.InnerText),
                                    None: () => text("")),
                                None: () => text(""))))));

        public static Dom remarksAndTitle =>
            doc(
                Some: c => combine(
                    c.Remarks.Count == 0
                        ? text("")
                        : combine(
                            sectionTitle("REMARKS"),
                            combine("", c.Remarks.Map(r => fmt(r.InnerText)).ToArray()))),
                None: () => text(""));

        public static Dom remarksSectionAndTitle =>
            doc(
                Some: c => combine(
                    c.Remarks.Count == 0
                        ? text("")
                        : combine(
                            section(
                                sectionTitle("REMARKS"),
                                combine("", c.Remarks.Map(r => fmt(r.InnerText)).ToArray())))),
                None: () => text(""));

        public static Dom exceptionsAndTitle =>
            doc(
                Some: c => combine(
                    c.Exceptions.Count == 0
                        ? text("")
                        : combine(
                            sectionTitle("EXCEPTIONS"),
                            combine(c.Exceptions.Map(e => 
                                code(
                                    div(new { @class="member-title" }, text(e.Attr("cref").Map(x => x.Value).IfNone(""))), 
                                    div(new { @class="normal" }, fmt(e.InnerText)))).ToArray()))),
                None: () => text(""));

        public static Dom param(Dom Some, Dom None) =>
            map<Tuple<Option<Document>, Arg>, Option<Tag>>(tup =>
               (from d in tup.Item1
                from p in d.Comments.Params.Filter(x => x.Attr("name").Map(y => y.Value == tup.Item2.Name.ToString()).IfNone(false))
                select p).HeadOrNone(),
                option<Tag>(Some, None));

        public static Dom delegateParam(Dom Some, Dom None) =>
            map<Tuple<Option<Document>, DelegateArg>, Option<Tag>>(tup =>
               (from d in tup.Item1
                from p in d.Comments.Params.Filter(x => x.Attr("name").Map(y => y.Value == tup.Item2.Name.ToString()).IfNone(false))
                select p).HeadOrNone(),
                option<Tag>(Some, None));
    }
}
