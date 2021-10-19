using System;
using LanguageExt;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace BestForm
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var name = args.Length < 4 ? @"LanguageExt.Core" : args[0];
            var src  = args.Length < 4 ? @"I:\dev\language-ext\LanguageExt.Core" : args[1];
            var dest = args.Length < 4 ? @"I:\dev\louthy.github.io-2\language-ext" : args[2];
            var repo = args.Length < 4 ? "https://github.com/louthy/language-ext/tree/main" : args[3];
            
            var sw = new Stopwatch();
            sw.Start();

            var project = Project.New(name, src, dest, repo, GetCSS(), GetLogo());
            project = await ReadComments.FromProject(project);
            project = DocGen.Run(project);

            sw.Stop();
            Console.WriteLine(TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
        }
        
        static string GetCSS()
        {
            var       assembly     = Assembly.GetExecutingAssembly();
            using var stream       = assembly.GetManifestResourceStream($"BestForm.ocean.css");
            if (stream == null) return "";
            using var reader       = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        
        static byte[] GetLogo()
        {
            var       assembly = Assembly.GetExecutingAssembly();
            using var stream   = assembly.GetManifestResourceStream($"BestForm.logo.png");
            if (stream == null) return new byte[0];
            using var reader = new BinaryReader(stream);
            return reader.ReadBytes((int)stream.Length);
        }
    }
}