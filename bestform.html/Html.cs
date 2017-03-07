using BestForm.Tokens;
using LanguageExt;
using static LanguageExt.Prelude;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BestForm
{
    public static class Html
    {
        public static readonly Dom Closed = _ => null;

        public static Dom attr(string value) => (object inp) =>
            List(new DomAttr<object>(inp, _ => value) as DomElement);

        public static Dom attr<T>(Func<T, string> format) => (object inp) =>
            List(new DomAttr<T>(inp, format) as DomElement);

        public static Dom text<T>(Func<T, string> format) => (object inp) =>
            List(new DomTextFmt<T>(inp, format) as DomElement);

        public static Dom text(string text) => _ =>
            List(new DomText(_, text) as DomElement);

        static Dom tag(string tag, object attrs, params Dom[] inner) => inp =>
            List(new DomTag(inp,tag,attrs ?? new { }, combine(inner)) as DomElement);

        public static Dom navLink(Dom name, Dom title) =>
            a(attr("nav-link"), name, title);

        public static Dom number(params Dom[] inner) =>
            span(new { @class = "number" }, inner);

        public static Dom source =>
            div( new { @class = "source-link-cont" }, 
                tag("a", new {
                    @class = "source-link",
                    href = "#",
                    onclick = attr<MethodDef>(m => $"document.getElementById('code-{m.UniqueName}').style.display='';")
                    }, text("SOURCE")));

        public static Dom navPanel(params Dom[] inner) =>
            div(new { @class = "nav-panel" }, inner);

        public static Dom contentPanel(params Dom[] inner) =>
            div(new { @class = "content-panel" }, inner);

        public static Dom left(params Dom[] inner) =>
            div(new { @class = "left" }, inner);

        public static Dom right(params Dom[] inner) =>
            div(new { @class = "right" }, inner);

        public static Dom section(params Dom[] inner) =>
            div(new { @class = "section" }, inner);

        public static Dom sectionTitle(string title) =>
            div(new { @class = "section_title" }, text(title));

        public static Dom code(params Dom[] inner) =>
            div(new { @class = "code" }, inner);

        public static Dom html(params Dom[] inner) =>
            tag("html", new { }, inner);

        public static Dom head(params Dom[] inner) =>
            tag("head", new { }, inner);

        public static Dom a(Dom @class, Dom href, Dom inner) =>
            tag("a", new { @class= @class, href = href }, inner);

        public static Dom anchor(Dom name) =>
            tag("a", new { name = name });

        public static Dom br =>
            tag("br", new { });

        public static Dom p(params Dom[] inner) =>
            tag("p", new { }, inner);

        public static Dom css(Dom href) =>
            tag("link", new { rel="stylesheet", type="text/css", href= href });

        public static Dom h1(Dom inner) =>
            tag("h1", new { }, inner);

        public static Dom h2(Dom inner) =>
            tag("h2", new { }, inner);

        public static Dom h3(Dom inner) =>
            tag("h3", new { }, inner);

        public static Dom h4(Dom inner) =>
            tag("h4", new { }, inner);

        public static Dom body(params Dom[] inner) =>
            tag("body", new { }, inner);

        public static Dom div(object attrs, params Dom[] inner) =>
            tag("div", attrs, inner);

        public static Dom div(Dom inner) =>
            tag("div", new { }, inner);

        public static Dom span(object attrs, params Dom[] inner) =>
            tag("span", attrs, inner);

        public static Dom combine(string sep, params Dom[] inner) => inp =>
            List(new DomGroup(
                inp,
                sep,
                from x in inner
                from y in x(inp)
                select y) as DomElement);

        public static Dom combine(params Dom[] inner) => 
            combine("", inner);

        public static Dom join(string sep, Dom inner) =>
            inp => 
                List(new DomGroup(inp, sep, iter(inner)(inp)) as DomElement);

        public static Dom semi => text(";");

        public static Dom quot(params Dom[] inner) =>
            combine(
                text("\""),
                combine(inner),
                text("\""));

        public static Dom parens(params Dom[] inner) =>
            combine(
                text("("),
                combine(inner),
                text(")"));

        public static Dom brackets(params Dom[] inner) =>
            combine(
                text("["),
                combine(inner),
                text("]"));

        public static Dom angles(params Dom[] inner) =>
            combine(
                text("<"),
                combine(inner),
                text(">"));

        public static Dom braces(params Dom[] inner) =>
            combine(
                text("{"),
                combine(inner),
                text("}"));

        public static Dom iter(Dom inner) => inp =>
            inp is IEnumerable
                ? ((IEnumerable)(inp)).Cast<object>().Map(x => new DomGroup(inp, "", inner(x)) as DomElement).Freeze()
                : inner(inp);

        public static Dom filter<T>(Func<T, bool> f, Dom inner) => inp =>
        {
            if (inp is IEnumerable)
            {
                var elements = new List<DomElement>();
                foreach (object item in ((IEnumerable)(inp)))
                {
                    if (f((T)item))
                    {
                        elements.Add(new DomGroup(item, "", inner(item)));
                    }
                }
                return elements.Freeze();
            }
            else
            {
                if (f((T)inp))
                {
                    return List(new DomGroup(inp, "", inner((T)inp)) as DomElement);
                }
                else
                {
                    return List<DomElement>();
                }
            }
        };

        public static Dom filter(Func<dynamic, bool> f, Dom inner) => inp =>
        {
            if (inp is IEnumerable)
            {
                var elements = new List<DomElement>();
                foreach (dynamic item in ((IEnumerable)(inp)))
                {
                    if (f(item))
                    {
                        elements.Add(new DomGroup(item, "", inner(item)));
                    }
                }
                return elements.Freeze();
            }
            else
            {
                if (f((dynamic)inp))
                {
                    return List(new DomGroup(inp, "", inner((dynamic)inp)) as DomElement);
                }
                else
                {
                    return List<DomElement>();
                }
            }
        };

        public static Dom zeroOrMany(Dom Zero, Dom Many) => inp =>
            inp is IEnumerable
                ? (((IEnumerable)(inp)).Cast<object>().Any())
                    ? Many(inp)
                    : Zero(inp)
                : List<DomElement>();

        public static Dom map<T, R>(Func<T, R> f, Dom inner) => inp =>
        {
            if (inp is IEnumerable)
            {
                var elements = new List<DomElement>();
                foreach (T item in ((IEnumerable)(inp)).Cast<T>())
                {
                    elements.Add(new DomGroup(item, "", inner(f(item))));
                }
                return elements.Freeze();
            }
            else
            {
                return inner(f((T)inp));
            }
        };

        public static Dom map(Func<dynamic, dynamic> f, Dom inner) => inp =>
        {
            if (inp is IEnumerable)
            {
                var elements = new List<DomElement>();
                foreach (dynamic item in ((IEnumerable)(inp)).Cast<object>())
                {
                    elements.Add(new DomGroup(item, "", inner(f(item))));
                }
                return elements.Freeze();
            }
            else
            {
                return inner(f(inp));
            }
        };

        public static Dom any(Dom inner) =>
            inp =>
                (inp is IEnumerable)
                    ? (((IEnumerable)(inp)).Cast<object>().Any())
                        ? inner(inp)
                        : List<DomElement>()
                    : List<DomElement>();

        public static Dom any<T, R>(Func<T, IEnumerable<R>> f, params Dom[] inner) =>
            inp =>
            {
                var res = f((T)inp).Freeze();
                return res.Any()
                    ? combine(inner)(res)
                    : text("")(res);
            };

        public static Dom option<T>(Func<T, Dom> Some, Func<Dom> None) => inp =>
            ((Option<T>)inp).Match(Some, None)(inp);


        public static Dom option<T>(Dom Some, Dom None) => inp =>
            ((Option<T>)inp).Match(
                 x => Some(x),
                () => None(null));
    }
}
