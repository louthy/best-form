using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.Parsec;
using LanguageExt;
using static LanguageExt.Prelude;
using BestForm.Tokens;

namespace BestForm.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if( args.Length != 2)
            {
                Console.WriteLine("bestform.cli <source-code-path> <output-path>");
                return;
            }

            var lang = new CS.Lang();

            var results = (from file in Files(args[0])//.Where(x => x.EndsWith("\\Either.cs"))
                           let src = ReadFile(file)
                           select lang.Parser.Run(src).MapLeft(err =>Tuple(err, Path.GetFileName(file)))).AsParallel().Freeze();

            Console.ForegroundColor = ConsoleColor.Red;
            results.Lefts().Iter(tup => Console.WriteLine($"{tup.Item2}: {tup.Item1}"));
            Console.ForegroundColor = ConsoleColor.White;

            HtmlDocBuilder.Run(args[1],results.Rights().Freeze());
        }

        static string ReadFile(string path) =>
            String.Join("\n", File.ReadAllLines(path).Map(l => l.Trim().StartsWith("#") ? "" : l));

        static string PrintResult(string path, Either<ParserError, SourceFile> result) =>
            result.Match(
                Right: res =>
                {
                    var msg = $"{Path.GetFileName(path)}: success";
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.White;
                    return msg;
                },
                Left: err =>
                {
                    var msg = $"{Path.GetFileName(path)}: {err}";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.White;
                    return msg;
                });

        static IEnumerable<string> Files(string path) =>
            Directory.GetFiles(path, "*.cs").Concat(
            (from files in Directory.GetDirectories(path)
                                    .Filter(x => !x.EndsWith("\\bin") && !x.EndsWith("\\obj"))
                                    .Map(Files)
             from file in files
             select file));
    }

    public static class ParserExt
    {
        public static Either<ParserError, SourceFile> Run(this Parser<SourceFile> p, string source)
        {
            var res = p(source.ToPString());
            return res.IsFaulted || res.Reply.State.ToString().Length > 0
                ? Left<ParserError, SourceFile>(res.Reply.Error)
                : Right<ParserError, SourceFile>(res.Reply.Result);
        }
    }
}
