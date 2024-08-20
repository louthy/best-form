using System.Linq;
using System.Web;
using BestForm.Model;
using LanguageExt;
using Markdig;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LanguageExt.Prelude;
using static BestForm.Model.Html;
using File = BestForm.Model.File;

namespace BestForm;

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
        var path = IO.Combine(project.TargetFolder, name);
        IO.CreateDirectory(path);
        foreach (var f in folder.Folders)
        {
            CreateFolders(project, IO.Combine(name, f.Key), f.Value, depth + 1);
        }

        CreateDocPage(project, name, folder, depth);

        return unit;
    }

    static Unit CreateDocPage(Project project, string name, Folder folder, int depth)
    {
        var urlRoot = GetWebName(project, name);
        var path    = IO.Combine(project.TargetFolder, name);
        var head    = Html.head(name);
        var header  = Html.header(project.Name, urlRoot, "../index.html", GetRoot(depth));
        var readMe  = folder.ReadMe;

        var content = div("content",
            MakeModuleHeader(name),
            readMe == "" ? Html.empty : MarkdownText(readMe),
            MakeTableOfContents(project, folder),
            div2("interface",
                MakeModuleLinks(project, folder),
                MakeMemberDocuments(project, folder, urlRoot)));

        var footer1 = script(
            src: "https://cdnjs.cloudflare.com/ajax/libs/prism/1.28.0/components/prism-core.min.js",
            integrity: "sha512-9khQRAUBYEJDCDVP2yw3LRUQvjJ0Pjx0EShmaQjcHa6AXiOv6qHQu9lCAIR8O+/D8FtaCoJ2c0Tf9Xo7hYH01Q==",
            crossorigin: "anonymous"); 

        var footer2 = script(
            src: "https://cdnjs.cloudflare.com/ajax/libs/prism/1.28.0/plugins/autoloader/prism-autoloader.min.js",
            integrity: "sha512-fTl/qcO1VgvKtOMApX2PdZzkziyr2stM65GYPLGuYMnuMm1z2JLJG6XVU7C/mR+E7xBUqCivykuhlzfqxXBXbg==",
            crossorigin: "anonymous"); 
        
        var page = html(head, body(header, content, footer1, footer2));
        var index = IO.Combine(path, "index.html");
        var text  = render(page);
        IO.WriteAllText(index, text);
        IO.WriteAllText(IO.Combine(path, "style.css"), project.CSS);
        IO.WriteAllBytes(IO.Combine(path, "logo.png"), project.Logo);
        return unit;
    }

    static Html MakeModuleHeader(string name)
    {
        var parts = name.Split('\\', '/');

        var hdr = parts.Fold((Seq<Html>(), parts.Length - 1),
                (h, p) =>
                    (h.Item1.Add(a(p, string.Concat(LanguageExt.List.repeat("../", h.Item2)) + "index.html")), h.Item2 - 1))
            .Item1
            .Intersperse(span(text(" ► ")))
            .Join();

        return moduleHeader(hdr);
    }

    static Html MakeModuleLinks(Project _, Folder folder)
    {
        if (folder.Folders.IsEmpty) return Html.empty;

        var title = h1("Sub modules");

        var fs = folder.Folders
            .Map(f => tr(td("src clearfix", span("inst-left", a(f.Key, $"{f.Key}/index.html")))))
            .Join();

        return many(
            title,
            table(tbody(fs)));
    }

    static Html MakeTableOfContents(Project project, Folder folder) =>
        div("table-of-contents",
            p("caption", text("Contents")),         
            ul(
                folder.Files
                    .Map(f => MakeTableOfContentsTopLevel(project, f.Value))
                    .Join()));

    static Html MakeTableOfContentsTopLevel(Project project, File file) =>
        file.Members
            .Map(m => li(a($"#{m.AnchorName}", nowrap(ShortTypeToText(m)))) +
                      MakeTableOfContentsMembers(project, m))
            .Join();

    static Html MakeTableOfContentsMembers(Project _, Member m) =>
        ul(
            m.Members
                .Map(m => li(a($"#{m.AnchorName}", nowrap(ShortTypeToText(m)))))
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
        var member = div2("top",
            p("src", keyword(DeclTypeText(m.Syntax)),
                FullTypeToText(m),
                a("Source", $"{urlRoot}#L{m.Syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1}"),
                a("selflink", "#", $"#{m.AnchorName}"),
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
            
        return div2("methods",
            p("caption", text("Examples")),
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
                tr(
                    td("src clearfix", keyword("type")),
                    td("src clearfix",
                        span("inst-left",
                            def(
                                text((string) t.Attrs.Find(a => a.Name == "name").Map(a => a.Value))))),
                    td("src clearfix",
                        span("instruction inst-left", MakeSectionsText(t.Inner)))))
            .Strict()
            .Join();

        var argParams = m.Comments
            .Sections
            .Filter(s => s.Type == SectionType.Param && s is Tag)
            .Map(s => (Tag) s)
            .Filter(t => t.Attrs.Find(a => a.Name == "name").IsSome)
            .Map(t =>
                tr(
                    td("src clearfix", keyword("param")),
                    td("src clearfix",
                        span("inst-left",
                            def(text((string) t.Attrs.Find(a => a.Name == "name").Map(a => a.Value))))),
                    td("src clearfix", span("instruction inst-left", MakeSectionsText(t.Inner)))))
            .Strict()
            .Join();

        var returns = m.Comments
            .Sections
            .Filter(s => s.Type == SectionType.Returns && s is Tag)
            .Map(s => (Tag) s)
            .Map(t =>
                tr(
                    td("src clearfix", keyword("returns")),
                    td2("src clearfix", span("instruction inst-left", MakeSectionsText(t.Inner)))))
            .Strict()
            .Join();

        return div2("methods",
            p("caption", text("Parameters")),
            div2("params-and-returns", 
                table(
                    tbody(typeParams + argParams + returns))));
    }

    static Html MakeSubMembers<MEM>(Project project, Seq<Member> ms, string urlRoot, string memberType) where MEM : MemberDeclarationSyntax
    {
        var submembers = ms.Filter(m => m.Syntax is MEM);
        if (submembers.IsEmpty) return Html.empty;
        return div2("subs methods",
            p("caption", text(memberType)),
            submembers.Map(m => MakeMemberDocument(project, m, urlRoot)).Join()); 
    }

    static Html FullTypeToText(Member m) =>
        m.Syntax switch
        {
            ClassDeclarationSyntax         => MemberName(m) + Generics(m),
            InterfaceDeclarationSyntax     => MemberName(m) + Generics(m),
            StructDeclarationSyntax        => MemberName(m) + Generics(m),
            DelegateDeclarationSyntax      => MemberName(m) + Generics(m),
            EnumDeclarationSyntax          => MemberName(m),
            RecordDeclarationSyntax c      => MemberName(m) + Generics(m) + Params(c.ParameterList),
            MethodDeclarationSyntax c      => Type(c.ReturnType) + MemberName(m) + Generics(m) + Params(c.ParameterList),
            FieldDeclarationSyntax c       => Type(c.Declaration.Type) + MemberName(m),
            PropertyDeclarationSyntax c    => Type(c.Type) + MemberName(m),
            ConstructorDeclarationSyntax c => MemberName(m) + Params(c.ParameterList),
            OperatorDeclarationSyntax c    => MemberName(m) + Params(c.ParameterList),
            EventDeclarationSyntax c       => Type(c.Type) + MemberName(m),
            IndexerDeclarationSyntax c     => text("[") + Type(c.Type) + text("]"),
            _                              => Html.empty,
        };

    static Html ShortTypeToText(Member m) =>
        m.Syntax switch
        {
            ClassDeclarationSyntax         => MemberNameText(m) + Generics(m),
            InterfaceDeclarationSyntax     => MemberNameText(m) + Generics(m),
            StructDeclarationSyntax        => MemberNameText(m) + Generics(m),
            DelegateDeclarationSyntax      => MemberNameText(m) + Generics(m),
            EnumDeclarationSyntax          => MemberNameText(m),
            RecordDeclarationSyntax c      => MemberNameText(m) + Generics(m) + Params(c.ParameterList),
            MethodDeclarationSyntax c      => MemberNameText(m) + Generics(m) + Params(c.ParameterList),
            FieldDeclarationSyntax         => MemberNameText(m),  // Type(c.Declaration.Type) +
            PropertyDeclarationSyntax      => MemberNameText(m),  // Type(c.Type) + 
            ConstructorDeclarationSyntax c => MemberNameText(m) + Params(c.ParameterList),
            OperatorDeclarationSyntax c    => MemberNameText(m) + Params(c.ParameterList),
            EventDeclarationSyntax         => MemberNameText(m),  //Type(c.Type) + 
            IndexerDeclarationSyntax       => text("this []"),
            _                              => Html.empty,
        };

    static Html MemberName(Member m) =>
        def(m.AnchorName, m.Name);
        
    static Html MemberNameText(Member m) =>
        text(m.Name);
        
    static Html ShortParams(ParameterListSyntax _) =>
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
            ClassDeclarationSyntax         => "class",
            InterfaceDeclarationSyntax     => "interface",
            StructDeclarationSyntax        => "struct",
            DelegateDeclarationSyntax      => "delegate",
            EnumDeclarationSyntax          => "enum",
            RecordDeclarationSyntax        => "record",
            MethodDeclarationSyntax        => "method",
            FieldDeclarationSyntax         => "field",
            PropertyDeclarationSyntax      => "property",
            ConstructorDeclarationSyntax   => "constructor",
            OperatorDeclarationSyntax      => "operator",
            EventDeclarationSyntax         => "event",
            IndexerDeclarationSyntax       => "this",
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

        return div2("doc",
            div(summary),
            div(remarks),
            MakeParametersAndReturns(m),
            MakeExamples(m));
    }

    static Html MakeSectionsText(Seq<Section> sections) =>
        sections.Fold(
            Html.empty,
            (h, s) => s.Type switch
            {
                SectionType.Text      => h + MarkdownText(((Text) s).Value),
                SectionType.Paragraph => h + p(MakeSectionsText(((Tag) s).Inner)),
                SectionType.Code      => h + codeBlock(MakeSectionsText(((Tag) s).Inner)),
                SectionType.Example   => h + p(MakeSectionsText(((Tag) s).Inner)),
                _                     => Html.empty
            });

    static Html MarkdownText(string text)
    {
        text = text.Replace("&lt;", "<");
        text = text.Replace("&gt;", ">");
        text = text.Replace("&nbsp;", " ");

        var html = Markdown.ToHtml(text, Pipeline);
        html = html.Replace("<li>", "<li class='markdown-bullet'>");
        html = html.Replace("<pre>", "<pre class='language-csharp'>");
        html = html.Replace("<code>", "<code class='language-csharp'>");
            
        return new HtmlRaw(html);
    }

    static MarkdownPipeline? pipeline;
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
        var parts = name.Split('\\', '/');
        if (parts.Length == 1) return parts[0];
        return IO.Combine(Seq(parts).Init.ToArray());
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
        var parts = path.Split('\\',  '/');

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
                                    Path = IO.Combine(parts.Take(depth).ToArray()),
                                    ReadMe = FindReadMe(project, IO.Combine(parts.Take(depth).ToArray()))
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