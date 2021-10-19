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

            CreateFolders(project, project.Name, project.Folder);
            
            return project;
        }

        /// <summary>
        /// Walk the folder data structure and create the real folders
        /// </summary>
        static Unit CreateFolders(Project project, string name, Folder folder)
        {
            if (folder.FileCount == 0) return unit;
            var path = Path.Combine(project.TargetFolder, name);
            Directory.CreateDirectory(path);
            foreach (var f in folder.Folders)
            {
                CreateFolders(project, Path.Combine(name, f.Key), f.Value);
            }

            CreateDocPage(project, name, folder);

            return unit;
        }

        static Unit CreateDocPage(Project project, string name, Folder folder)
        {
            var urlRoot = GetWebName(project, name);
            var path    = Path.Combine(project.TargetFolder, name);
            var head    = Html.head(name);
            var header  = Html.header(project.Name, urlRoot, "../index.html", GetRoot(name));

            var content = Html.div("content",
                                   MakeModuleHeader(name),
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
                                          ConstraintsToText(m)),
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
                                           Html.td("src clearfix", Html.keyword("type param")),
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
                ClassDeclarationSyntax c       => Html.span(Html.def($"{m.Name}") + Generics(m)),
                InterfaceDeclarationSyntax c   => Html.span(Html.def($"{m.Name}") + Generics(m)),
                StructDeclarationSyntax c      => Html.span(Html.def($"{m.Name}") + Generics(m)),
                DelegateDeclarationSyntax c    => Html.span(Html.def($"{m.Name}") + Generics(m)),
                EnumDeclarationSyntax c        => Html.span(Html.def($"{m.Name}")),
                RecordDeclarationSyntax c      => Html.span(Html.def($"{m.Name}") + Generics(m) + Params(c.ParameterList)),
                MethodDeclarationSyntax c      => Type(c.ReturnType) + Html.def($"{m.Name}") + Generics(m) + Params(c.ParameterList),
                FieldDeclarationSyntax c       => Type(c.Declaration.Type) + Html.def($"{m.Name}"),
                PropertyDeclarationSyntax c    => Type(c.Type) + Html.def($"{m.Name}"),
                ConstructorDeclarationSyntax c => Html.def($"{m.Name}") + Params(c.ParameterList),
                OperatorDeclarationSyntax c    => Html.def($"{m.Name}") + Params(c.ParameterList),
                EventDeclarationSyntax c       => Type(c.Type) + Html.def($"{m.Name}"),
                IndexerDeclarationSyntax c     => Html.text("[") + Type(c.Type) + Html.text("]"),
                _                              => Html.empty,
            };
        
        static Html Type(TypeSyntax t) =>
            new HtmlRaw(
                HttpUtility.HtmlEncode(t)
                           .Replace("&lt;", "<span class='generics'>&lt;</span>")
                           .Replace("&gt;", "<span class='generics'>&gt;</span>")
                           .Replace(", ", "<span class='comma'>, </span>"));
        
        static Html Generics(Member m) =>
            new HtmlRaw(
                HttpUtility.HtmlEncode(TypeParamsToText(m.Syntax))
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

        static Html ConstraintsToText(Member m) =>
            Html.div2("constraints",
                      ConstraintsToText(m.Syntax) switch
                      {
                          "" => Html.empty,

                          var cs => cs.Split("where")
                                      .Filter(x => !string.IsNullOrWhiteSpace(x))
                                      .Map(c => Html.div2("constraint",
                                                          Html.keyword("where"),
                                                          Html.span(Html.text(c))))
                                      .Join()
                      });
        
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
        
        static string TypeParamsToText(MemberDeclarationSyntax m) =>
            m switch
            {
                ClassDeclarationSyntax c     => c.TypeParameterList?.ToString() ?? "",
                InterfaceDeclarationSyntax c => c.TypeParameterList?.ToString() ?? "",
                StructDeclarationSyntax c    => c.TypeParameterList?.ToString() ?? "",
                DelegateDeclarationSyntax c  => c.TypeParameterList?.ToString() ?? "",
                RecordDeclarationSyntax c    => c.TypeParameterList?.ToString() ?? "",
                MethodDeclarationSyntax c    => c.TypeParameterList?.ToString() ?? "",
                _                            => "",
            };        

        static string ConstraintsToText(MemberDeclarationSyntax m) =>
            m switch
            {
                ClassDeclarationSyntax c     => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
                InterfaceDeclarationSyntax c => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
                StructDeclarationSyntax c    => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
                DelegateDeclarationSyntax c  => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
                RecordDeclarationSyntax c    => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
                MethodDeclarationSyntax c    => c.ConstraintClauses.Count == 0 ? "" : c.ConstraintClauses.ToString(),
                _                            => "",
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
        static string GetRoot(string name) =>
            $"/{name.Split("\\").Head()}/index.html";

        /// <summary>
        /// Walks the files that were found in the first pass and makes them into a Folder tree structure with Files
        /// at the leaves
        /// </summary>
        static Folder MakeFolder(Project project, Folder parent, int depth) =>
            project.Data.Files.Fold(parent, (p, f) => MakeNode(project, f.Key, f.Value, depth, p));

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
                                                         None: () => MakeNode(project, path, file, depth + 1, Folder.Empty).UpdateFileCount())
                                },
                       
                       _ => folder
                   };
        }

        static Folder UpdateFileCount(this Folder f) =>
            f with {FileCount = f.FileCount + f.Folders.Map(s => s.FileCount).Sum()};
    }
}