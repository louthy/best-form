using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using static LanguageExt.List;
using static LanguageExt.Prelude;
using static BestForm.Html;
using static BestForm.TokenHtml;
using BestForm.Tokens;

namespace BestForm
{
    public static class DelegatePage
    {
        public class Model
        {
            public readonly string Namespace;
            public readonly DelegateDef Delegate;
            public readonly Option<Document> Document;
            public readonly Lst<AttributeDef> Attributes;

            public Model(string ns, DelegateDef def, Option<Document> document, Lst<AttributeDef> attributes)
            {
                Namespace = ns;
                Delegate = def;
                Document = document;
                Attributes = attributes;
            }
        }

        public static Dom page =>
            map<Tuple<string, DelegateDef>, object>(inp => new Model(inp.Item1, inp.Item2, inp.Item2.Document, inp.Item2.Attributes),
                combine(
                    navPanel(nav),
                    contentPanel(content)));

        public static Dom content =>
            combine(
                h1(text<Model>(m => m.Delegate.Name.ToString())),
                p(DocumentHtml.summary),
                left(
                    section(
                        sectionTitle("DECLARATION"),
                        map<Model, DelegateDef>(m => m.Delegate, code(delegateDef))
                    ),
                    section(
                        map<Model, DelegateDef>(m => m.Delegate, delegateParams)
                    )
                ),
                right(
                    section(
                        sectionTitle("NAMESPACE"),
                        text<Model>(m => m.Namespace)),
                    section(
                        map<Model, DelegateDef>(m => m.Delegate, returns)
                    ),
                    DocumentHtml.remarksSectionAndTitle
                )
            );

        public static Dom delegateParams =>
            filter<DelegateDef>(m => m.Args.Any(),
                combine(
                    sectionTitle("PARAMETERS"),
                        map<DelegateDef, IEnumerable<Tuple<DelegateDef, DelegateArg>>>(m => zip(initInfinite(_ => m), m.Args), iter(delegateArg))));

        public static Dom delegateArg =>
            div(new { @class = "arg" },
                div(new { @class = "arg-header" },
                    div(new { @class = "arg-name" }, text<Tuple<DelegateDef, DelegateArg>>(a => a.Item2.Name.Map(x => x.ToString()).IfNone(""))),
                    div(new { @class = "arg-type" }, map<Tuple<DelegateDef, DelegateArg>, TypeRef>(a => a.Item2.Type, typeRef))),
                map<Tuple<DelegateDef, DelegateArg>, Tuple<Option<Document>, DelegateArg>>(tup => Tuple(tup.Item1.Document, tup.Item2),
                    DocumentHtml.delegateParam(
                        Some: div(new { @class = "arg-desc" }, text<Tag>(t => t.InnerText)),
                        None: text(""))));

        public static Dom nav =>
            text("");
    }
}
