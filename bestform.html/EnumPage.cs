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
    public static class EnumPage
    {
        public class Model
        {
            public readonly string Namespace;
            public readonly EnumDef Enum;
            public readonly Option<Document> Document;
            public readonly Lst<AttributeDef> Attributes;

            public Model(string ns, EnumDef def, Option<Document> document, Lst<AttributeDef> attributes)
            {
                Namespace = ns;
                Enum = def;
                Document = document;
                Attributes = attributes;
            }
        }

        public static Dom page =>
            map<Tuple<string, EnumDef>, object>(inp => new Model(inp.Item1, inp.Item2, inp.Item2.Document, inp.Item2.Attributes),
                combine(
                    map<Model, Lst<EnumMember>>(m => m.Enum.Members, navPanel(nav)),
                    contentPanel(content)));

        public static Dom body =>
            combine(
                p(DocumentHtml.summary),
                left(
                    section(
                        sectionTitle("DECLARATION"),
                        map<Model, EnumDef>(m => m.Enum, code(enumDef)))),
                right(
                    section(
                        sectionTitle("NAMESPACE"),
                        text<Model>(m => m.Namespace)),
                    DocumentHtml.remarksSectionAndTitle
                ),
                h2(text("Fields")),
                map<Model, Lst<EnumMember>>(m => m.Enum.Members, iter(enumFieldDetail)));

        public static Dom content =>
            combine(
                h1(text<Model>(e => $"{e.Enum.Name.ToString()} Enum")),
                body);

        public static Dom enumFieldDetail =>
            div(new { @class = "arg" },
                div(new { @class = "arg-header" },
                    div(new { @class = "arg-name" }, text<EnumMember>(a => a.Name.ToString())),
                    div(new { @class = "arg-type" }, number(text<EnumMember>(a => a.Value.Map(x => x.ToString()).IfNone(""))))),
                div(new { @class = "arg-desc" }, DocumentHtml.summary),
                div(new { @class = "arg-desc" }, DocumentHtml.remarksAndTitle)
                );

        public static Dom nav =>
            combine(
                h3(text("FIELDS")),
                iter(
                    navLink(
                        attr<EnumMember>(m => $"#{m.Name.ToString()}"), 
                        text<EnumMember>(m => m.Name.ToString()))));
    }
}
