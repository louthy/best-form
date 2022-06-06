# best-form

_The best form of documentation is the code itself_

This is a C# doc-gen tool.  It was primarily built to support my [Language-Ext](https://github.com/louthy/language-ext) project.  Which is non-idiomatic in its approach.  I couldn't find documentation generators that did it justice.  I also really liked the Hackage documentation style from Haskell, so have taken a styling approach from there (even if it looks a little dated now, it was always the documentation I felt most comfortable reading).

* Parses C# source code to extract the XML documentation of source files
  * Comments can contain markdown for elegant formatting of documentation blocks
  * Handles `<` and `>` within tags blocks correctly
* Groups all files within a folder together into a single documentation page
  * Produces a quick links index for each page, so it's easy to drop down to specific entity documentation
  * Each folder can have a __`README.md`__ which will be placed at the top of the page to be used as contextual/introductory information for the reference documentation below it.  This can be very good to provide a little more flavour to the raw source-code documentation.
* Creates a browsable hierarchy that reflects the folder names
  * Using more interesting folder names gives good titles



See the [language-ext API reference for a demo of this tool](https://louthy.github.io/language-ext/LanguageExt.Core/)
