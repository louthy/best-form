using LanguageExt;
using static LanguageExt.Prelude;
using System;
using System.Linq;

namespace BestForm.Tokens
{
    public enum Vis
    {
        Public,
        Private,
        Protected,
        Internal
    }

    public enum TypeDefTag
    {
        Class,
        Struct,
        Interface
    }

    public abstract class SrcToken
    {
    }

    public class SourceFile : Namespace
    {
        public SourceFile(Lst<Using> usings, Lst<UsingAlias> usingAliases, Lst<Namespace> namespaces, Lst<TypeDef> types, Lst<EnumDef> enums, Lst<DelegateDef> delegates)
            :
            base(new FQName(List<Identifier>()), usings, usingAliases, namespaces, types, enums, delegates)
        {
        }
    }

    public static class Keywords
    {
        public static readonly Set<string> All = Set(new[] {
            "ctor","finalise","var","abstract","as","base",
            "bool","break","byte","case","catch","char","checked","class",
            "const","continue","decimal","default","delegate","do","double",
            "else","enum","event","explicit","extern","false","finally",
            "fixed","float","for","foreach","goto","if","implicit","in",
            "int","interface","internal","is","lock","long","namespace","new",
            "null","object","operator","out", "override", "params", "private",
            "protected","public","readonly","ref","return","sbyte","sealed",
            "short","sizeof","stackalloc","static","string","struct","switch",
            "this","throw","true","try","typeof","uint","ulong","unchecked",
            "unsafe","ushort","using","virtual","void","volatile","while"
        });
    }

    public class Identifier : SrcToken
    {
        public readonly string Name;
        public readonly Lst<TypeRef> GenericArgs;
        public readonly bool IsKeyword;

        public Identifier(string name)
        {
            Name = name;
            GenericArgs = List<TypeRef>();
            IsKeyword = Keywords.All.Contains(name);
        }

        public Identifier(string name, Lst<TypeRef> genericArgs)
        {
            Name = name;
            GenericArgs = genericArgs;
            IsKeyword = Keywords.All.Contains(name);
        }

        private string GenericStr =>
            GenericArgs.Count == 0
                ? ""
                : $"<{System.String.Join(", ", GenericArgs)}>";

        public override string ToString() => $"{Name}{GenericStr}";

        public string UniqueName =>
            $"{Name}_" + String.Join("_", GenericArgs.Map(ga => ga.Name.ToString()));
    }

    public class FQName : SrcToken
    {
        public readonly Lst<Identifier> Idents;

        public FQName(Lst<Identifier> idents)
        {
            Idents = idents;
        }

        public override string ToString() => String.Join(".", Idents);

        public string UniqueName => String.Join(".", Idents.Map(x => x.UniqueName));
    }

    public class ArrayDef : SrcToken
    {
        public readonly int Dimensions;
        public readonly bool Inner;

        public ArrayDef(int dimensions, bool inner)
        {
            Dimensions = dimensions;
            Inner = inner;
        }

        public override string ToString() =>
            Inner
                ? $"[{ String.Join("", LanguageExt.List.repeat(",", Dimensions - 1)) }]"
                : String.Join("", LanguageExt.List.repeat("[]", Dimensions - 1));
    }

    public class TypeRef : SrcToken
    {
        public readonly FQName Name;
        public readonly Option<ArrayDef> ArrayDef;
        public readonly bool IsNullable;
        public readonly bool IsIn;
        public readonly bool IsOut;

        public Lst<TypeRef> GenericArgs =>
            Name.Idents.Last().GenericArgs;

        public TypeRef(FQName name, Option<ArrayDef> arrayDef, bool isNullable, bool isIn, bool isOut)
        {
            Name = name;
            ArrayDef = arrayDef;
            IsNullable = isNullable;
            IsIn = isIn;
            IsOut = isOut;
        }

        public override string ToString() =>
            $"{Name}{NullStr}{ArrayStr}";

        private string NullStr =>
            IsNullable
                ? "?"
                : "";

        private string ArrayStr =>
            ArrayDef.Map(x => x.ToString()).IfNone("");

        private static Lst<TypeRef> NoArgs = List<TypeRef>();

        public static readonly TypeRef Final = new TypeRef(new FQName(List(new Identifier("finalise"))), None, false, false, false);
        public static readonly TypeRef Ctor = new TypeRef(new FQName(List(new Identifier("ctor"))), None, false, false, false);
        public static readonly TypeRef Var = new TypeRef(new FQName(List(new Identifier("var"))), None, false, false, false);

