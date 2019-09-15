using System;
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
    public class Lang
    {
        readonly GenLanguageDef def;
        readonly GenTokenParser lexer;
        public readonly Parser<SourceFile> Parser;

        Parser<string> stringLiteral => lexer.StringLiteral;
        Parser<string> semi => lexer.Semi;
        Parser<string> comma => lexer.Comma;
        Parser<int> integer => lexer.Integer;
        Parser<double> floating => lexer.Float;
        Parser<int> natural => lexer.Natural;
        Parser<Unit> whiteSpace => lexer.WhiteSpace;
        Parser<string> reserved(string name) => lexer.Reserved(name);
        Parser<Unit> reservedOp(string op) => lexer.ReservedOp(op);

        public static Func<string, Func<CsExpr, CsExpr, CsExpr>> BinaryOp =
            (string op) =>
                (CsExpr lhs, CsExpr rhs) =>
                    new BinaryExpr(lhs,rhs,op);

        public static Func<string, Func<CsExpr, CsExpr>> PrefixOp =
            (string op) =>
                (CsExpr rhs) =>
                    new UnaryExpr(rhs, op, true);

        public static Func<string, Func<CsExpr, CsExpr>> PostfixOp =
            (string op) =>
                (CsExpr lhs) =>
                    new UnaryExpr(lhs, op, false);


        // Binary operator parser
        Func<string, Assoc, Operator<CsExpr>> binary =>
            (name, assoc) =>
                 Operator.Infix(assoc,
                    from x in reservedOp(name)
                    select BinaryOp(name));

        // Prefix operator parser
        Func<string, Operator<CsExpr>> prefix =>
            (name) =>
                Operator.Prefix(
                    from x in reservedOp(name)
                    select PrefixOp(name));

        // Postfix operator parser
        Func<string, Operator<CsExpr>> postfix =>
            (name) =>
                Operator.Postfix(
                    from x in reservedOp(name)
                    select PostfixOp(name));

        Parser<Identifier> identifier =>
            from idt in
                either(
                    attempt(lexer.Identifier),
                    token(from id in def.IdentStart
                          from __ in notFollowedBy(def.IdentLetter)
                          select id.ToString()))
            from gen in optional(genericArgs)
            select new Identifier(idt, gen.IfNone(List<TypeRef>()));

        Func<string, Parser<string>> symbol =>
            lexer.Symbol;

        Parser<T> token<T>(Parser<T> p) =>
            lexer.Lexeme(p);

        Parser<T> angles<T>(Parser<T> p) =>
            lexer.Angles(p);

        Parser<T> brackets<T>(Parser<T> p) =>
            lexer.Brackets(p);

        Parser<T> parens<T>(Parser<T> p) =>
            lexer.Parens(p);

        Parser<T> braces<T>(Parser<T> p) =>
            lexer.Braces(p);

        Parser<Lst<T>> commaSep<T>(Parser<T> p) =>
            lexer.CommaSep(p);

        Parser<Lst<T>> commaSep1<T>(Parser<T> p) =>
            lexer.CommaSep1(p);

        Parser<FQName> fqn =>
            from ps in sepBy1(identifier, lexer.Dot)
            select new FQName(ps.Freeze());

        Parser<string> docline =>
            token(
                from _ in str("///")
                from cs in either(consumeUntilEOL, result(""))
                select cs);

        static Doc docp = new Doc();

        Parser<Option<Document>> documentation =>
            optional(
                from line in many1(docline)
                let comments = docp.Parse(line.Freeze())
                from res in comments.Match(
                    Right: x => result(new Document(x)),
                    Left: e => failure<Document>(e.Msg))
                select res);

        Parser<CsExpr> expr = null;

        Parser<VarDecl> varDeclare =>
            from typ in either(
                reserved("var").Map(_ => TypeRef.Var),
                typeRef)
            from nam in identifier
            from rhs in optional(
                from eq in symbol("=")
                from ex in expr
                select ex)
            select new VarDecl(typ, nam, rhs);

        Parser<Lst<CsExpr>> block =>
            either(
                from e in either(attempt(statement), withsemi(expr))
                select List(e),
                braces(code));

        Parser<IfExpr> ifExpr =>
            from _ in reserved("if")
            from b in parens(expr)
            from t in block
            from e in optional(
                from __ in reserved("else")
                from el in either(
                    attempt(ifExpr.Map(x => List(x as CsExpr))),
                    block)
                select el)
            select new IfExpr(b, t, e);

        Parser<CallExpr> callExpr =>
            from n in fqn
            from a in tuple
            select new CallExpr(n, a);

        Parser<NewExpr> newExpr =>
            from _ in reserved("new")
            from n in typeRef
            from a in tuple
            select new NewExpr(n, a);

        Parser<ThrowExpr> throwExpr =>
            from t in reserved("throw")
            from n in optional(newExpr)
            select new ThrowExpr(n);

        Parser<T> withsemi<T>(Parser<T> p) =>
            attempt(from x in p
                    from _ in semi
                    select x);

        Parser<CsExpr> statement => choice(
            attempt(ifExpr).asExpr(),
            withsemi(throwExpr).asExpr(),
            withsemi(varDeclare).asExpr(),
            withsemi(callExpr).asExpr());

        Parser<CsExpr> simpleExpr => choice(
            attempt(parens(expr)).asExpr(),
            attempt(newExpr).asExpr(),
            attempt(callExpr).asExpr(),
            attempt(fqn.Map(id => new VarExpr(id))).asExpr()
            );

        Parser<Lst<CsExpr>> code =>
            from statements in many1(
                from e in statement
                select e
                )
            select statements.Freeze();

        Parser<Lst<TypeRef>> genericArgs =>
            token(lexer.AnglesCommaSep1(typeRef));

        Parser<AttributeDef> attribute =>
            from b in brackets(asString(many1(satisfy(x => x != ']'))))
            select new AttributeDef(b);

        Parser<Lst<AttributeDef>> attributes =>
            from a in optional(many1(attribute))
            select a.Map(x => x.Freeze()).IfNone(List<AttributeDef>());

        Parser<ArrayDef> arrayDef =>
            choice(
                attempt(symbol("[][]")).Map(_ => new ArrayDef(2, false)),
                attempt(symbol("[]")).Map(_ => new ArrayDef(1, false)),
                brackets(many1(comma)).Map(x => new ArrayDef(x.Count() + 1, true)));

        Parser<TypeRef> tupleRef =>
            from items in parens(
                commaSep1(
                    from t in lazyp(() => typeRef)
                    from n in optional(identifier)
                    select (t, n)))
            from n in optional(symbol("?"))
            from a in optional(arrayDef)
            select TypeRef.ValueTuple(items, a, n.IsSome);

        Parser<TypeRef> typeRef =>
            either(
                attempt(tupleRef),
                attempt(
                    from i in optional(reserved("in"))
                    from o in optional(reserved("out"))
                    from t in fqn
                    from n in optional(symbol("?"))
                    from a in optional(arrayDef)
                    select new TypeRef(t, a, n.IsSome, i.IsSome, o.IsSome)));

        Parser<Arg> arg =>
            from a in attributes
            from s in optional(reserved("this"))
            from o in optional(reserved("out"))
            from r in optional(reserved("ref"))
            from p in optional(reserved("params"))
            from t in typeRef
            from n in identifier
            from d in optional(
                from _ in symbol("=")
                from v in choice(
                    attempt(from deflt in reserved("default")
                            from dtype in parens(typeRef)
                            select new Constant(dtype, "default")),
                    attempt(floating).Map(c => new Constant(t, c.ToString())),
                    attempt(integer).Map(c => new Constant(t, c.ToString())),
                    attempt(stringLiteral.Map(c => new Constant(t, c))),
                    attempt(reserved("true").Map(c => new Constant(t, "true"))),
                    attempt(reserved("false").Map(c => new Constant(t, "false"))),
                    fqn.Map(c => new Constant(t, c.ToString())))
                select v)
            select new Arg(t, n, s.IsSome, p.IsSome, o.IsSome, r.IsSome, d, a);

        Parser<Lst<Arg>> args =>
            parens(commaSep(arg));

        Parser<Arg> oneArg =>
            parens(arg);

        Parser<Tuple<Arg,Arg>> twoArg =>
            parens(
                from one in arg
                from ___ in comma
                from two in arg
                select Tuple(one,two));

        Parser<Lst<CsExpr>> tuple =>
            either(
                attempt(from o in symbol("(")
                        from c in symbol(")")
                        select List<CsExpr>()),
                parens(commaSep(expr)));

        Parser<Vis> visibility =>
            from x in optional(
                choice(
                    attempt(symbol("public").Map(_ => Vis.Public)),
                    attempt(symbol("private").Map(_ => Vis.Private)),
                    attempt(symbol("protected").Map(_ => Vis.Protected)),
                    attempt(symbol("internal").Map(_ => Vis.Internal))))
            select x.IfNone(Vis.Private);

        Parser<DelegateArg> delegateArg =>
            from t in typeRef
            from n in optional(identifier)
            select new DelegateArg(t, n);

        Parser<Lst<DelegateArg>> delegateArgs =>
            parens(commaSep(delegateArg));

        Parser<DelegateDef> delegateDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from ___ in reserved("delegate")
            from ret in typeRef
            from nam in identifier
            from ags in delegateArgs
            from s in semi
            select new DelegateDef(nam, ret, ags, vis, doc, atr);

        Parser<EnumMember> enumMember =>
            from doc in documentation
            from a in attributes
            from nam in identifier
            from val in optional(
                from _ in symbol("=")
                from v in integer
                select v)
            from pos in getPos
            select new EnumMember(nam, val, doc, a);

        Parser<EnumDef> enumDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from ___ in reserved("enum")
            from nam in identifier
            from typ in optional(
                from c in symbol(":")
                from t in choice(
                    attempt(symbol("byte").Map(_ => TypeRef.Byte)),
                    attempt(symbol("char").Map(_ => TypeRef.Char)),
                    attempt(symbol("int").Map(_ => TypeRef.Int)),
                    attempt(symbol("short").Map(_ => TypeRef.Short)),
                    attempt(symbol("long").Map(_ => TypeRef.Long)),
                    attempt(symbol("uint").Map(_ => TypeRef.UInt)),
                    attempt(symbol("ushort").Map(_ => TypeRef.UShort)),
                    attempt(symbol("ubyte").Map(_ => TypeRef.UByte)),
                    symbol("ulong").Map(_ => TypeRef.ULong))
                select t)
            from mem in braces(commaSep1(attempt(enumMember)))
            select new EnumDef(nam, mem.Freeze(), typ.IfNone(TypeRef.Int), doc, atr, vis);

        Parser<Lst<TypeRef>> derived =>
            from x in commaSep1(typeRef)
            select x.Freeze();

        Parser<string> consumeUntilEOL = asString(many1(satisfy(c => c != '\n')));
        Parser<string> consumeUntilBrace = asString(many1(satisfy(c => c != '}')));
        Parser<string> consumeUntilSemi = asString(many1(satisfy(c => c != ';')));

        Parser<CodeBlockExpr> codeBlock =>
            either(
                attempt(from pos in getPos
                        from opn in ch('{')
                        from res in codeBlockInner
                        from cls in token(ch('}'))
                        select new CodeBlockExpr(res, pos)),
                from arr in str("=>")
                from pos in getPos
                from cod in lambdaBlock
                from sem in semi
                select new CodeBlockExpr(cod, pos));

        Parser<Set<string>> qualifiers(params string[] names) =>
            from xs in many(choice(names.Map(n => attempt(reserved(n)))))
            select LanguageExt.Set.createRange(xs);

        Parser<FieldDef> fieldDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from qua in qualifiers("readonly", "static", "event", "volatile")
            from typ in typeRef
            from nam in identifier
            from src in optional(
                from eq in symbol("=")
                from ps in getPos
                from sr in lambdaBlock // expr
                select new CodeBlockExpr(sr, ps) as CsExpr )
            from sem in semi
            select new FieldDef(typ, nam, vis, src.IfNone(new CsExpr()), atr, doc, qua.Contains("readonly"), qua.Contains("static"), qua.Contains("event"), qua.Contains("volatile"));

        Parser<ConstDef> constDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from ___ in reserved("const")
            from typ in typeRef
            from nam in identifier
            from eq  in symbol("=")
            from pos in getPos
            from src in lambdaBlock // expr
            from sem in semi
            select new ConstDef(typ, nam, vis, new CodeBlockExpr(src, pos), atr, doc);

        Parser<CsExpr> getBlock =>
            from gvis in visibility
            from pos  in getPos
            from gres in reserved("get")
            from ginr in either(
                symbol(";").Map(_ => new CodeBlockExpr("get;", pos) as CsExpr),
                codeBlock.asExpr())
            select ginr;

        Parser<CsExpr> setBlock =>
            from gvis in visibility
            from pos in getPos
            from gres in reserved("set")
            from ginr in either(
                symbol(";").Map(_ => new CodeBlockExpr("set;", pos) as CsExpr),
                codeBlock.asExpr())
            select ginr;

        Parser<PropDef> indexPropDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from qua in qualifiers("static", "abstract", "virtual", "override", "new")
            from ret in typeRef
            from ths in fqn
            from def in brackets(arg)
            from src in either(
                attempt(from arr in symbol("=>")
                        from pos in getPos
                        from get in lambdaBlock // expr
                        from sem in semi
                        select Tuple(new CodeBlockExpr(get, pos) as CsExpr, CsExpr.None)),
                braces(
                    choice(
                        attempt(from g in getBlock
                                from s in setBlock
                                select Tuple(g, s)),
                        attempt(from s in setBlock
                                from g in getBlock
                                select Tuple(g, s)),
                        attempt(from g in getBlock
                                select Tuple(g, CsExpr.None)),
                        from s in setBlock
                        select Tuple(CsExpr.None, s)
                    )))
            select new PropDef(ret, ths, vis, src.Item1, src.Item2, new CsExpr(), atr, doc, qua.Contains("static"), qua.Contains("abstract"), qua.Contains("virtual"), qua.Contains("override"), qua.Contains("new"), Some(def));

        Parser<PropDef> propDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from qua in qualifiers("static", "abstract", "virtual", "override", "new")
            from typ in typeRef
            from nam in fqn
            from src in either(
                attempt(from arr in symbol("=>")
                        from pos in getPos
                        from get in lambdaBlock // expr
                        from sem in semi
                        select Tuple(new CodeBlockExpr(get, pos) as CsExpr, CsExpr.None)),
                braces(
                    choice(
                        attempt(from g in getBlock
                                from s in setBlock
                                select Tuple(g, s)),
                        attempt(from s in setBlock
                                from g in getBlock
                                select Tuple(g, s)),
                        attempt(from g in getBlock
                                select Tuple(g, CsExpr.None)),
                        from s in setBlock
                        select Tuple(CsExpr.None, s)
                    )))
            from alo in optional(
                from sq in symbol("=")
                from ps in getPos
                from sr in lambdaBlock
                from sem in semi
                select new CodeBlockExpr(sr, ps) as CsExpr
            )
            select new PropDef(typ, nam, vis, src.Item1, src.Item2, alo.IfNone(new CsExpr()), atr, doc, qua.Contains("static"), qua.Contains("abstract"), qua.Contains("virtual"), qua.Contains("override"), qua.Contains("new"), None);

        Parser<CsExpr> function =>
            choice(
                from _ in semi
                select CsExpr.None,
                codeBlock.asExpr());

        Parser<Constraint> constraint =>
            from _  in reserved("where")
            from id in identifier
            from __ in symbol(":")
            from co in commaSep1(choice(
                attempt(reserved("struct").Map(_ => new ConstrainedBy("struct",None))),
                attempt(reserved("class").Map(_ => new ConstrainedBy("class", None))),
                attempt(reserved("new()").Map(_ => new ConstrainedBy("new", None))),
                typeRef.Map(t => new ConstrainedBy("type", t))
                ))
            select new Constraint(id,co);

        Parser<Lst<Constraint>> constraints =>
            from cs in optional(many1(constraint))
            select cs.Map(x=>x.Freeze()).IfNone(List<Constraint>());

        Parser<MethodDef> finaliserDef =>
            from doc in documentation
            from atr in attributes
            from til in symbol("~")
            from nam in fqn
            from opn in symbol("(")
            from clo in symbol(")")
            from src in function
            select new MethodDef(TypeRef.Final, nam, List<Arg>(), Vis.Public, src, atr, doc, List<Constraint>(), false, false, false, false, false, false);

        Parser< MethodDef> methodDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from qua in qualifiers("static", "abstract", "virtual", "override", "async", "new")
            from typ in typeRef
            from nam in fqn
            from ags in args
            from con in constraints
            from src in function
            select new MethodDef(typ, nam, ags, vis, src, atr, doc, con, qua.Contains("static"), qua.Contains("abstract"), qua.Contains("virtual"), qua.Contains("override"), qua.Contains("new"), qua.Contains("async"));

        Parser<MethodDef> ctorDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from sta in optional(reserved("static"))
            from nam in fqn
            from ags in args
            from bas in baseThis            
            from src in codeBlock.asExpr()
            select new MethodDef(TypeRef.Ctor, nam, ags, vis, src, atr, doc, List<Constraint>(), sta.IsSome, false, false, false, false, false);

        Parser<ConvOperatorDef> convOperatorDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from sta in reserved("static")
            from imp in either(reserved("implicit"), reserved("explicit"))
            from opr in reserved("operator")
            from typ in typeRef
            from arg in oneArg
            from src in function
            select new ConvOperatorDef(imp == "implicit", typ, arg, vis, src, atr, doc);

        Parser<BinaryOperatorDef> binaryOperatorDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from sta in reserved("static")
            from typ in typeRef
            from opr in reserved("operator")
            from op  in lexer.Operator
            from arg in twoArg
            from src in function
            select new BinaryOperatorDef(op, typ, arg.Item1, arg.Item2, vis, src, atr, doc);

        Parser<UnaryOperatorDef> unaryOperatorDef =>
            from doc in documentation
            from atr in attributes
            from vis in visibility
            from sta in reserved("static")
            from typ in typeRef
            from opr in reserved("operator")
            from op in choice(lexer.Operator, symbol("true"), symbol("false"))
            from arg in oneArg
            from src in function
            select new UnaryOperatorDef(op, typ, arg, vis, src, atr, doc);

        Parser<Lst<SrcToken>> typeMembers =>
            either(
                attempt(from ms in many1(
                        choice(
                            attempt(finaliserDef).asToken(),
                            attempt(indexPropDef).asToken(),
                            attempt(propDef).asToken(),
                            attempt(binaryOperatorDef).asToken(),
                            attempt(unaryOperatorDef).asToken(),
                            attempt(convOperatorDef).asToken(),
                            attempt(fieldDef).asToken(),
                            attempt(methodDef).asToken(),
                            attempt(ctorDef).asToken(),
                            attempt(constDef).asToken(),
                            attempt(typeDef).asToken(),
                            attempt(delegateDef).asToken(),
                            attempt(enumDef).asToken(),
                            attempt(delegateDef).asToken()
                            ))
                    select ms.Freeze()),
                result(List<SrcToken>()));
    
        Parser<TypeDef> typeDef =>
                from doc in documentation
                from atr in attributes
                from vis in visibility
                from qua in qualifiers("static", "partial", "abstract", "sealed")
                from tag in choice(
                    attempt(reserved("class").Map(_ => TypeDefTag.Class)),
                    attempt(reserved("interface").Map(_ => TypeDefTag.Interface)),
                    reserved("struct").Map(_ => TypeDefTag.Struct))
                from nam in identifier
                from der in optional(from _ in symbol(":")
                                     from d in derived
                                     select d)
                from con in constraints
                from mem in braces(typeMembers)
                select new TypeDef(tag, nam, vis, der.IfNone(List<TypeRef>()), mem, qua.Contains("static"), qua.Contains("partial"), qua.Contains("abstract"), qua.Contains("sealed"), doc, atr, con);

        Parser<Using> usingDef =>
            from _ in reserved("using")
            from s in optional(reserved("static"))
            from n in fqn
            from c in semi
            select new Using(s.IsSome, n);

        Parser<UsingAlias> usingAliasDef =>
            from _ in reserved("using")
            from a in identifier
            from e in symbol("=")
            from s in fqn
            from c in semi
            select new UsingAlias(s, a);

        Parser <Namespace> namespaceDef =>
            from _ in reserved("namespace")
            from n in fqn
            from b in braces(namespaceInnerDef)
            select new Namespace(n, b.Usings, b.UsingAliases, b.Namespaces, b.Types, b.Enums, b.Delegates);

        Parser<Namespace> namespaceInnerDef =>
            from toks in many1(
                choice(
                    attempt(namespaceDef.asToken()),
                    attempt(usingDef.asToken()),
                    attempt(usingAliasDef.asToken()),
                    attempt(typeDef.asToken()),
                    attempt(enumDef.asToken()),
                    attempt(delegateDef.asToken()),
                    attempt(delegateDef.asToken())))
            let toklist = toks.Freeze()
            select new Namespace(
                new FQName(List<Identifier>()),
                toklist.Filter(t => t is Using).Map(t => t as Using),
                toklist.Filter(t => t is UsingAlias).Map(t => t as UsingAlias),
                toklist.Filter(t => t is Namespace).Map(t => t as Namespace),
                toklist.Filter(t => t is TypeDef).Map(t => t as TypeDef),
                toklist.Filter(t => t is EnumDef).Map(t => t as EnumDef),
                toklist.Filter(t => t is DelegateDef).Map(t => t as DelegateDef));

        public Lang()
        {
            def = GenLanguageDef.Empty.With(
                CommentStart: "/*",
                CommentEnd: "*/",
                CommentLine: "// ",
                NestedComments: false,
                IdentStart: choice(letter, ch('_'), ch('@')),
                IdentLetter: either(alphaNum, oneOf("_'")),
                OpStart: oneOf(@"!%&*+.<=>?@/\^|-~"),
                OpLetter: oneOf(@"!%&*+.<=>?@/\^|-~"),
                ReservedOpNames: List<string>(),
                ReservedNames: List<string>(
                    "if", "else", "throw", "switch", "catch", "new", "return", "yield", "static",
                    "public", "private", "internal", "abstract", "virtual", "override", "class", "interface", "struct",
                    "enum", "break"
                    ),
                CaseSensitive: true
                );

            lexer = makeTokenParser(def);

            // Operator table
            Operator<CsExpr>[][] table = {
                new [] { postfix("->"), postfix("++"), postfix("--") },
                new [] { prefix("!"), prefix("~"), prefix("++"), prefix("--"), prefix("-"), prefix("+") },
                new [] { binary("*", Assoc.Left), binary("/", Assoc.Left), binary("%", Assoc.Left) },
                new [] { binary("+", Assoc.Left), binary("-", Assoc.Left) },
                new [] { binary("<", Assoc.Left), binary(">", Assoc.Left), binary(">=", Assoc.Left) , binary("<=", Assoc.Left) },
                new [] { binary("==", Assoc.Left), binary("!=", Assoc.Left) },
                new [] { binary("&", Assoc.Left) },
                new [] { binary("^", Assoc.Left) },
                new [] { binary("|", Assoc.Left) },
                new [] { binary("&&", Assoc.Left) },
                new [] { binary("||", Assoc.Left) },
                new [] { binary("?", Assoc.Right), binary(":", Assoc.Right) },
                new [] { binary("=", Assoc.Right), binary("+=", Assoc.Right), binary("-=", Assoc.Right), binary("*=", Assoc.Right), binary("/=", Assoc.Right) , binary("%=", Assoc.Right), binary(">>=", Assoc.Right) , binary("<<=", Assoc.Right), binary("&=", Assoc.Right), binary("^=", Assoc.Right), binary("|=", Assoc.Right) },
            };

            expr = Expr.buildExpressionParser<CsExpr>(table, simpleExpr);

            Parser =
                from __ in whiteSpace
                from ns in namespaceInnerDef
                select new SourceFile(ns.Usings, ns.UsingAliases, ns.Namespaces, ns.Types, ns.Enums, ns.Delegates);
        }

        /// <summary>
        /// Parse nested code blocks, ignore the structure and content other than its braces
        /// </summary>
        Parser<string> codeBlockInner =>
            inp =>
            {
                var depth = 1;
                var builder = new StringBuilder();
                bool quote = false;

                for (var index = inp.Index; index < inp.EndIndex; index++)
                {
                    var res = anyChar(inp);
                    if (res.Tag == ResultTag.Consumed && res.Reply.Tag == ReplyTag.OK)
                    {
                        if (res.Reply.Result == '{')
                        {
                            depth++;
                            inp = res.Reply.State;
                            builder.Append(res.Reply.Result);
                        }
                        else if (res.Reply.Result == '}')
                        {
                            depth--;
                            if (depth == 0 && !quote)
                            {
                                return ParserResult.ConsumedOK(builder.ToString(), inp);
                            }
                            else
                            {
                                inp = res.Reply.State;
                                builder.Append(res.Reply.Result);
                            }
                        }
                        else if (res.Reply.Result == '"')
                        {
                            quote = !quote;
                            inp = res.Reply.State;
                            builder.Append(res.Reply.Result);
                        }
                        else
                        {
                            inp = res.Reply.State;
                            builder.Append(res.Reply.Result);
                        }
                    }
                    else
                    {
                        return builder.Length == 0
                            ? ParserResult.EmptyError<string>(ParserError.Message(inp.Pos, "invalid code block"))
                            : ParserResult.ConsumedError<string>(ParserError.Message(inp.Pos, "invalid code block"));
                    }
                }
                return ParserResult.ConsumedError<string>(ParserError.Unexpect(inp.Pos, "unexpected end-of-file, missing '}'?")); ;
            };

        /// <summary>
        /// scrape base() and this() blocks between a constructor and its code block
        /// </summary>
        Parser<string> baseThis =>
            inp =>
            {
                var depth = 0;
                var builder = new StringBuilder();
                bool quote = false;

                for (var index = inp.Index; index < inp.EndIndex; index++)
                {
                    var res = anyChar(inp);
                    if (res.Tag == ResultTag.Consumed && res.Reply.Tag == ReplyTag.OK)
                    {
                        if (res.Reply.Result == '{' && depth == 0 && !quote)
                        {
                            return ParserResult.ConsumedOK(builder.ToString(), inp);
                        }
                        else if (res.Reply.Result == '"')
                        {
                            quote = !quote;
                        }
                        else if (res.Reply.Result == '{' || res.Reply.Result == '(')
                        {
                            depth++;
                        }
                        else if (res.Reply.Result == '}' || res.Reply.Result == ')')
                        {
                            depth--;
                        }
                        inp = res.Reply.State;
                        builder.Append(res.Reply.Result);
                    }
                    else
                    {
                        return builder.Length == 0
                            ? ParserResult.EmptyError<string>(ParserError.Message(inp.Pos, "invalid base / this block"))
                            : ParserResult.ConsumedError<string>(ParserError.Message(inp.Pos, "invalid base / this block"));
                    }
                }
                return ParserResult.ConsumedError<string>(ParserError.Unexpect(inp.Pos, "unexpected end-of-file, missing '}'?")); ;
            };


        /// <summary>
        /// scrape a lamdba function
        /// </summary>
        Parser<string> lambdaBlock =>
            inp =>
            {
                var depth = 0;
                var builder = new StringBuilder();
                var quote = false;

                for (var index = inp.Index; index < inp.EndIndex; index++)
                {
                    var res = anyChar(inp);
                    if (res.Tag == ResultTag.Consumed && res.Reply.Tag == ReplyTag.OK)
                    {
                        if (res.Reply.Result == ';' && depth == 0 && !quote)
                        {
                            return ParserResult.ConsumedOK(builder.ToString(), inp);
                        }
                        else if( res.Reply.Result == '"')
                        {
                            quote = !quote;
                        }
                        else if (res.Reply.Result == '{' || res.Reply.Result == '(')
                        {
                            depth++;
                        }
                        else if (res.Reply.Result == '}' || res.Reply.Result == ')')
                        {
                            depth--;
                        }
                        inp = res.Reply.State;
                        builder.Append(res.Reply.Result);
                    }
                    else
                    {
                        return builder.Length == 0
                            ? ParserResult.EmptyError<string>(ParserError.Message(inp.Pos, "invalid lambda block"))
                            : ParserResult.ConsumedError<string>(ParserError.Message(inp.Pos, "invalid lambda block"));
                    }
                }
                return ParserResult.ConsumedError<string>(ParserError.Unexpect(inp.Pos, "unexpected end-of-file, missing ';'?")); ;
            };
    }

    public static class Ext
    {
        public static Parser<SrcToken> asToken<T>(this Parser<T> self) where T : SrcToken =>
            self.Map(x => x as SrcToken);

        public static Parser<CsExpr> asExpr<T>(this Parser<T> self) where T : CsExpr =>
            self.Map(x => x as CsExpr);
    }
}
