using static LanguageExt.Prelude;
using System;
using System.Linq;
using LanguageExt;
using LanguageExt.Parsec;

namespace BestForm.Tokens
{

    public class CsExpr : SrcToken
    {
        public static readonly CsExpr None = new CsExpr();

        public override string ToString() => "";

        public override string GetName() => "";
    }

    public class CodeBlockExpr : CsExpr
    {
        public readonly string Source;
        public readonly Pos Pos;

        public CodeBlockExpr(string source, Pos pos)
        {
            Source = source;
        }
        public override string ToString() => Source;
    }

    public class IfExpr : CsExpr
    {
        public readonly CsExpr BoolExpr;
        public readonly Lst<CsExpr> ThenExpr;
        public readonly Option<Lst<CsExpr>> ElseExpr;

        public IfExpr(CsExpr boolExpr, Lst<CsExpr> thenExpr, Option<Lst<CsExpr>> elseExpr)
        {
            BoolExpr = boolExpr;
            ThenExpr = thenExpr;
            ElseExpr = elseExpr;
        }
    }

    public class CallExpr : CsExpr
    {
        public readonly FQName Name;
        public readonly Lst<CsExpr> Args;

        public Lst<TypeRef> GenericArgs => Name.Idents.Last().GenericArgs;

        public CallExpr(FQName name, Lst<CsExpr> args)
        {
            Name = name;
            Args = args;
        }
    }

    public class TernaryExpr : CsExpr
    {
        public readonly CsExpr BoolExpr;
        public readonly CsExpr ThenExpr;
        public readonly CsExpr ElseExpr;

        public TernaryExpr(CsExpr boolExpr, CsExpr thenExpr, CsExpr elseExpr)
        {
            BoolExpr = boolExpr;
            ThenExpr = thenExpr;
            ElseExpr = elseExpr;
        }
    }

    public class NewExpr : CsExpr
    {
        public readonly TypeRef Type;
        public readonly Lst<CsExpr> Args;

        public NewExpr(TypeRef type, Lst<CsExpr> args)
        {
            Type = type;
            Args = args;
        }
    }

    public class ThrowExpr : CsExpr
    {
        public readonly Option<NewExpr> NewExpr;

        public ThrowExpr(Option<NewExpr> newExpr)
        {
            NewExpr = newExpr;
        }
    }

    public class VarExpr : CsExpr
    {
        public FQName Name;

        public VarExpr(FQName name)
        {
            Name = name;
        }
    }

    public class NoExpr : CsExpr
    {
    }

    public class ReturnExpr : CsExpr
    {
        public readonly TypeDef Type;
        public readonly Lst<CsExpr> Args;

        public ReturnExpr(TypeDef type, Lst<CsExpr> args)
        {
            Type = type;
            Args = args;
        }
    }

    public class BinaryExpr : CsExpr
    {
        public readonly CsExpr Left;
        public readonly CsExpr Right;
        public readonly string Op;

        public BinaryExpr(CsExpr left, CsExpr right, string op)
        {
            Left = left;
            Right = right;
            Op = op;
        }
    }

    public class UnaryExpr : CsExpr
    {
        public readonly CsExpr Expr;
        public readonly string Op;
        public readonly bool Prefix;

        public UnaryExpr(CsExpr expr, string op, bool prefix)
        {
            Expr = expr;
            Op = op;
            Prefix = prefix;
        }
    }

    public class VarAssign : CsExpr
    {
        public readonly FQName Name;
        public readonly CsExpr RHS;

        public VarAssign(FQName name, CsExpr rhs)
        {
            Name = name;
            RHS = rhs;
        }
    }

    public class VarDecl : CsExpr
    {
        public readonly TypeRef Type;
        public readonly Identifier Name;
        public readonly Option<CsExpr> Assign;

        public VarDecl(TypeRef type, Identifier name, Option<CsExpr> assign)
        {
            Type = type;
            Name = name;
            Assign = assign;
        }
    }
}
