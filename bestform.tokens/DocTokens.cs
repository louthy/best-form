using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BestForm.Tokens
{
    public class DocAttr
    {
        public readonly string Name;
        public readonly string Value;

        public DocAttr(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() =>
            $"{Name}='{Value}'";
    }

    public class Tag
    {
        public readonly string Name;
        public readonly Lst<DocAttr> Attrs;
        public readonly string InnerText;

        public Tag(string name, Lst<DocAttr> attrs, string innerText)
        {
            Name = name;
            Attrs = attrs;
            InnerText = HttpUtility.HtmlDecode(innerText);
        }

        string AttrsStr => String.Join(" ", Attrs);

        public override string ToString() =>
            String.IsNullOrEmpty(InnerText)
                ? $"<{Name} {AttrsStr}/>"
                : $"<{Name} {AttrsStr}>{InnerText}</{Name}>";


        public Option<DocAttr> Attr(string name) =>
            Attrs.Filter(x => x.Name == name).HeadOrNone();
    }

    public class DocumentComments
    {
        public readonly Option<Tag> Summary;
        public readonly Option<Tag> Returns;
        public readonly Lst<Tag> Remarks;
        public readonly Lst<Tag> Params;
        public readonly Lst<Tag> TypeParams;
        public readonly Lst<Tag> Exceptions;
        public readonly Lst<Tag> Examples;
        public readonly Lst<Tag> Permissions;
        public readonly Lst<Tag> Includes;
        public readonly Lst<Tag> Completionlists;
        public readonly Lst<Tag> Seealsos;
        public readonly Lst<Tag> Sees;

        public DocumentComments(Lst<Tag> tags)
        {
            Summary = tags.Filter(t => t.Name == "summary").HeadOrNone();
            Returns = tags.Filter(t => t.Name == "returns").HeadOrNone();
            Remarks = tags.Filter(t => t.Name == "remarks");
            TypeParams = tags.Filter(t => t.Name == "typeparam");
            Params = tags.Filter(t => t.Name == "param");
            Examples = tags.Filter(t => t.Name == "example");
            Exceptions = tags.Filter(t => t.Name == "exception");
            Permissions = tags.Filter(t => t.Name == "permission");
            Includes = tags.Filter(t => t.Name == "include");
            Completionlists = tags.Filter(t => t.Name == "completionlist");
            Seealsos = tags.Filter(t => t.Name == "seealso");
            Sees = tags.Filter(t => t.Name == "see");
        }
    }
}
