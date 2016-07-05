# best-form
C# doc-gen

* Parses C# source code to extract the XML documentation as well as a fully tokenised structure of the code itself
  * Uses [the LanguageExt.Parsec library](https://louthy.github.io/LanguageExt.Parsec/LanguageExt.Parsec/index.htm) to parse the raw C# source
* Uses a virtual DOM that is actually a reader monad to project the tokenised structure into HTML markup
* Generates doc-pages that look very much like the .NET Core docs

See the [language-ext API reference for a demo of this tool](https://louthy.github.io/)
