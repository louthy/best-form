using BestForm.Tokens;
using LanguageExt;
using static LanguageExt.Prelude;
using static LanguageExt.List;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using BestForm.CS;

namespace BestForm.CLI
{
    public class HtmlDocBuilder
    {
        static Doc docp = new Doc();

        public static void Run(string output, Lst<SourceFile> sources)
        {
            Directory.CreateDirectory(output);
            Copy("doc.css", Path.Combine(output, "doc.css"));

            var allns = YieldNamespaces("", sources.Map(x => x as Namespace).Freeze());

            var namespaces = fold(allns, Map<string, Namespace>(), (state, ns) =>
                 state.ContainsKey(ns.Item1)
                     ? state.AddOrUpdate(ns.Item1, Merge(state[ns.Item1], ns.Item2))
                     : state.Add(ns.Item1, ns.Item2));

            namespaces.AsEnumerable().Select(ns =>
            {
                var trail = List(Tuple("API Reference", "../index.htm"), Tuple(ns.Key, $"../{ns.Key}/index.htm"));

                var nspath = Path.Combine(output, ns.Key);
                Directory.CreateDirectory(nspath);
                Copy("doc.css", Path.Combine(nspath, "doc.css"));

                File.WriteAllText(
                    Path.Combine(nspath, $"index.htm"),
                    Body.Build(trail, NamespacePage.page)(ns.Value).AsString()
                    );

                WriteDelegates(ns, nspath, trail);
                WriteTypes(ns, nspath, trail);
                WriteEnums(ns, nspath, trail);

                return ns;
            }).AsParallel().ToList();
        }

        private static void WriteTypes(IMapItem<string, Namespace> ns, string nspath, Lst<Tuple<string, string>> trail)
        {
            ns.Value.Types.Filter(t => t.Visibility == Vis.Public || t.Visibility == Vis.Protected).Select(type =>
            {
                var tpath = Path.Combine(nspath, $"{type.UniqueName}.htm");

                File.WriteAllText(
                    tpath,
                    Body.Build(
                        trail.Add(Tuple(type.Name.ToString(), $"{type.UniqueName}.htm")),
                        TypePage.page
                        )(Tuple(ns.Key, type)).AsString()
                    );

                return type;
            }).AsParallel().ToList();
        }

        private static void WriteEnums(IMapItem<string, Namespace> ns, string nspath, Lst<Tuple<string, string>> trail)
        {
            ns.Value.Enums.Filter(t => t.Visibility == Vis.Public).Select(type =>
            {
                var tpath = Path.Combine(nspath, $"{type.Name}.htm");

                File.WriteAllText(
                    tpath,
                    Body.Build(
                        trail.Add(Tuple(type.Name.ToString(), $"{type.Name}.htm")),
                        EnumPage.page
                        )(Tuple(ns.Key, type)).AsString()
                    );

                return type;
            }).AsParallel().ToList();
        }

        private static void WriteDelegates(IMapItem<string, Namespace> ns, string nspath, Lst<Tuple<string,string>> trail)
        {
            ns.Value.Delegates.Filter(t => t.Visibility == Vis.Public).Select(type =>
            {

                var tpath = Path.Combine(nspath, $"{type.UniqueName}.htm");

                File.WriteAllText(
                    tpath,
                    Body.Build(
                        trail.Add(Tuple(type.Name.ToString(), $"{type.UniqueName}.htm")), 
                        DelegatePage.page
                        )(Tuple(ns.Key, type)).AsString()
                    );

                return type;
            }).AsParallel().ToList();
        }

        private static void Copy(string from, string to)
        {
            if (File.Exists(to))
            {
                File.Delete(to);
            }
            File.Copy(from, to);
        }

        static IEnumerable<Tuple<string,Namespace>> YieldNamespaces(string current, Lst<Namespace> list)
        {
            foreach(var ns in list)
            {
                var fqn = current == "" ? ns.Name.ToString() : $"{current}.{ns.Name}";
                yield return Tuple(fqn, ns);
                foreach (var cns in YieldNamespaces(fqn, ns.Namespaces))
                {
                    yield return cns;
                }
            }
        }

        static Namespace Merge(Namespace lhs, Namespace rhs) =>
            new Namespace(
                lhs.Name,
                lhs.Usings + rhs.Usings,
                lhs.UsingAliases + rhs.UsingAliases,
                lhs.Namespaces + rhs.Namespaces,
                lhs.Types + rhs.Types,
                lhs.Enums + rhs.Enums,
                lhs.Delegates + rhs.Delegates).MergeTypes();
    }
}
