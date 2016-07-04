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
                navLinks(namespacesFilter,"NAMESPACES", namespaceNavLink),
                navLinks(delegateFilter, "DELEGATES", delegateNavLink),
                navLinks(typeFilter(TypeDefTag.Struct), "STRUCTS", typeNavLink),
                navLinks(typeFilter(TypeDefTag.Class), "CLASSES", typeNavLink),
                navLinks(typeFilter(TypeDefTag.Interface), "INTERFACES", typeNavLink),
                navLinks(enumFilter, "ENUMS", enumNavLink));

        public static Dom content =>
            combine(
                h1(spaced(text<Namespace>(ns => ns.Name.ToString()), text("Namespace"))),
                overviews(delegateFilter, "DELEGATES", delegateOverview),
                overviews(typeFilter(TypeDefTag.Struct), "STRUCTS", typeOverview),
                overviews(typeFilter(TypeDefTag.Class), "CLASSES", typeOverview),
                overviews(typeFilter(TypeDefTag.Interface), "INTERFACES", typeOverview),
                overviews(enumFilter, "ENUMS", enumOverview));

        static readonly Func<TypeDefTag, Func<Namespace, Lst<TypeDef>>> typeFilter =
            tag =>
                ns =>
                    (from t in ns.Types
                     where t.Type == tag && (t.Visibility == Vis.Public || t.Visibility == Vis.Protected)
                     orderby t.Name.Name
                     select t)
                    .Freeze();

        static readonly Func<Namespace, Lst<string>> namespacesFilter =
            ns =>
                (from n in ns.Namespaces
                 let name = n.ToString()
                 orderby name
                 select name)
                .Distinct()
                .Freeze();

        static readonly Func<Namespace, Lst<DelegateDef>> delegateFilter =
            ns =>
                (from d in ns.Delegates
                 where d.Visibility == Vis.Public
                 orderby d.Name.Name
                 select d)
                .Freeze();

        static readonly Func<Namespace, Lst<EnumDef>> enumFilter =
            ns =>
                (from e in ns.Enums
                 where e.Visibility == Vis.Public
                 orderby e.Name.Name
                 select e)
                .Freeze();

        public static Dom typeNavLink =>
            navLink(attr<TypeDef>(t => $"#{t.UniqueName}"), text<TypeDef>(t => t.ToString()));

        public static Dom typeLink =
            a(@class: text("type-name"), href: attr<TypeDef>(td => $"{td.UniqueName}.htm"), inner: text<TypeDef>(td => td.ToString()));

        public static Dom typeOverview =>
            div(new { @class = "doc-ns-type" },
                anchor(attr<TypeDef>(t => t.UniqueName)),
                left(
                    p(typeLink),
                    p(DocumentHtml.summary)),
                right(
                    code(typeDef)));

        public static Dom namespaceNavLink =>
            navLink(attr<string>(t => $"{t}/index.htm"), text<string>(identity));

        public static Dom navLinks<T, R>(Func<T, Lst<R>> items, string title, Dom itemDom) =>
            map(items,
                any(combine(
                    h3(text(title.ToUpper())),
                    iter(itemDom))));

        public static Dom overviews<T, R>(Func<T,Lst<R>> items, string title, Dom itemDom) =>
            map(items,
                any(combine(
                    h2(text(title)),
                    iter(itemDom))));

        public static Dom delegateNavLink =>
            navLink(attr<DelegateDef>(t => $"#{t.UniqueName}"), text<DelegateDef>(t => t.Name.ToString()));

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

        public static Dom enumNavLink =>
            navLink(attr<EnumDef>(t => $"#{t.Name}"), text<EnumDef>(t => t.Name.ToString()));

        public static Dom enumLink =
            a(@class: text("type-name"), href: attr<EnumDef>(td => $"{td.Name}.htm"), inner: text<EnumDef>(td => td.Name.ToString()));

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
