using System;
using System.Linq;
using LanguageExt;
using static LanguageExt.List;
using static LanguageExt.Prelude;
using static BestForm.Html;
using static BestForm.TokenHtml;
using BestForm.Tokens;

namespace BestForm
{
    public static class NamespacePage
    {
        public static Dom page =>
            combine(
                navPanel(nav),
                contentPanel(content));

        public static Dom nav =>
            combine(
                typeNav("STRUCTS", TypeDefTag.Struct),
                typeNav("CLASSES", TypeDefTag.Class),
                typeNav("INTERFACES", TypeDefTag.Interface),
                delegatesNav,
                enumNav
                );

        public static Dom content =>
            combine(
                h1(spaced(text<Namespace>(ns => ns.Name.ToString()), text("Namespace"))),
                type("Structs", TypeDefTag.Struct),
                type("Classes", TypeDefTag.Class),
                type("Interfaces", TypeDefTag.Interface),
                delegates,
                enums
                );

        public static Dom typeNav(string title, TypeDefTag tag) =>
            combine(
                h3(text(title)),
                map<Namespace, Lst<TypeDef>>(ns => ns.Types.Filter(t => t.Type == tag && (t.Visibility == Vis.Public || t.Visibility == Vis.Protected))
                                                           .OrderBy(t => t.Name.Name).Freeze(), iter(typeNavLink)));

        public static Dom typeNavLink =>
            navLink(attr<TypeDef>(t => $"#{t.UniqueName}"), text<TypeDef>(t => t.ToString()));

        public static Dom type(string title, TypeDefTag tag) =>
            combine(
                h2(text(title)), 
                map<Namespace, Lst<TypeDef>>(ns => ns.Types.Filter(t => t.Type == tag && (t.Visibility == Vis.Public || t.Visibility == Vis.Protected))
                                                           .OrderBy(t => t.Name.Name).Freeze(), iter(overview)));

        public static Dom typeLink =
            a(@class: text("type-name"), href: attr<TypeDef>(td => $"{td.UniqueName}.htm"), inner: text<TypeDef>(td => td.ToString()));

        public static Dom overview =>
            div(new { @class = "doc-ns-type" },
                anchor(attr<TypeDef>(t => t.UniqueName)),
                left(
                    p(typeLink),
                    p(DocumentHtml.summary)),
                right(
                    code(typeDef)));

        public static Dom delegatesNav =>
            combine(
                h3(text("DELEGATES")),
                map<Namespace, Lst<DelegateDef>>(ns => ns.Delegates
                                                         .Filter(t => t.Visibility == Vis.Public)
                                                         .OrderBy(t => t.Name.Name).Freeze(), iter(delegateNavLink)));

        public static Dom delegateNavLink =>
            navLink(attr<DelegateDef>(t => $"#{t.UniqueName}"), text<DelegateDef>(t => t.Name.ToString()));

        public static Dom delegates =>
            combine(
                h2(text("Delegates")),
                map<Namespace, Lst<DelegateDef>>(ns => ns.Delegates
                                                         .Filter(t => t.Visibility == Vis.Public)
                                                         .OrderBy(t => t.Name.Name).Freeze(), iter(delegateOverview)));

        public static Dom delegateOverview =>
            div(new { @class = "doc-ns-type" },
                anchor(attr<DelegateDef>(t => t.UniqueName)),
                left(
                    p(delegateLink),
                    p(DocumentHtml.summary)),
                right(
                    code(delegateDef)));

        public static Dom delegateLink =
            a(@class: text("type-name"), href: attr<DelegateDef>(td => $"{td.UniqueName}.htm"), inner: text<DelegateDef>(td => td.Name.ToString()));

        public static Dom enumNav =>
            combine(
                h3(text("ENUMS")),
                map<Namespace, Lst<EnumDef>>(ns => ns.Enums.Filter(t => t.Visibility == Vis.Public)
                                                           .OrderBy(t => t.Name.Name).Freeze(), iter(enumNavLink)));

        public static Dom enumNavLink =>
            navLink(attr<EnumDef>(t => $"#{t.Name}"), text<EnumDef>(t => t.Name.ToString()));

        public static Dom enumLink =
            a(@class: text("type-name"), href: attr<EnumDef>(td => $"{td.Name}.htm"), inner: text<EnumDef>(td => td.Name.ToString()));

        public static Dom enums =>
            combine(
                h2(text("Enums")),
                map<Namespace, Lst<EnumDef>>(ns => ns.Enums
                                                     .Filter(t => t.Visibility == Vis.Public)
                                                     .OrderBy(t => t.Name.Name).Freeze(), iter(enumOverview)));

        public static Dom enumOverview =>
            div(new { @class = "doc-ns-type" },
                anchor(attr<EnumDef>(t => t.Name.ToString())),
                left(
                    p(enumLink),
                    p(DocumentHtml.summary)),
                right(
                    code(enumDef)));

    }
}
