using System;
using LanguageExt;
using static LanguageExt.List;
using static LanguageExt.Prelude;
using static BestForm.Html;
using static BestForm.TokenHtml;
using BestForm.Tokens;

namespace BestForm
{
    public static class Body
    {
        public static Dom Build(Lst<Tuple<string, string>> trail, Dom content) =>
            html(
                head(css(href: attr("doc.css"))),
                body(NavBar.dom(trail, content)));
    }
}
