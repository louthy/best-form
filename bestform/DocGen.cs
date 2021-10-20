using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using LanguageExt;
using Markdig;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LanguageExt.Prelude;
using F = System.IO.File;

namespace BestForm
{
    public static class DocGen
    {
        public static Project Run(Project project)
        {
            project = project with { Folder = MakeFolder(project, Folder.Empty, 1) };

            CreateFolders(project, project.Name, project.Folder, 1);
            
            return project;
        }

        /// <summary>
        /// Walk the folder data structure and create the real folders
        /// </summary>
        static Unit CreateFolders(Project project, string name, Folder folder, int depth)
        {
            if (folder.FileCount == 0) return unit;
            var path = Path.Combine(project.TargetFolder, name);
            Directory.CreateDirectory(path);
            foreach (var f in folder.Folders)
            {
                CreateFolders(project, Path.Combine(name, f.Key), f.Value, depth + 1);
            }

            CreateDocPage(project, name, folder, depth);

            return unit;
        }

        static Unit CreateDocPage(Project project, string name, Folder folder, int depth)
        {
            var urlRoot = GetWebName(project, name);
            var path    = Path.Combine(project.TargetFolder, name);
            var head    = Html.head(name);
            var header  = Html.header(project.Name, urlRoot, "../index.html", GetRoot(depth));
            var readMe  = folder.ReadMe;

            var content = Html.div("content",
                                   MakeModuleHeader(name),
                                   readMe == "" ? Html.empty : MarkdownText(readMe),
                                   MakeTableOfContents(project, folder),
                                   Html.div2("interface",
                                             MakeModuleLinks(project, folder),
                                             MakeMemberDocuments(project, folder, urlRoot)));

            var page = Html.html(
                head, 
                Html.body(
                    header,
                    content));
 
            var index = Path.Combine(path, "index.html");
            var text  = Html.render(page);
            F.WriteAllText(index, text);
            F.WriteAllText(Path.Combine(path, "style.css"), project.CSS);
            F.WriteAllBytes(Path.Combine(path, "logo.png"), project.Logo);
            return unit;
        }

        static Html MakeModuleHeader(string name)
        {
            var parts = name.Split('\\');

            var hdr = parts.Fold((Seq<Html>(), parts.Length - 1),
                                 (h, p) =>
                                     (h.Item1.Add(Html.a(p, string.Concat(LanguageExt.List.repeat("../", h.Item2)) + "index.html")), h.Item2 - 1))
                           .Item1
                           .Intersperse(Html.span(Html.text(" ► ")))
                           .Join();

            return Html.moduleHeader(hdr);
        }

        static Html MakeModuleLinks(Project project, Folder folder)
        {
            if (folder.Folders.IsEmpty) return Html.empty;

            var title = Html.h1("Sub modules");

            var fs = folder.Folders
                           .Map(f =>
                                    Html.tr(
                                        Html.td("src clearfix",
                                                Html.span("inst-left",
                                                          Html.a(f.Key, $"{f.Key}\\index.html")))))
                           .Join();

            return Html.many(
                title,
                Html.table(Html.tbody(fs)));
        }

        static Html MakeTableOfContents(Project project, Folder folder) =>
            Html.div("table-of-contents",
                Html.p("caption", Html.text("Contents")),         
                Html.ul(
                folder.Files
                          .Map(f => MakeTableOfContentsTopLevel(project, f.Value))
                          .Join()));

        static Html MakeTableOfContentsTopLevel(Project project, File file) =>
            file.Members
                .Map(m => Html.li(Html.a($"#{m.AnchorName}", Html.nowrap(ShortTypeToText(m)))) +
                          MakeTableOfContentsMembers(project, m))
                .Join();

        static Html MakeTableOfContentsMembers(Project project, Member m) =>
            Html.ul(
                m.Members
                 .Map(m => Html.li(Html.a($"#{m.AnchorName}", Html.nowrap(ShortTypeToText(m)))))
                 .Join());
        
        static Html MakeMemberDocuments(Project project, Folder folder, string urlRoot) =>
            folder.Files
                  .Map(f => MakeMemberDocuments(project, f.Value, $"{urlRoot}/{f.Key}"))
                  .Join();

        static Html MakeMemberDocuments(Project project, File file, string urlRoot) =>
            file.Members
                .Map(m => MakeMemberDocument(project, m, urlRoot))
                .Join();