        public static readonly TypeRef Char = new TypeRef(new FQName(List(new Identifier("char"))), None, false, false, false);
        public static readonly TypeRef Short = new TypeRef(new FQName(List(new Identifier("short"))), None, false, false, false);
        public static readonly TypeRef Int = new TypeRef(new FQName(List(new Identifier("int"))), None, false, false, false);
        public static readonly TypeRef Long = new TypeRef(new FQName(List(new Identifier("long"))), None, false, false, false);

        public static readonly TypeRef Byte = new TypeRef(new FQName(List(new Identifier("byte"))), None, false, false, false);
        public static readonly TypeRef UByte = new TypeRef(new FQName(List(new Identifier("ubyte"))), None, false, false, false);
        public static readonly TypeRef UShort = new TypeRef(new FQName(List(new Identifier("ushort"))), None, false, false, false);
        public static readonly TypeRef UInt = new TypeRef(new FQName(List(new Identifier("uint"))), None, false, false, false);
        public static readonly TypeRef ULong = new TypeRef(new FQName(List(new Identifier("ulong"))), None, false, false, false);

        public static readonly TypeRef Double = new TypeRef(new FQName(List(new Identifier("double"))), None, false, false, false);
        public static readonly TypeRef Float = new TypeRef(new FQName(List(new Identifier("float"))), None, false, false, false);
        public static readonly TypeRef Decimal = new TypeRef(new FQName(List(new Identifier("decimal"))), None, false, false, false);

        public static readonly TypeRef String = new TypeRef(new FQName(List(new Identifier("string"))), None, false, false, false);
        public static readonly TypeRef Bool = new TypeRef(new FQName(List(new Identifier("bool"))), None, false, false, false);

        public string UniqueName => Name.UniqueName;

        public static TypeRef ValueTuple(Lst<(TypeRef Type, Option<Identifier> Name)> items, Option<ArrayDef> arrayDef, bool isNullable) =>
            new TypeRef(new FQName(List(new Identifier("ValueTuple", items.Map(x => x.Type)))), arrayDef, isNullable, false, false);
    }

    public class AttributeDef : SrcToken
    {
        public readonly string Body;

        public AttributeDef(string body)
        {
            Body = body;
        }
    }

    public class Using : SrcToken
    {
        public readonly bool IsStatic;
        public readonly FQName Name;

        public Using(bool isStatic, FQName name)
        {
            IsStatic = isStatic;
            Name = name;
        }

        public override string ToString() => Name.ToString();
    }

    public class UsingAlias : SrcToken

    {

        public readonly FQName Source;

        public readonly Identifier Alias;



        public UsingAlias(FQName source, Identifier alias)

        {

            Source = source;

            Alias = alias;

        }

    }

    public class EnumMember : SrcToken
    {
        public readonly Identifier Name;
        public readonly Option<int> Value;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;

        public EnumMember(Identifier name, Option<int> value, Option<Document> document, Lst<AttributeDef> attributes)
        {
            Name = name;
            Value = value;
            Attributes = attributes;
            Document = document;
        }

        public override string ToString() => Name.ToString();
    }

    public class EnumDef : SrcToken
    {
        public readonly Identifier Name;
        public readonly Lst<EnumMember> Members;
        public readonly TypeRef Type;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;
        public readonly Vis Visibility;

        public EnumDef(Identifier name, Lst<EnumMember> members, TypeRef type, Option<Document> document, Lst<AttributeDef> attributes, Vis visibility)
        {
            Type = type;
            Name = name;
            Members = members;
            Document = document;
            Attributes = attributes;
            Visibility = visibility;
        }

        public override string ToString() =>
            $"{Visibility.ToString().ToLower()} enum {Name} : {Type}";
    }

    public class Constant : SrcToken
    {
        public TypeRef Type;
        public string Value;

        public Constant(TypeRef type, string value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString() => Value;
    }

    public class DelegateArg : SrcToken
    {
        public readonly TypeRef Type;
        public readonly Option<Identifier> Name;

        public DelegateArg(TypeRef type, Option<Identifier> name)
        {
            Type = type;
            Name = name;
        }

        public override string ToString() =>
            $"{Type} {Name.Map(x => x.ToString()).IfNone("")}";
    }

    public class Arg : SrcToken
    {
        public readonly bool IsParams;
        public readonly bool IsThis;
        public readonly bool IsOut;
        public readonly bool IsRef;
        public readonly TypeRef Type;
        public readonly Identifier Name;
        public readonly Option<Constant> Default;
        public readonly Lst<AttributeDef> Attributes;

