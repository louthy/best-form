using System;
using System.Linq;
using LanguageExt;
using static LanguageExt.Prelude;
using static BestForm.Html;
using BestForm.Tokens;

namespace BestForm
{
    public static class TokenHtml
    {
        public static Dom vis =>
            map(x => (Vis)x.Visibility,
                span(new { @class = "keyword" }, text<Vis>(x => x.ToString().ToLower())));

        public static Dom attrs =>
            map(x => x.Attributes,
                iter(div(new { @class = "attr" }, text((AttributeDef a) => $"[{a.Body}]"))));

        public static Dom keyword(object kw) =>
            span(new { @class = "keyword" }, text(kw.ToString()));

        public static Dom asyncAttr =>
            filter(x => x.IsAsync, keyword("async"));

        public static Dom staticAttr =>
            filter(x => x.IsStatic, keyword("static"));

        public static Dom partialAttr =>
            filter(x => x.IsPartial, keyword("partial"));

        public static Dom abstractAttr =>
            filter(x => x.IsAbstract, keyword("abstract"));

        public static Dom virtualAttr =>
            filter(x => x.IsVirtual, keyword("virtual"));

        public static Dom overrideAttr =>
            filter(x => x.IsOverride, keyword("override"));

        public static Dom newAttr =>
            filter(x => x.IsNew, keyword("new"));

        public static Dom sealedAttr =>
            filter(x => x.IsSealed, keyword("sealed"));

        public static Dom paramsAttr =>
            filter(x => x.IsParams, keyword("params"));

        public static Dom readonlyAttr =>
            filter(x => x.IsReadOnly, keyword("readonly"));

        public static Dom nullable =>
            filter(x => x.IsNullable, text("?"));

        public static Dom thisAttr =>
            filter(x => x.IsThis, keyword("this"));

        public static Dom ident =>
            combine(
                span(new { @class = "ident" }, text<Identifier>(id => id.Name)),
                generics);

        public static Dom fqn =>
            map<FQName, Lst<Identifier>>(n => n.Idents, dotSep(ident));

        public static Dom dotSep(Dom x) =>
            join(".", x);

        public static Dom commaSep(Dom x) =>
            join(", ", x);

        public static Dom spaceSep(Dom x) =>
            join(" ", x);

        public static Dom spaced(params Dom[] xs) =>
            combine(" ", xs);

        public static Dom rec(Func<Dom> dom) => inp =>
            dom()(inp);

        public static Dom generics =>
            map(tr => tr.GenericArgs, any(angles(rec(() => commaSep(typeRef)))));

        public static Dom typeRef =>
            combine(
                map<TypeRef, FQName>(tr => tr.Name, fqn),
                nullable,
                map<TypeRef, Option<ArrayDef>>(tr => tr.ArrayDef,
                    option<ArrayDef>(
                        Some: arr => text(arr.ToString()),
                        None: () => text(""))));

        public static Dom arg =>
            combine(
                spaced(
                    filter<Arg>(arg => arg.IsThis, keyword("this")),
                    filter<Arg>(arg => arg.IsOut, keyword("out")),
                    filter<Arg>(arg => arg.IsRef, keyword("ref")),
                    map<Arg, TypeRef>(arg => arg.Type, typeRef),
                    map<Arg, Identifier>(arg => arg.Name, ident)));

        public static Dom delarg =>
            spaced(
                map<DelegateArg, TypeRef>(arg => arg.Type, typeRef),
                map<DelegateArg, Option<Identifier>>(arg => arg.Name,
                    option<Identifier>(
                        Some: nm => text(nm.ToString()),
                        None: () => text(""))));

        public static Dom args =>
            map( x => x.Args,
                zeroOrMany(
                    Zero: text("()"),
                    Many: parens(commaSep(arg))));

        public static Dom delArgs =>
            map<DelegateDef,Lst<DelegateArg>>(x => x.Args,
                zeroOrMany(
                    Zero: text("()"),
                    Many: parens(commaSep(delarg))));

        public static Dom enumDef =>
            spaced(
                combine(attrs, vis),
                keyword("enum"),
                map<EnumDef, Identifier>(e => e.Name, ident),
                text(":"),
                map<EnumDef, TypeRef>(e => e.Type, typeRef));