        static Html MakeMemberDocument(Project project, Member m, string urlRoot)
        {
            var member = Html.div2("top",
                                   Html.p("src",
                                          Html.keyword(DeclTypeText(m.Syntax)),
                                          FullTypeToText(m),
                                          Html.a("Source", $"{urlRoot}#L{m.Syntax.GetLocation().GetLineSpan().StartLinePosition.Line}"),
                                          Html.a("selflink", "#", $"#{m.AnchorName}"),
                                          m.ConstraintsToHtml()),
                                   MakeDocumentInToHtml(m),
                                   MakeSubMembers<EnumDeclarationSyntax>(project, m.Members, urlRoot, "Enums"),
                                   MakeSubMembers<DelegateDeclarationSyntax>(project, m.Members, urlRoot, "Delegates"),
                                   MakeSubMembers<EventDeclarationSyntax>(project, m.Members, urlRoot, "Events"),
                                   MakeSubMembers<FieldDeclarationSyntax>(project, m.Members, urlRoot, "Fields"),
                                   MakeSubMembers<IndexerDeclarationSyntax>(project, m.Members, urlRoot, "Indexers"),
                                   MakeSubMembers<PropertyDeclarationSyntax>(project, m.Members, urlRoot, "Properties"),
                                   MakeSubMembers<ConstructorDeclarationSyntax>(project, m.Members, urlRoot, "Constructors"),
                                   MakeSubMembers<MethodDeclarationSyntax>(project, m.Members, urlRoot, "Methods"),
                                   MakeSubMembers<OperatorDeclarationSyntax>(project, m.Members, urlRoot, "Operators"),
                                   MakeSubMembers<InterfaceDeclarationSyntax>(project, m.Members, urlRoot, "Interfaces"),
                                   MakeSubMembers<StructDeclarationSyntax>(project, m.Members, urlRoot, "Structs"),
                                   MakeSubMembers<RecordDeclarationSyntax>(project, m.Members, urlRoot, "Records"),
                                   MakeSubMembers<ClassDeclarationSyntax>(project, m.Members, urlRoot, "Classes"));

            return member;
        }

        static Html MakeExamples(Member m)
        {
            if (!m.Comments.Sections.Exists(s => s.Type == SectionType.Example)) return Html.empty;
            
            var examples = m.Comments
                            .Sections
                            .Filter(s => s.Type == SectionType.Example && s is Tag)
                            .Map(s => (Tag) s)
                            .Map(t => MakeSectionsText(t.Inner)) 
                            .Join();
            
            return Html.div2("methods",
                             Html.p("caption", Html.text("Examples")),
                             examples);            
        }

        static Html MakeParametersAndReturns(Member m)
        {
            if (!m.Comments.Sections.Exists(s => s is Tag &&
                                                 (s.Type == SectionType.TypeParam ||
                                                  s.Type == SectionType.Param ||
                                                  s.Type == SectionType.Returns))) return Html.empty;

            var typeParams = m.Comments
                              .Sections
                              .Filter(s => s.Type == SectionType.TypeParam && s is Tag)
                              .Map(s => (Tag) s)
                              .Filter(t => t.Attrs.Find(a => a.Name == "name").IsSome)
                              .Map(t =>
                                       Html.tr(
                                           Html.td("src clearfix", Html.keyword("type")),
                                           Html.td("src clearfix",
                                                   Html.span("inst-left",
                                                             Html.def(
                                                                 Html.text((string) t.Attrs.Find(a => a.Name == "name").Map(a => a.Value))))),
                                           Html.td("src clearfix",
                                                   Html.span("instruction inst-left", MakeSectionsText(t.Inner)))))
                              .Strict()
                              .Join();

            var argParams = m.Comments
                             .Sections
                             .Filter(s => s.Type == SectionType.Param && s is Tag)
                             .Map(s => (Tag) s)
                             .Filter(t => t.Attrs.Find(a => a.Name == "name").IsSome)
                             .Map(t =>
                                      Html.tr(
                                          Html.td("src clearfix", Html.keyword("param")),
                                          Html.td("src clearfix",
                                                  Html.span("inst-left",
                                                            Html.def(Html.text((string) t.Attrs.Find(a => a.Name == "name").Map(a => a.Value))))),
                                          Html.td("src clearfix",
                                                  Html.span("instruction inst-left", MakeSectionsText(t.Inner)))))
                             .Strict()
                             .Join();

            var returns = m.Comments
                           .Sections
                           .Filter(s => s.Type == SectionType.Returns && s is Tag)
                           .Map(s => (Tag) s)
                           .Map(t =>
                                    Html.tr(
                                        Html.td("src clearfix", Html.keyword("returns")),
                                        Html.td2("src clearfix",
                                                Html.span("instruction inst-left", MakeSectionsText(t.Inner)))))
                           .Strict()
                           .Join();

            return Html.div2("methods",
                             Html.p("caption", Html.text("Parameters")),
                             Html.div2("params-and-returns",
                                       Html.table(
                                           Html.tbody(
                                               typeParams + argParams + returns))));
        }

