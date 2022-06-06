# best-form

_The best form of documentation is the code itself_

This is a C# doc-gen tool.  It was primarily built to support my [Language-Ext](https://github.com/louthy/language-ext) project.  Which is non-idiomatic in its approach.  I couldn't find documentation generators that did it justice.  I also really liked the Hackage documentation style from Haskell, so have taken a styling approach from there (even if it looks a little dated now, it was always the documentation I felt most comfortable reading).

* Parses C# source code to extract the XML documentation of source files
  * XML comments can contain markdown for elegant formatting of documentation blocks
* Groups all files within a folder together into a single documentation page
  * Each folder can have a README.md which will be placed at the top of the documentation
* Creates a browsable hierarchy that reflects the folder names
  * Using more interesting folder names gives good titles



See the [language-ext API reference for a demo of this tool](https://louthy.github.io/language-ext/LanguageExt.Core/)