        public Arg(TypeRef type, Identifier name, bool isThis, bool isParams, bool isOut, bool isRef, Option<Constant> defaultValue, Lst<AttributeDef> attributes)
        {
            Type = type;
            Name = name;
            IsThis = isThis;
            IsParams = isParams;
            IsOut = isOut;
            IsRef = isRef;
            Default = defaultValue;
            Attributes = attributes;
        }

        public override string ToString() =>
            $"{Type} {Name}";

        public string UniqueName =>
            $"{Type.UniqueName}_{Name.UniqueName}";
    }

    public class DelegateDef : SrcToken
    {
        public readonly Identifier Name;
        public readonly TypeRef Type;
        public readonly Lst<DelegateArg> Args;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;
        public readonly Vis Visibility;

        public Lst<TypeRef> GenericArgs => Name.GenericArgs;

        public DelegateDef(Identifier name, TypeRef returnType, Lst<DelegateArg> args, Vis visibility, Option<Document> document, Lst<AttributeDef> attributes)
        {
            Name = name;
            Type = returnType;
            Args = args;
            Visibility = visibility;
            Document = document;
            Attributes = attributes;
        }

        string ArgsStr => String.Join(", ", Args);

        public override string ToString() =>
            $"{Visibility.ToString().ToLower()} {Type} {Name}({ArgsStr})";

        public string UniqueName =>
            Name.UniqueName;
    }

    public class FieldDef : SrcToken
    {
        public readonly Vis Visibility;
        public readonly TypeRef Type;
        public readonly Identifier Name;
        public readonly CsExpr Source;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;
        public readonly bool IsReadOnly;
        public readonly bool IsStatic;
        public readonly bool IsEvent;
        public readonly bool IsVolatile;

        public FieldDef(TypeRef type, Identifier name, Vis vis, CsExpr source, Lst<AttributeDef> attributes, Option<Document> document, bool isReadOnly, bool isStatic, bool isEvent, bool isVolatile)
        {
            Type = type;
            Name = name;
            Visibility = vis;
            Source = source;
            Attributes = attributes;
            Document = document;
            IsReadOnly = isReadOnly;
            IsStatic = isStatic;
            IsEvent = isEvent;
            IsVolatile = isVolatile;
        }

        string StaticStr => IsStatic ? "static " : "";
        string ReadOnlyStr => IsReadOnly ? "readonly " : "";

        public override string ToString() =>
            $"{Visibility.ToString().ToLower()} {StaticStr}{ReadOnlyStr}{Type} {Name}";
    }

    public class ConvOperatorDef : SrcToken
    {
        public readonly Vis Visibility;
        public readonly TypeRef ToType;
        public readonly Arg FromArg;
        public readonly CsExpr Source;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;
        public bool Implicit;

        public ConvOperatorDef(bool impl, TypeRef toType, Arg fromArg, Vis vis, CsExpr source, Lst<AttributeDef> attributes, Option<Document> document)
        {
            Implicit = impl;
            ToType = toType;
            FromArg = fromArg;
            Visibility = vis;
            Source = source;
            Attributes = attributes;
            Document = document;
        }

        string ExplitStr =>
            Implicit ? "implicit" : "explict";
    }

    public class BinaryOperatorDef : SrcToken
    {
        public readonly string Op;
        public readonly Vis Visibility;
        public readonly TypeRef Type;
        public readonly Arg ArgLHS;
        public readonly Arg ArgRHS;
        public readonly CsExpr Source;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;

        public BinaryOperatorDef(string op, TypeRef type, Arg lhs, Arg rhs, Vis vis, CsExpr source, Lst<AttributeDef> attributes, Option<Document> document)
        {
            Op = op;
            Type = type;
            ArgLHS = lhs;
            ArgRHS = rhs;
            Visibility = vis;
            Source = source;
            Attributes = attributes;
            Document = document;
        }
    }

    public class UnaryOperatorDef : SrcToken
    {
        public readonly string Op;
        public readonly Vis Visibility;
        public readonly TypeRef Type;
        public readonly Arg Arg;
        public readonly Identifier Name;
        public readonly CsExpr Source;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;

        public UnaryOperatorDef(string op, TypeRef type, Arg arg, Vis vis, CsExpr source, Lst<AttributeDef> attributes, Option<Document> document)
        {
            Op = op;
            Type = type;
            Arg = arg;
            Visibility = vis;
            Source = source;
            Attributes = attributes;
            Document = document;
        }
    }

