using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Parsec;
using LanguageExt.Pretty;
using Microsoft.CodeAnalysis;
using static LanguageExt.Prelude;
using Microsoft.CodeAnalysis.CSharp;
using static LanguageExt.Parsec.Char;
using static LanguageExt.Parsec.Prim;
using static LanguageExt.Parsec.Token;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BestForm
{
    public class ReadComments
    {
        /// <summary>
        /// Read all the document elements we care about from the source folders
        /// </summary>
        public static async Task<Project> FromProject(Project project) =>
            project with {Data = await FromFolder(project, project.Root)};
        
        /// <summary>
        /// Read all the document elements we care about from the source folder
        /// </summary>
        public static async Task<ProjectData> FromFolder(Project project, string path)
        {
            if (project.FoldersToExclude.Contains(Path.GetFileName(path).ToLower())) return ProjectData.Empty; 
            var files = Directory.GetFiles(path, "*.cs");
            var data1 = (await files.SequenceParallel(p => FromFile(project, p))).ToSeq().Strict();
            var folders = Directory.GetDirectories(path);
            var data2 = (await folders.SequenceParallel(f => FromFolder(project, f))).ToSeq().Strict();
            return (data1 + data2).Fold(ProjectData.Empty, (s, d) => s with {Files = s.Files + d.Files});
        }

        /// <summary>
        /// Read all the document elements we care about from the source file
        /// </summary>
        public static async Task<ProjectData> FromFile(Project project, string path)
        {
            var src = await System.IO.File.ReadAllTextAsync(path);
            return FromSource(path.Substring(project.Root.Length + 1), src, project);
        }
        
        /// <summary>
        /// Read all the document elements we care about from the source text
        /// </summary>
        static ProjectData FromSource(string path, string src, Project project)
        {
            var tree    = SyntaxFactory.ParseSyntaxTree(src, path: path);
            
            var enums      = FromMembers<EnumDeclarationSyntax>(tree);
            var records    = FromMembers<RecordDeclarationSyntax>(tree);
            var classes    = FromMembers<ClassDeclarationSyntax>(tree);
            var structs    = FromMembers<StructDeclarationSyntax>(tree);
            var delegates  = FromMembers<DelegateDeclarationSyntax>(tree);
            var interfaces = FromMembers<InterfaceDeclarationSyntax>(tree);

            return ProjectData.Empty with {Files = HashMap<string, File>((path, new File(enums + delegates + records + structs + interfaces + classes)))};
        }
        
        /// <summary>
        /// Get all documentation and member names
        /// </summary>
        static Seq<Member> FromMembers<MEM>(SyntaxTree tree) where MEM : MemberDeclarationSyntax =>
            tree.GetRoot()
                .DescendantNodes()
                .Filter(m => m is MEM)
                .Filter(m => ((MEM)m).Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword))
                .Map(FromMember)
                .ToSeq()
                .Strict();

        /// <summary>
        /// Extract the documentation, name, and child member documentation
        /// </summary>
        static Member FromMember(SyntaxNode node)
        {
            var memberDecls = node.ChildNodes()
                                  .Filter(m => m is MemberDeclarationSyntax && 
                                               m is not EventFieldDeclarationSyntax && 
                                               m is not ClassDeclarationSyntax && 
                                               m is not InterfaceDeclarationSyntax && 
                                               m is not StructDeclarationSyntax && 
                                               m is not DelegateDeclarationSyntax &&
                                               m is not EnumDeclarationSyntax &&
                                               m is not RecordDeclarationSyntax &&
                                               m is not ConversionOperatorDeclarationSyntax)
                                  .Map(m => (MemberDeclarationSyntax) m)
                                  .Filter(m => m.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword))
                                  .ToSeq()
                                  .Strict();

            var members = memberDecls.Map(FromMember).Strict();

            var mnode = (MemberDeclarationSyntax) node;
            var name  = NameFromDeclaration(mnode);

            var comments = node.GetLeadingTrivia()
                               .Filter(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                                            t.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia)
                               .Map(t => ParseComment(t.GetLocation(), t.ToString()))
                               .ToSeq()
                               .Strict();
            

            return new Member(name, mnode, Merge(comments), members);
        }

        /// <summary>
        /// Get the name of the member
        /// </summary>
        public static string NameFromDeclaration(MemberDeclarationSyntax? m) =>
            m switch
            {
                null                           => "",
                ClassDeclarationSyntax c       => c.Identifier.Text,
                InterfaceDeclarationSyntax c   => c.Identifier.Text,
                StructDeclarationSyntax c      => c.Identifier.Text,
                DelegateDeclarationSyntax c    => c.Identifier.Text,
                EnumDeclarationSyntax c        => c.Identifier.Text,
                RecordDeclarationSyntax c      => c.Identifier.Text,
                MethodDeclarationSyntax c      => c.Identifier.Text,
                FieldDeclarationSyntax c       => c.Declaration.Variables.ToString(),
                PropertyDeclarationSyntax c    => c.Identifier.Text,
                ConstructorDeclarationSyntax c => $"{NameFromDeclaration(m.Parent as MemberDeclarationSyntax)}",
                OperatorDeclarationSyntax c    => $"{c.OperatorToken}",
                EventDeclarationSyntax c       => c.Identifier.Text,
                IndexerDeclarationSyntax c     => "this",
                NamespaceDeclarationSyntax c   => c.Name.ToString(),
                _                              => "",
            };

        /// <summary>
        /// Merge a sequence of documents into one
        /// </summary>
        static Document Merge(Seq<Document> docs) =>
            docs.IsEmpty
                ? Document.Empty 
                : new Document(docs.Head.Loc, docs.Tail.Fold(docs.Head.Sections, (s, x) => s + x.Sections));

        /// <summary>
        /// Parse an entire XML documentation comment into a Document
        /// </summary>
        /// <remarks>
        /// Always succeeds, but will print any errors to the console
        /// </remarks>
        static Document ParseComment(Location loc, string text)
        {
            var lines = Lines(text);
            lines = lines.Map(RemoveLeadingToken);
            text  = string.Join("\n", lines);
            
            var doc = document.Parse(text);
            if (doc.IsFaulted)
            {
                var err  = doc.Reply.Error.ToStringNoPosition();
                var line = loc.GetLineSpan().StartLinePosition.Line + doc.Reply.Error.Pos.Line + 1;
                var col  = doc.Reply.Error.Pos.Column + 1;
                Console.WriteLine($"{loc.GetLineSpan().Path}: error ({line}, {col}): {err}");
                return new Document(loc, Seq1(new Text(text) as Section));
            }
            else
            {
                return doc.Reply.Result with {Loc = loc};
            }
        }

        /// <summary>
        /// Split the text into lines
        /// </summary>
        static Seq<string> Lines(string text) =>
            text.Split('\n').ToSeq();

        /// <summary>
        /// Quickly shave off the leading whitespace and comment token
        /// </summary>
        static string RemoveLeadingToken(string text)
        {
            text = text.Trim();
            text = text.StartsWith("/// ") ? text.Substring(4) : text;
            text = text.StartsWith("///") ? text.Substring(3) : text;
            return text;
        }

        /// <summary>
        /// Convert from SectionType to text  
        /// </summary>
        static string tagText(SectionType type) =>
            type switch
            {
                SectionType.Param     => "param",
                SectionType.TypeParam => "typeparam",
                SectionType.Summary   => "summary",
                SectionType.Returns   => "returns",
                SectionType.Remarks   => "remarks",
                SectionType.Paragraph => "para",
                SectionType.Code      => "code",
                SectionType.Code2     => "c",
                SectionType.Example   => "example",
                SectionType.Exception => "exception",
                _                     => throw new NotSupportedException()
            };

        /// <summary>
        /// Convert from text to SectionType 
        /// </summary>
        static SectionType textTag(string type) =>
            type switch
            {
                "param"     => SectionType.Param,
                "typeparam" => SectionType.TypeParam,
                "summary"   => SectionType.Summary,
                "returns"   => SectionType.Returns,
                "remarks"   => SectionType.Remarks,
                "para"      => SectionType.Paragraph,
                "code"      => SectionType.Code,
                "c"         => SectionType.Code2,
                "example"   => SectionType.Example,
                "exception" => SectionType.Exception,
                _           => throw new NotSupportedException()
            };

        /// <summary>
        /// Supported XML tag keywords
        /// </summary>
        readonly static HashSet<string> keywords = 
            HashSet("param", "typeparam", "summary", "returns", "remarks", "para", "code", "c", "example", "exception");

        /// <summary>
        /// Parse a token
        /// </summary>
        static Parser<A> token<A>(Parser<A> p) =>
            from x in p
            from _ in spaces
            select x;

        /// <summary>
        /// Parse a symbol
        /// </summary>
        static Parser<string> symbol(string x) =>
            token(str(x)).label(x);

        /// <summary>
        /// Parse an identifier, as long as it doesn't match a keyword
        /// </summary>
        static readonly Parser<string> ident = token(from id in asString(many1(letter))
                                                     from rs in keywords.Contains(id)
                                                                    ? unexpected<string>("keyword")
                                                                    : result(id)
                                                     select id)
                                                    .label("identifier");

        /// <summary>
        /// Parse a any keyword (keywords are the known XML document tags)
        /// </summary>
        static readonly Parser<string> keyword = token(from kw in asString(many1(letter))
                                                       from rs in keywords.Contains(kw)
                                                                      ? result(kw)
                                                                      : unexpected<string>("identifier")
                                                       select rs)
                                                      .label($"{string.Join(", ", keywords)}");

        /// <summary>
        /// Parse a known keyword (keywords are the known XML document tags)
        /// </summary>
        static Parser<string> keywordExact(string kw) =>
            token(from _ in symbol(kw)
                  from x in notFollowedBy(letter)
                  select kw)
               .label(kw);

        /// <summary>
        /// Parse a single XML attibute
        /// </summary>
        static readonly Parser<Attr> attribute = (from id in ident
                                                  from eq in symbol("=")
                                                  from op in symbol("\"")
                                                  from vl in asString(many(satisfy(ch => ch != '"')))
                                                  from cl in symbol("\"")
                                                  select new Attr(id, vl))
                                                 .label("attribute");

        /// <summary>
        /// Parse XML attributes
        /// </summary>
        static readonly Parser<Seq<Attr>> attributes = many(attribute);

        /// <summary>
        /// Parses any text, as long as it's not the less-than token
        /// </summary>
        static readonly Parser<Section> text = 
            asString(many1(satisfy(ch => ch != '<')))
               .Map(static t => new Text(t) as Section)
               .label("documentation text");
 
        /// <summary>
        /// Parses a single less-than token, that is not followed by '/'
        /// </summary>
        static readonly Parser<Section> lessThan = 
            (from lt in ch('<')
             from _ in notFollowedBy(ch('/'))
             select new Text("<") as Section)
            .label("'<' not followed by a '/'");

        /// <summary>
        /// Parses an XML tag
        /// </summary>
        static readonly Parser<Section> tag = token(from opn in symbol("<")
                                                    from kw1 in keyword
                                                    from attrs in attributes
                                                    from cls in str(">")
                                                    from val in innerValue
                                                    from op1 in symbol("</")
                                                    from kw2 in keywordExact(kw1)
                                                    from cl1 in symbol(">")
                                                    select new Tag(textTag(kw1), attrs, val) as Section)
                                                   .label("XML documentation tag");

        /// <summary>
        /// Parses the inner section of an XML element
        /// </summary>
        static Parser<Seq<Section>> innerValue =>
            many(choice(attempt(tag),
                        attempt(text),
                        attempt(lessThan)))
               .Map(joinText);

        /// <summary>
        /// Document parser
        /// </summary>
        /// <remarks>Parses complete XML documentation, with special care for rogue tag-opening tokens</remarks>
        static readonly Parser<Document> document = from ts in innerValue
                                                    from _ in eof
                                                    select new Document(Location.None, ts);

        /// <summary>
        /// Aggregates adjacent text sections 
        /// </summary>
        /// <param name="sections">Sections</param>
        /// <returns>Seq of sections with the Text sections aggregated</returns>
        static Seq<Section> joinText(Seq<Section> sections)
        {
            if (sections.IsEmpty) return sections;
            if (sections.Tail.IsEmpty) return sections;
            var nsections = new System.Collections.Generic.List<Section>();

            // ugly, but fast
            foreach (var s in sections)
            {
                if (s is Text t1)
                {
                    if (nsections.Count > 0 && nsections[^1] is Text t2)
                    {
                        nsections[^1] = new Text(t2.Value + t1.Value);
                    }
                    else
                    {
                        nsections.Add(s);
                    }
                }
                else
                {
                    nsections.Add(s);
                }
            }
            return Seq(nsections);
        }

        /// <summary>
        /// Breakpoint parser, because Rider can't break on LINQ 
        /// </summary>
        /// <param name="f"></param>
        /// <typeparam name="A"></typeparam>
        /// <returns></returns>
        static Parser<A> bp<A>(Func<PString, A> f) =>
            new Parser<A>(inp => ParserResult.EmptyOK<A>(f(inp), inp, null));
    }
}