        static Html MakeSubMembers<MEM>(Project project, Seq<Member> ms, string urlRoot, string memberType) where MEM : MemberDeclarationSyntax
        {
            var submembers = ms.Filter(m => m.Syntax is MEM);
            if (submembers.IsEmpty) return Html.empty;
            return Html.div2("subs methods",
                             Html.p("caption", Html.text(memberType)),
                             submembers.Map(m => MakeMemberDocument(project, m, urlRoot)).Join()); 
        }

        static Html FullTypeToText(Member m) =>
            m.Syntax switch
            {
                ClassDeclarationSyntax c       => MemberName(m) + Generics(m),
                InterfaceDeclarationSyntax c   => MemberName(m) + Generics(m),
                StructDeclarationSyntax c      => MemberName(m) + Generics(m),
                DelegateDeclarationSyntax c    => MemberName(m) + Generics(m),
                EnumDeclarationSyntax c        => MemberName(m),
                RecordDeclarationSyntax c      => MemberName(m) + Generics(m) + Params(c.ParameterList),
                MethodDeclarationSyntax c      => Type(c.ReturnType) + MemberName(m) + Generics(m) + Params(c.ParameterList),
                FieldDeclarationSyntax c       => Type(c.Declaration.Type) + MemberName(m),
                PropertyDeclarationSyntax c    => Type(c.Type) + MemberName(m),
                ConstructorDeclarationSyntax c => MemberName(m) + Params(c.ParameterList),
                OperatorDeclarationSyntax c    => MemberName(m) + Params(c.ParameterList),
                EventDeclarationSyntax c       => Type(c.Type) + MemberName(m),
                IndexerDeclarationSyntax c     => Html.text("[") + Type(c.Type) + Html.text("]"),
                _                              => Html.empty,
            };

        static Html ShortTypeToText(Member m) =>
            m.Syntax switch
            {
                ClassDeclarationSyntax c       => MemberNameText(m) + Generics(m),
                InterfaceDeclarationSyntax c   => MemberNameText(m) + Generics(m),
                StructDeclarationSyntax c      => MemberNameText(m) + Generics(m),
                DelegateDeclarationSyntax c    => MemberNameText(m) + Generics(m),
                EnumDeclarationSyntax c        => MemberNameText(m),
                RecordDeclarationSyntax c      => MemberNameText(m) + Generics(m) + Params(c.ParameterList),
                MethodDeclarationSyntax c      => MemberNameText(m) + Generics(m) + Params(c.ParameterList),
                FieldDeclarationSyntax c       => MemberNameText(m),  // Type(c.Declaration.Type) +
                PropertyDeclarationSyntax c    => MemberNameText(m),  // Type(c.Type) + 
                ConstructorDeclarationSyntax c => MemberNameText(m) + Params(c.ParameterList),
                OperatorDeclarationSyntax c    => MemberNameText(m) + Params(c.ParameterList),
                EventDeclarationSyntax c       => MemberNameText(m),  //Type(c.Type) + 
                IndexerDeclarationSyntax c     => Html.text("this []"),
                _                              => Html.empty,
            };

        static Html MemberName(Member m) =>
            Html.def(m.AnchorName, m.Name);
        
        static Html MemberNameText(Member m) =>
            Html.text(m.Name);
        
        static Html ShortParams(ParameterListSyntax p) =>
            Html.empty;
        
        static Html Type(TypeSyntax t) =>
            new HtmlRaw(
                HttpUtility.HtmlEncode(t)
                           .Replace("&lt;", "<span class='generics'>&lt;</span>")
                           .Replace("&gt;", "<span class='generics'>&gt;</span>")
                           .Replace(", ", "<span class='comma'>, </span>"));
        
        static Html Generics(Member m) =>
            new HtmlRaw(
                HttpUtility.HtmlEncode(m.TypeParamsToText())
                           .Replace("&lt;", "<span class='generics'>&lt;</span>")
                           .Replace("&gt;", "<span class='generics'>&gt;</span>")
                           .Replace(", ", "<span class='comma'>, </span>"));

        static Html Params(ParameterListSyntax? ps) =>
            ps == null
                ? Html.empty
                : new HtmlRaw(HttpUtility.HtmlEncode(ps)
                                         .Replace("&lt;", "<span class='generics'>&lt;</span>")
                                         .Replace("&gt;", "<span class='generics'>&gt;</span>")
                                         .Replace("(", "<span class='parens'>(</span>")
                                         .Replace(")", "<span class='parens'>)</span>")
                                         .Replace(", ", "<span class='comma'>, </span>"));