    public class ConstDef : SrcToken
    {
        public readonly Vis Visibility;
        public readonly TypeRef Type;
        public readonly Identifier Name;
        public readonly CsExpr Source;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;

        public ConstDef(TypeRef type, Identifier name, Vis vis, CsExpr source, Lst<AttributeDef> attributes, Option<Document> document)
        {
            Type = type;
            Name = name;
            Visibility = vis;
            Source = source;
            Attributes = attributes;
            Document = document;
        }

        public override string ToString() =>
            $"{Visibility.ToString().ToLower()} {Type} {Name} = {Source}";
    }

    public class PropDef : SrcToken
    {
        public readonly Vis Visibility;
        public readonly TypeRef Type;
        public readonly FQName Name;
        public readonly CsExpr SetSource;
        public readonly CsExpr GetSource;
        public readonly CsExpr AssignSource;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;
        public readonly bool IsStatic;
        public readonly bool IsAbstract;
        public readonly bool IsVirtual;
        public readonly bool IsOverride;
        public readonly bool IsNew;
        public readonly Option<Arg> IndexerArg;

        public PropDef(
            TypeRef type,
            FQName name,
            Vis vis,
            CsExpr getSource,
            CsExpr setSource,
            CsExpr assignSource,
            Lst<AttributeDef> attributes,
            Option<Document> document,
            bool isStatic,
            bool isAbstract,
            bool isVirtual,
            bool isOverride,
            bool isNew, 
            Option<Arg> indexerArg
            )
        {
            Type = type;
            Name = name;
            Visibility = vis;
            SetSource = setSource;
            GetSource = getSource;
            AssignSource = assignSource;
            Attributes = attributes;
            Document = document;
            IsStatic = isStatic;
            IsAbstract = isAbstract;
            IsVirtual = isVirtual;
            IsOverride = isOverride;
            IsNew = isNew;
            IndexerArg = indexerArg;
        }

        string StaticStr => IsStatic ? "static " : "";
        string AbstractStr => IsAbstract ? "abstract " : "";
        string VirtualStr => IsVirtual ? "virtual " : "";
        string OverrideStr => IsOverride ? "override " : "";
        string NewStr => IsNew ? "new " : "";
        string GetStr => GetSource is CodeBlockExpr ? "get; " : "";
        string SetStr => SetSource is CodeBlockExpr ? "set; " : "";

        public override string ToString() =>
            $"{Visibility.ToString().ToLower()} {StaticStr}{AbstractStr}{VirtualStr}{OverrideStr}{NewStr}{Type} {Name} {{{GetStr}{SetStr}}}";
    }

    public class Constraint : SrcToken
    {
        public readonly Identifier What;
        public readonly Lst<ConstrainedBy> By;

        public Constraint(Identifier what, Lst<ConstrainedBy> by)
        {
            What = what;
            By = by;
        }

        public override string ToString() =>
            $"where {What} : {ByStr}";

        string ByStr =>
            String.Join(", ", By);
    }

    public class ConstrainedBy : SrcToken
    {
        public readonly string With;
        public readonly Option<TypeRef> Type;

        public ConstrainedBy(string with, Option<TypeRef> type)
        {
            With = with;
            Type = type;
        }

        public override string ToString() =>
            With == "type"
                ? Type.IfNone(() => null).ToString()
                : With;
    }


    public class MethodDef : SrcToken
    {
        public readonly Vis Visibility;
        public readonly TypeRef Type;
        public readonly FQName Name;
        public readonly CsExpr Source;
        public readonly Option<Document> Document;
        public readonly Lst<AttributeDef> Attributes;
        public readonly Lst<Arg> Args;
        public readonly bool IsStatic;
        public readonly bool IsAbstract;
        public readonly bool IsVirtual;
        public readonly bool IsOverride;
        public readonly bool IsNew;
        public readonly bool IsAsync;
        public readonly Lst<Constraint> Constraints;


        public MethodDef(
            TypeRef type,
            FQName name,
            Lst<Arg> args,
            Vis vis,
            CsExpr source,
            Lst<AttributeDef> attributes,
            Option<Document> document,
            Lst<Constraint> constraints,
            bool isStatic,
            bool isAbstract,
            bool isVirtual,
            bool isOverride,
            bool isNew,
            bool isAsync
            )
        {
            Type = type;
            Name = name;
            Visibility = vis;
            Source = source;
            Args = args;
            Attributes = attributes;
            Document = document;
            IsStatic = isStatic;
            IsAbstract = isAbstract;
            IsVirtual = isVirtual;
            IsOverride = isOverride;
            IsNew = isNew;
            IsAsync = isAsync;
            Constraints = constraints;
        }

