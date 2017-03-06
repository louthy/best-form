using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BestForm.Tokens;
using LanguageExt;
using LanguageExt.Parsec;
using static LanguageExt.Prelude;
using static LanguageExt.Parsec.Char;
using static LanguageExt.Parsec.Expr;
using static LanguageExt.Parsec.Prim;
using static LanguageExt.Parsec.Token;

namespace BestForm.CS
{
    public class Doc
    {
        readonly GenLanguageDef def;
        readonly GenTokenParser lexer;

        Parser<string> stringLiteral => lexer.StringLiteral;
        Parser<string> semi => lexer.Semi;
        Parser<string> comma => lexer.Comma;
        Parser<Unit> whiteSpace => lexer.WhiteSpace;
        Parser<string> reserved(string name) => lexer.Reserved(name);
        Parser<string> identifier => lexer.Identifier;
        Func<string, Parser<string>> symbol => lexer.Symbol;
        Parser<T> token<T>(Parser<T> p) => lexer.Lexeme(p);
        Parser<T> angles<T>(Parser<T> p) => lexer.Angles(p);
        Parser<T> brackets<T>(Parser<T> p) => lexer.Brackets(p);
        Parser<T> parens<T>(Parser<T> p) => lexer.Parens(p);
        Parser<T> braces<T>(Parser<T> p) => lexer.Braces(p);
        Parser<Lst<T>> commaSep<T>(Parser<T> p) => lexer.CommaSep(p);
        Parser<Lst<T>> commaSep1<T>(Parser<T> p) => lexer.CommaSep1(p);

        Parser<string> textUntil(Parser<string> p) =>
            from xs in many1(satisfy(c => c != '<'))
            from rs in
                either(
                    from _ in attempt(lookAhead(p))
                    select new string(xs.ToArray()),

                    from b in ch('<')
                    from n in textUntil(p)
                    select new string(xs.ToArray()) + "<" + n)
            select rs;

        Parser<DocAttr> attribute =>
            from id in identifier
            from eq in symbol("=")
            from vl in stringLiteral
            select new DocAttr(id, vl);

        Parser<Lst<DocAttr>> attributes =>
            many(attribute).Map(x => x.Freeze());

        Parser<string> endTag(string tag) =>
            from b1 in symbol("<")
            from s1 in symbol("/")
            from tg in reserved(tag)
            from b2 in symbol(">")
            select tg;

        Parser<Tag> openTag(string tag) =>
            from _____ in whiteSpace
            from openb in symbol("<")
            from open  in reserved(tag)
            from attrs in attributes
            from opene in symbol(">")
            from text  in either(
                            attempt(textUntil(endTag(tag))),
                            result(""))
            from close in endTag(tag)
            select new Tag(tag,attrs,text);

        Parser<Tag> closedTag(string tag) =>
            from openb in symbol("<")
            from open in reserved(tag)
            from attrs in attributes
            from opens in symbol("/")
            from opene in symbol(">")
            select new Tag(tag, attrs, "");

        Parser<Lst<Tag>> tags =>
            from tags in many1(
                choice(
                    attempt(openTag("summary")),
                    attempt(openTag("remarks")),
                    attempt(openTag("returns")),
                    attempt(openTag("param")),
                    attempt(openTag("typeparam")),
                    attempt(openTag("example")),
                    attempt(openTag("exception")),
                    attempt(openTag("permission")),
                    attempt(closedTag("include")),
                    attempt(closedTag("completionlist")),
                    attempt(closedTag("seealso")),
                    closedTag("see")))
            select tags.Freeze();

        Parser<Lst<Tag>> notags =>
            from t in asString(many(anyChar))
            select List<Tag>(new Tag("#text", List<DocAttr>(), t));

        public Parser<DocumentComments> Parser =>
            from tags in either(
                tags,
                notags
                )
            select new DocumentComments(tags);

        public Either<ParserError,DocumentComments> Parse(Lst<string> lines)
        {
            lines = lines.Map(x => String.IsNullOrWhiteSpace(x) ? "[CR-LF]" : x);

            lines = lines.Map(x =>
            {
                if(x.StartsWith("    "))
                {
                    return "[CODE]" + x/*.Replace(" ", "[NBSP]") */+ "[/CODE]";
                }
                else
                {
                    return x;
                }
            });

            var text = String.Join("", lines)   
                             .Replace("<para>","")
                             .Replace("</para>", "[CR-LF]");
            var res = Parser(text.ToPString());
            if( res.IsFaulted)
            {
                return res.Reply.Error;
            }
            else
            {
                return res.Reply.Result;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="original"></param>
        /// <completionlist cref=""/>
        /// <example></example>
        /// <include file='' path='[@name=""]'/>
        /// <permission cref=""></permission>
        /// <remarks></remarks>
        /// <see cref=""/>
        /// <seealso cref=""/>
        /// <typeparam></typeparam>
        public Doc()
        {
            def = GenLanguageDef.Empty.With(
                CommentStart: null,
                CommentEnd: null,
                CommentLine: null,
                NestedComments: false,
                IdentStart: letter,
                IdentLetter: alphaNum,
                OpStart: oneOf(@"!%&*+.<=>?@/\^|-~"),
                OpLetter: oneOf(@"!%&*+.<=>?@/\^|-~"),
                ReservedOpNames: List<string>(),
                ReservedNames: List<string>(
                    "summary", "remarks", "param", "example", "permission",
                    "include", "completionlist", "see", "seealso"),
                CaseSensitive: true
                );

            lexer = makeTokenParser(def);
        }
    }
}