        static string DeclTypeText(MemberDeclarationSyntax m) =>
            m switch
            {
                null                           => "type",
                ClassDeclarationSyntax c       => "class",
                InterfaceDeclarationSyntax c   => "interface",
                StructDeclarationSyntax c      => "struct",
                DelegateDeclarationSyntax c    => "delegate",
                EnumDeclarationSyntax c        => "enum",
                RecordDeclarationSyntax c      => "record",
                MethodDeclarationSyntax c      => "method",
                FieldDeclarationSyntax c       => "field",
                PropertyDeclarationSyntax c    => "property",
                ConstructorDeclarationSyntax c => "constructor",
                OperatorDeclarationSyntax c    => "operator",
                EventDeclarationSyntax c       => "event",
                IndexerDeclarationSyntax c     => "this",
                _                              => $"[Unknown declaration type: {m.ToString()}]",
            };        

        static Html MakeDocumentInToHtml(Member m)
        {
            var doc = m.Comments;
            
            var summary = MakeSectionsText(
                doc.Sections
                   .Filter(s => s.Type == SectionType.Summary).Map(s => (Tag) s)
                   .Bind(s => s.Inner));

            var remarks = MakeSectionsText(
                doc.Sections
                   .Filter(s => s.Type == SectionType.Remarks).Map(s => (Tag) s)
                   .Bind(s => s.Inner));

            return Html.div2("doc",
                             Html.div(summary),
                             Html.div(remarks),
                             MakeParametersAndReturns(m),
                             MakeExamples(m));
        }

        static Html MakeSectionsText(Seq<Section> sections) =>
            sections.Fold(
                Html.empty,
                (h, s) => s.Type switch
                          {
                              SectionType.Text      => h + MarkdownText(((Text) s).Value),
                              SectionType.Paragraph => h + Html.p(MakeSectionsText(((Tag) s).Inner)),
                              SectionType.Code      => h + Html.code(MakeSectionsText(((Tag) s).Inner)),
                              SectionType.Example   => h + Html.p(MakeSectionsText(((Tag) s).Inner)),
                              _                     => Html.empty
                          });

        static Html MarkdownText(string text)
        {
            text = text.Replace("&lt;", "<");
            text = text.Replace("&gt;", ">");
            text = text.Replace("&nbsp;", " ");

            var html = Markdig.Markdown.ToHtml(text, Pipeline);
            html = html.Replace("<li>", "<li class='markdown-bullet'>");
            
            return new HtmlRaw(html);
        }

        static MarkdownPipeline? pipeline = null;
        static MarkdownPipeline Pipeline =>
            pipeline ??= NewPipeline();

        static MarkdownPipeline NewPipeline() =>
            new MarkdownPipelineBuilder()
               .UsePipeTables()
               .UseAutoLinks()
               .UseTaskLists()
               .Build();
        

        static string GetWebName(Project project, string name) =>
            $"{project.Repo}/{name.Replace('\\', '/')}";

        /// <summary>
        /// Find the parent page
        /// </summary>
        static string GetParent(string name)
        {
            var parts = name.Split("\\");
            if (parts.Length == 1) return parts[0];
            return string.Join("/", Seq(parts).Init);
        }

        /// <summary>
        /// Find the parent page
        /// </summary>
        static string GetRoot(int depth) =>
            $"{string.Concat(Range(0, depth).Map(_ => "../"))}index.html";

        /// <summary>
        /// Walks the files that were found in the first pass and makes them into a Folder tree structure with Files
        /// at the leaves
        /// </summary>
        static Folder MakeFolder(Project project, Folder parent, int depth)
        {
            var folder = project.Data.Files.Fold(parent, (p, f) => MakeNode(project, f.Key, f.Value, depth, p));
            folder = folder with {ReadMe = project.Data.ReadMe.Find(folder.Path).IfNone("")};
            return folder;
        }

        /// <summary>
        /// Recursive walks a path to add it to the Folder tree
        /// </summary>
        static Folder MakeNode(Project project, string path, File file, int depth, Folder folder)
        {
            var parts = path.Split('\\');

            return parts.Length switch
                   {
                       0 => folder,
                       
                       var len when len == depth =>
                           folder with {Files = folder.Files.Add(parts[depth - 1], file), FileCount = folder.FileCount + 1},
                       
                       var len when depth < len =>
                           folder with
                                {
                                    Folders = folder.Folders
                                                    .AddOrUpdate(
                                                         parts[depth - 1],
                                                         Some: exists => MakeNode(project, path, file, depth + 1, exists).UpdateFileCount(),
                                                         None: () => MakeNode(project, path, file, depth + 1, 
                                                                              Folder.Empty with 
                                                                              { 
                                                                                Path = string.Join("\\",parts.Take(depth)),
                                                                                ReadMe = FindReadMe(project, string.Join("\\",parts.Take(depth)))
                                                                              }).UpdateFileCount())
                                },
                       
                       _ => folder
                   };
        }

        static string FindReadMe(Project project, string path) =>
            project.Data
                   .ReadMe
                   .Find(path)
                   .IfNone("");

        static Folder UpdateFileCount(this Folder f) =>
            f with {FileCount = f.FileCount + f.Folders.Map(s => s.FileCount).Sum()};
    }
}