        public Lst<TypeRef> GenericArgs => Name.Idents.Last().GenericArgs;

        string ArgsStr => String.Join(", ", Args);
        string StaticStr => IsStatic ? "static " : "";
        string AbstractStr => IsAbstract ? "abstract " : "";
        string VirtualStr => IsVirtual ? "virtual " : "";
        string OverrideStr => IsOverride ? "override " : "";
        string NewStr => IsNew ? "new " : "";
        string AsyncStr => IsAsync ? "async " : "";

        public override string ToString() =>
            $"{Visibility.ToString().ToLower()} {StaticStr}{AsyncStr}{AbstractStr}{VirtualStr}{OverrideStr}{NewStr}{Type} {Name}({ArgsStr})";

        public string UniqueName => $"{Name.UniqueName}_{String.Join("_",Args.Map(x => x.UniqueName))}";
    }

    public class TypeDef : SrcToken
    {
        public readonly TypeDefTag Type;
        public readonly Identifier Name;
        public readonly bool IsStatic;
        public readonly bool IsPartial;
        public readonly bool IsAbstract;
        public readonly bool IsSealed;
        public readonly Vis Visibility;
        public readonly Lst<TypeRef> DerivedFrom;
        public readonly Option<Document> Document;
        public readonly Lst<SrcToken> Members;
        public readonly Lst<AttributeDef> Attributes;
        public readonly Lst<Constraint> Constraints;

        public TypeDef(TypeDefTag type, Identifier name, Vis visibility, Lst<TypeRef> derivedFrom, Lst<SrcToken> members, bool isStatic, bool isPartial, bool isAbstract, bool isSealed, Option<Document> document, Lst<AttributeDef> attributes, Lst<Constraint> constraints)
        {
            Type = type;
            Name = name;
            IsStatic = isStatic;
            IsPartial = isPartial;
            IsAbstract = isAbstract;
            IsSealed = isSealed;
            Visibility = visibility;
            DerivedFrom = derivedFrom;
            Members = members;
            Document = document;
            Attributes = attributes;
            Constraints = constraints;
        }

        public Lst<TypeRef> GenericArgs => Name.GenericArgs;

        public override string ToString() =>
            $"{Name}";

        public string UniqueName =>
            Name.UniqueName;
    }

    public class Namespace : SrcToken
    {
        public readonly FQName Name;
        public readonly Lst<Using> Usings;
        public readonly Lst<UsingAlias> UsingAliases;
        public readonly Lst<Namespace> Namespaces;
        public readonly Lst<TypeDef> Types;
        public readonly Lst<EnumDef> Enums;
        public readonly Lst<DelegateDef> Delegates;

        public Namespace(FQName name, Lst<Using> usings, Lst<UsingAlias> usingAliases, Lst<Namespace> namespaces, Lst<TypeDef> types, Lst<EnumDef> enums, Lst<DelegateDef> delegates)
        {
            Name = name;
            Usings = usings;
            UsingAliases = usingAliases;
            Namespaces = namespaces;
            Types = types;
            Enums = enums;
            Delegates = delegates;
        }

        public Namespace MergeTypes()
        {
            var types = LanguageExt.List.fold(Types, Map<string, TypeDef>(), (state, type) =>
            {
                var tn = type.ToString();
                return state.ContainsKey(tn)
                    ? state.AddOrUpdate(tn, Merge(state[tn], type))
                    : state.Add(tn, type);
            });

            return new Namespace(Name, Usings, UsingAliases, Namespaces, types.Values.Freeze(), Enums, Delegates);
        }

        /// <summary>
        /// Marges partial type
        /// </summary>
        private static TypeDef Merge(TypeDef lhs, TypeDef rhs) =>
            new TypeDef(
                lhs.Type,
                lhs.Name,
                lhs.Visibility,
                lhs.DerivedFrom,
                lhs.Members + rhs.Members,
                lhs.IsStatic,
                lhs.IsPartial,
                lhs.IsAbstract,
                lhs.IsSealed,
                lhs.Document,
                lhs.Attributes + rhs.Attributes,
                lhs.Constraints + rhs.Constraints
                );

        public override string ToString() => Name.ToString();
    }

    public class Document : SrcToken
    {
        public readonly DocumentComments Comments;

        public Document(DocumentComments comments)
        {
            Comments = comments;
        }
    }
}