        public static Dom delegateDef =>
            spaced(
                combine(attrs, vis),
                keyword("delegate"),
                map<DelegateDef, TypeRef>(d => d.Type, typeRef),
                combine(
                    map<DelegateDef, Identifier>(d => d.Name, ident),
                    delArgs));

        public static Dom fieldDef =>
            spaced(
                combine(attrs, vis),
                staticAttr,
                readonlyAttr,
                map<FieldDef, TypeRef>(f => f.Type, typeRef),
                map<FieldDef, Identifier>(f => f.Name, ident));

        public static Dom convOperator =>
            spaced(
                combine(attrs, vis),
                keyword("static"),
                filter<ConvOperatorDef>(op => op.Implicit,keyword("implicit")),
                filter<ConvOperatorDef>(op => !op.Implicit, keyword("explicit")),
                keyword("operator"),
                combine(
                    map<ConvOperatorDef, TypeRef>(o => o.ToType, typeRef),
                    map<ConvOperatorDef, Arg>(o => o.FromArg, parens(arg))));

        public static Dom binaryOperator =>
            spaced(
                combine(attrs, vis),
                keyword("static"),
                map<BinaryOperatorDef, TypeRef>(p => p.Type, typeRef),
                keyword("operator"),
                combine(
                    text<BinaryOperatorDef>(x => x.Op),
                    parens(
                        map<BinaryOperatorDef, Arg>(o => o.ArgLHS, arg),
                        text(", "),
                        map<BinaryOperatorDef, Arg>(o => o.ArgRHS, arg))));

        public static Dom unaryOperator =>
            spaced(
                combine(attrs, vis),
                keyword("static"),
                map<UnaryOperatorDef, TypeRef>(o => o.Type, typeRef),
                keyword("operator"),
                combine(
                    text<UnaryOperatorDef>(x => x.Op),
                    parens(map<UnaryOperatorDef, Arg>(o => o.Arg, arg))));

        public static Dom constDef =>
            spaced(
                combine(attrs, vis),
                keyword("const"),
                map<ConstDef, TypeRef>(c => c.Type, typeRef),
                map<ConstDef, Identifier>(c => c.Name, ident),
                text("="),
                text<ConstDef>(x => x.Source.ToString()));

        public static Dom propDef =>
            spaced(
                combine(attrs, vis),
                staticAttr,
                abstractAttr,
                virtualAttr,
                overrideAttr,
                newAttr,
                map<PropDef, TypeRef>(p => p.Type, typeRef),
                map<PropDef, FQName>(p => p.Name, fqn),
                map<PropDef, Option<Arg>>(p => p.IndexerArg, 
                    option<Arg>(
                        Some: brackets(arg),
                        None: text(""))),
                braces(
                    text(" "),
                    filter<PropDef>(p => p.GetSource is CodeBlockExpr, combine(keyword("get"), semi, text(" "))),
                    filter<PropDef>(p => p.SetSource is CodeBlockExpr, combine(keyword("set"), semi, text(" ")))));

        public static Dom methodDef =>
            spaced(
                combine(attrs, vis),
                staticAttr,
                asyncAttr,
                abstractAttr,
                virtualAttr,
                overrideAttr,
                newAttr,
                map<MethodDef, TypeRef>(m => m.Type, typeRef),
                combine(
                    map<MethodDef, FQName>(m => m.Name, fqn),
                    args));

        public static Dom typeTag =>
            map<TypeDef,TypeDefTag>(x => x.Type,
                span(new { @class = "keyword" }, text<TypeDefTag>(x => x.ToString().ToLower())));

        public static Dom tab =>
            text("    ");

        public static Dom typeDef =>
            spaced(
                combine(attrs, vis),
                staticAttr,
                sealedAttr,
                partialAttr,
                abstractAttr,
                typeTag,
                combine(
                    map<TypeDef, Identifier>(m => m.Name, ident),
                    map<TypeDef, Lst<TypeRef>>(m => m.DerivedFrom,
                        any(
                            combine(
                                text(" :\n"),
                                join(",<br/>", iter(span(tab, tab, typeRef))))))));

        static string[] ignoreReturnTypesOf = { "void", "System.Void", "Unit", "LanguageExt.Unit", "ctor" };

        public static Dom returns =>
            filter(m => !ignoreReturnTypesOf.Contains((string)(m.Type.Name.ToString())),
                map(m => Tuple(m.Type, m.Document), DocumentHtml.returnsAndTitle));
    }
}
