using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using BestForm;
using BestForm.Model;

var name = args.Length < 4 ? @"LanguageExt.Core" : args[0];
var src  = args.Length < 4 ? @"/home/paul/Documents/dev/language-ext/LanguageExt.Core" : args[1];
var dest = args.Length < 4 ? @"/home/paul/Documents/dev/louthy.github.io/language-ext" : args[2];
var repo = args.Length < 4 ? "https://github.com/louthy/language-ext/tree/main" : args[3];

var sw = new Stopwatch();
sw.Start();

var project = Project.New(name, IO.SafePath(src), IO.SafePath(dest), repo, GetCSS(), GetLogo());
project = await ReadComments.FromProject(project);
DocGen.Run(project);

sw.Stop();
Console.WriteLine(TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));

static string GetCSS()
{
    var       assembly = Assembly.GetExecutingAssembly();
    using var stream   = assembly.GetManifestResourceStream("BestForm.ocean.css");
    if (stream == null) return GetBackupCSS();
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

// Linux doesn't seem to like .NET Resources, so go grab the source
static string GetBackupCSS()
{
    using var client = new HttpClient();
    var bytes = client.GetByteArrayAsync("https://raw.githubusercontent.com/louthy/best-form/main/bestform/ocean.css").Result;
    return Encoding.UTF8.GetString(bytes);
}

static byte[] GetLogo()
{
    var       assembly = Assembly.GetExecutingAssembly();
    using var stream   = assembly.GetManifestResourceStream("BestForm.logo.png");
    if (stream == null) return GetBackupLogo();
    using var reader = new BinaryReader(stream);
    return reader.ReadBytes((int)stream.Length);
}

// Linux doesn't seem to like .NET Resources, so go grab the source
static byte[] GetBackupLogo()
{
    using var client = new HttpClient();
    return client.GetByteArrayAsync("https://raw.githubusercontent.com/louthy/best-form/main/bestform/logo.png").Result;
}
