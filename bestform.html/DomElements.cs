using LanguageExt;
using static LanguageExt.Prelude;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BestForm
{
    public delegate Lst<DomElement> Dom(object env);

    public enum DomElementType
    {
        Tag,
        Attr,
        Text,
        Group
    }

    public abstract class DomElement
    {
        public readonly object Env;

        public abstract DomElementType ElementType
        {
            get;
        }

        public DomElement(object env)
        {
            Env = env;
        }
    }

    public class DomGroup : DomElement
    {
        public readonly Lst<DomElement> Group;
        public readonly string Sep;

        public DomGroup(object env, string sep, Lst<DomElement> group) : base(env)
        {
            Group = group;
            Sep = sep;
        }

        public override DomElementType ElementType => DomElementType.Group;
        public override string ToString() => String.Join(Sep, Group.Map(x => x.ToString()));
    }

    public class DomText : DomElement
    {
        public readonly string Text;

        public DomText(object env, string text) : base(env)
        {
            Text = HttpUtility.HtmlEncode(text);
        }

        public override DomElementType ElementType => DomElementType.Text;
        public override string ToString() => Text;
    }

    public class DomAttr<T> : DomElement
    {
        public readonly string Text;

        public DomAttr(object env, Func<T, string> fmt) : base(env)
        {
            Text = fmt((T)env);
        }

        public override DomElementType ElementType => DomElementType.Attr;
        public override string ToString() => Text;
    }

    public class DomTextFmt<T> : DomText
    {
        public DomTextFmt(object env, Func<T, string> fmt)  
            : base(env, fmt((T)env))
        {
        }
    }

    public class DomTag : DomElement
    {
        public readonly string Tag;
        public readonly object Attrs;
        public readonly Dom Inner;

        public DomTag(object env, string tag, object attrs, Dom inner) : base(env)
        {
            Tag = tag;
            Attrs = attrs;
            Inner = inner;
        }

        public override DomElementType ElementType => DomElementType.Tag;

        public override string ToString()
        {
            var res = Inner(Env);
            if (res.Count == 0) // TODO, some zero item tags should still be open tags
            {
                return $"<{Tag} {Attributes(Env, Attrs)} />";
            }
            else 
            {
                return $"<{Tag} {Attributes(Env, Attrs)}>{String.Join("", Inner(Env))}</{Tag}>";
            }
        }

        static string Attributes(object inp, object attrs)
        {
            var domElType = typeof(Dom);
            var calculatedFields = attrs.GetType()
                                        .GetFields(BindingFlags.Public | BindingFlags.Instance)
                                        .Filter(f => f.FieldType == domElType)
                                        .Map(f => Tuple(f.Name.Replace("@", ""), ((Dom)f.GetValue(attrs))(inp).AsString()));

            var calculateProps = attrs.GetType()
                                      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Filter(f => f.PropertyType == domElType)
                                      .Map(f => Tuple(f.Name.Replace("@", ""), ((Dom)f.GetValue(attrs))(inp).AsString()));

            var fields = attrs.GetType()
                              .GetFields(BindingFlags.Public | BindingFlags.Instance)
                              .Filter(f => f.FieldType != domElType)
                              .Map(f => Tuple(f.Name.Replace("@", ""), f.GetValue(attrs)?.ToString()));

            var props = attrs.GetType()
                             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Filter(f => f.PropertyType != domElType)
                             .Map(f => Tuple(f.Name.Replace("@", ""), f.GetValue(attrs)?.ToString()));

            var all = fields.Concat(props).Concat(calculatedFields).Concat(calculateProps);

            return String.Join(" ", all.Map(attr => $"{attr.Item1}=\"{HttpUtility.HtmlAttributeEncode(attr.Item2)}\""));
        }

    }

    public static class DomExt
    {
        public static string AsString(this Lst<DomElement> self)
        {
            return String.Join("", self);
        }

        public static Lst<DomElement> Do(this Either<string, Dom> self, object env)
        {
            return self.Match(r => r(env), l => Html.text(l)(env));
        }
    }
}
