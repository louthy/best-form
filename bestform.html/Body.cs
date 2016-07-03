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
        public static Dom Build(Dom content) =>
            html(
                head(css(href: attr("doc.css"))),
                body(content));
    }
}
