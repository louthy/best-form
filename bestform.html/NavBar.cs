using System;
using System.Linq;
using LanguageExt;
using static LanguageExt.List;
using static LanguageExt.Prelude;
using static BestForm.Html;
using static BestForm.TokenHtml;
using BestForm.Tokens;

namespace BestForm
{
    public static class NavBar
    {
        public static Dom dom(Lst<Tuple<string, string>> trail, Dom rest) =>
            combine(
                div(new { @class = "nav-bar" },
                    combine(
                        " \\ ",
                        trail.Take(trail.Count-1).Map( tup =>
                            tup.Map((title, url) => 
                                a(@class: attr("nav-bar-link"), 
                                    href: attr(url), 
                                    inner: text(title))))
                            .ToArray()
                            .Freeze()
                            .Add(trail.Last().Map(
                                (title, url) => 
                                    span( new { @class = attr("nav-bar-link"), href = attr(url) }, text(title)))).ToArray())),
                rest);
    }
}
