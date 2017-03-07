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
    public static class TypePage
    {
        public static Dom page =>
            map<Tuple<string, TypeDef>, object>(inp => new Model(inp.Item1, inp.Item2),
                combine(
                    navPanel(nav),
                    contentPanel(content)));

        public static Dom content =>
            combine(
                h1(typeTitle),
                p(DocumentHtml.summary),
                left(
                    section(
                        sectionTitle("DECLARATION"),
                        map<Model, TypeDef>(m => m.Type, code(typeDef)))),
                right(
                    section(
                        sectionTitle("NAMESPACE"),
                        text<Model>(m => m.Namespace)),
                    DocumentHtml.remarksSectionAndTitle
                ),
                any<Model, MethodDef>(
                    m => m.Ctors,
                    h2(text("Constructors")),
                    iter(methodDetail)),
                any<Model, DelegateDef>(
                    m => m.Delegates,
                    h2(text("Delegates")),
                    iter(delegateDetail)),
                any<Model, FieldDef>(
                    m => m.Fields,
                    h2(text("Fields")),
                    iter(fieldDetail)),
                any<Model, PropDef>(
                    m => m.Props,
                    h2(text("Properties")),
                    iter(propDetail)),
                any<Model, MethodDef>(
                    m => m.Methods,
                    h2(text("Methods")),
                    iter(methodDetail)),
                any<Model, EnumDef>(
                    m => m.Enums,
                    h2(text("Enums")),
                    iter(enumDetail)));

        public static Dom nav =>
            combine(
                any<Model, MethodDef>(
                    m => m.Ctors,
                    h3(text("CONSTRUCTORS")),
                    iter(methodNav)),
                any<Model, FieldDef>(
                    m => m.Fields,
                    h3(text("FIELDS")),
                    iter(fieldNav)),
                any<Model, PropDef>(
                    m => m.Props,
                    h3(text("PROPERTIES")),
                    iter(propNav)),
                any<Model, MethodDef>(
                    m => m.Methods,
                    h3(text("METHODS")),
                    iter(methodNav)),
                any<Model, DelegateDef>(
                    m => m.Delegates,
                    h3(text("DELEGATES")),
                    iter(delegateNav)),
                any<Model, EnumDef>(
                    m => m.Enums,
                    h3(text("ENUMS")),
                    iter(enumNav)));

        public static Dom methodArg =>
            div(new { @class = "arg" },
                div(new { @class = "arg-header" },
                    div(new { @class = "arg-name" }, text<Tuple<MethodDef, Arg>>(a => a.Item2.Name.ToString())),
                    div(new { @class = "arg-type" }, map<Tuple<MethodDef, Arg>, TypeRef>(a => a.Item2.Type, typeRef))),
                map<Tuple<MethodDef, Arg>, Tuple<Option<Document>, Arg>>(tup => Tuple(tup.Item1.Document, tup.Item2),
                    DocumentHtml.param(
                        Some: div(new { @class = "arg-desc" }, DocumentHtml.fmttag()),
                        None: text(""))));

        public static Dom constrainedBy =>
            iter(
                combine(
                    filter<ConstrainedBy>(cb => cb.Type.IsNone,
                        span(new { @class = "keyword" }, text<ConstrainedBy>(cb => cb.With))),
                    filter<ConstrainedBy>(cb => cb.Type.IsSome,
                        map<ConstrainedBy, TypeRef>(cb => cb.Type.IfNone(() => null), typeRef))));

        public static Dom constraint =>
            iter(
                code(
                    spaced(
                        keyword("where"),
                        map<Constraint, Identifier>(m => m.What, ident),
                        text(":"),
                        map<Constraint, Lst<ConstrainedBy>>(m => m.By, constrainedBy))));

        public static Dom constraints =>
            filter<MethodDef>(m => m.Constraints.Any(),
                combine(
                    sectionTitle("CONSTRAINTS"),
                    map<MethodDef, Lst<Constraint>>(m => m.Constraints, constraint)));

        public static Dom methodParams =>
            filter<MethodDef>(m => m.Args.Any(),
                combine(
                    sectionTitle("PARAMETERS"),
                        map<MethodDef, IEnumerable<Tuple<MethodDef, Arg>>>(m => zip(initInfinite(_ => m), m.Args), iter(methodArg))));

        public static Dom methodNav =>
            navLink(
                attr<MethodDef>(m => $"#{m.Name}"),
                combine(
                    text<MethodDef>(m => m.Name.ToString()),
                    parens(text<MethodDef>(m => String.Join(", ", m.Args.Map(x => x.Type.ToString()))))));

        public static Dom methodTitle =>
            div(new { @class = "member-title" },
                anchor(attr<MethodDef>(m => m.Name.ToString())),
                text<MethodDef>(m => m.Name.ToString()),
                parens(text<MethodDef>(m => String.Join(", ", m.Args.Map(x => x.Type.ToString())))),
                source);

        public static Dom methodDetail =>
            combine(
                methodTitle,
                section(
                    left(
                        DocumentHtml.summaryAndTitle,
                        methodParams,
                        DocumentHtml.remarksAndTitle
                    ),
                    right(
                        sectionTitle("DECLARATION"),
                        code(methodDef),
                        returns,
                        constraints,
                        DocumentHtml.exceptionsAndTitle),
                    div( new { @style="display:none", id=attr<MethodDef>(m => $"code-{m.UniqueName}") },
                        sectionTitle("SOURCE"),
                        code(text<MethodDef>(m => (m.Source as CodeBlockExpr)?.Source)))));

        public static Dom enumTitle =>
            div(new { @class = "member-title" },
                anchor(attr<EnumDef>(m => m.Name.ToString())),
                text<EnumDef>(m => m.Name.ToString()));

        public static Dom enumFieldDetail =>
            div(new { @class = "arg" },
                div(new { @class = "arg-header" },
                    div(new { @class = "arg-name" }, text<EnumMember>(a => a.Name.ToString())),
                    div(new { @class = "arg-type" }, number(text<EnumMember>(a => a.Value.Map(x => x.ToString()).IfNone(""))))),
                div(new { @class = "arg-desc" }, DocumentHtml.summary));

        public static Dom enumFieldDetail2 =>
            combine(
                anchor(attr<EnumMember>(m => m.Name.ToString())),
                div(new { @class = "enum-member" },
                    div(new { @class = "enum-field-name" },
                        span(new { @class = "type-name" }, text<EnumMember>(m => m.Name.ToString()))),
                    div(new { @class = "enum-field-summary" },
                        span(new { @class = "normal" }, DocumentHtml.summary))));

        public static Dom enumDetail =>
            combine(
                enumTitle,
                section(
                    p(DocumentHtml.summaryAndTitle),
                    left(
                        map<EnumDef, Lst<EnumMember>>(e => e.Members, iter(enumFieldDetail))),
                    right(
                        sectionTitle("DECLARATION"),
                        code(enumDef),
                        DocumentHtml.remarksSectionAndTitle
                    )));

        public static Dom enumNav =>
            navLink(attr<EnumDef>(m => $"#{m.Name}"), text<EnumDef>(m => m.Name.ToString()));

        public static Dom delegateTitle =>
            div(new { @class = "member-title" },
                anchor(attr<DelegateDef>(m => m.UniqueName)),
                text<DelegateDef>(m => m.Name.ToString()));

        public static Dom delegateDetail =>
            combine(
                delegateTitle,
                section(
                    left(
                        DocumentHtml.summaryAndTitle,
                        DelegatePage.delegateParams,
                        DocumentHtml.remarksAndTitle
                    ),
                    right(
                        sectionTitle("DECLARATION"),
                        code(delegateDef),
                        returns
                        )));

        public static Dom delegateNav =>
            navLink(attr<DelegateDef>(m => $"#{m.UniqueName}"), text<DelegateDef>(m => m.Name.ToString()));

        public static Dom fieldNav =>
            navLink(attr<FieldDef>(m => $"#{m.Name}"), text<FieldDef>(m => m.Name.ToString()));

        public static Dom fieldTitle =>
            div(new { @class = "member-title" },
                anchor(attr<FieldDef>(m => m.Name.ToString())),
                text<FieldDef>(m => m.Name.ToString()));

        public static Dom fieldDetail =>
            combine(
                fieldTitle,
                section(
                    left(
                        DocumentHtml.summaryAndTitle,
                        DocumentHtml.remarksAndTitle
                    ),
                    right(
                        sectionTitle("DECLARATION"),
                        code(fieldDef))));

        public static Dom propNav =>
            navLink(attr<PropDef>(m => $"#{m.Name}"), text<PropDef>(m => m.Name.ToString()));

        public static Dom propTitle =>
            div(new { @class = "member-title" },
                anchor(attr<PropDef>(m => m.Name.ToString())),
                text<PropDef>(m => m.Name.ToString()));

        public static Dom propDetail =>
            combine(
                propTitle,
                section(
                    left(
                        DocumentHtml.summaryAndTitle,
                        DocumentHtml.remarksAndTitle
                    ),
                    right(
                        sectionTitle("DECLARATION"),
                        code(propDef),
                        returns
                        )));

        public static Dom typeTitle =>
            spaced(
                text<Model>(m => m.Type.ToString()),
                text<Model>(m => m.Type.Type.ToString()));

        public class Model
        {
            public readonly string Namespace;
            public readonly TypeDef Type;
            public readonly Lst<MethodDef> Methods;
            public readonly Lst<MethodDef> Ctors;
            public readonly Lst<PropDef> Props;
            public readonly Lst<FieldDef> Fields;
            public readonly Lst<EnumDef> Enums;
            public readonly Lst<DelegateDef> Delegates;
            public readonly Option<Document> Document;

            public Model(string nameSpace, TypeDef type)
            {
                Namespace = nameSpace;

                Type = type;

                Document = type.Document;

                Methods = type.Members.Filter(m => m is MethodDef)
                                      .Map(m => m as MethodDef)
                                      .Filter(t => type.Type == TypeDefTag.Interface || t.Visibility == Vis.Public || t.Visibility == Vis.Protected)
                                      .OrderBy(t => t.Name.ToString())
                                      .Freeze();

                Ctors = Methods.Filter(m => m.Type.Name.ToString() == "ctor").Freeze();

                Props = type.Members.Filter(m => m is PropDef)
                                    .Map(m => m as PropDef)
                                    .Filter(t => type.Type == TypeDefTag.Interface || t.Visibility == Vis.Public || t.Visibility == Vis.Protected)
                                    .OrderBy(t => t.Name.ToString())
                                    .Freeze();

                Fields = type.Members.Filter(m => m is FieldDef)
                                     .Map(m => m as FieldDef)
                                     .Filter(t => t.Visibility == Vis.Public || t.Visibility == Vis.Protected)
                                     .OrderBy(t => t.Name.ToString())
                                     .Freeze();

                Methods = type.Members.Filter(m => m is MethodDef)
                                      .Map(m => m as MethodDef)
                                      .Filter(t => type.Type == TypeDefTag.Interface || t.Visibility == Vis.Public || t.Visibility == Vis.Protected)
                                      .Filter(m => m.Type.Name.ToString() != "ctor")
                                      .OrderBy(t => t.Name.ToString())
                                      .Freeze();

                Enums = type.Members.Filter(m => m is EnumDef)
                                    .Map(m => m as EnumDef)
                                    .Filter(t => t.Visibility == Vis.Public)
                                    .OrderBy(t => t.Name.ToString())
                                    .Freeze();

                Delegates = type.Members.Filter(m => m is DelegateDef)
                                        .Map(m => m as DelegateDef)
                                        .Filter(t => t.Visibility == Vis.Public)
                                        .OrderBy(t => t.Name.ToString())
                                        .Freeze();
            }
        }
    }
}
