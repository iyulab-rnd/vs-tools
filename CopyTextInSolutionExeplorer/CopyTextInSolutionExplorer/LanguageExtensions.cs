using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CopyTextInSolutionExeplorer
{
    public static class LanguageExtensions
    {
        private static Dictionary<string, string[]> GetMapDictionary()
        {
            return new Dictionary<string, string[]>()
        {
            { "cucumber", new string[] { ".feature" } },
            { "abap", new string[] { ".abap" } },
            { "ada", new string[] { ".adb", ".ads", ".ada" } },
            { "ahk", new string[] { ".ahk", ".ahkl" } },
            { "apacheconf", new string[] { ".htaccess", "apache.conf", "apache2.conf" } },
            { "applescript", new string[] { ".applescript" } },
            { "as", new string[] { ".as" } },
            { "as3", new string[] { ".as" } },
            { "asy", new string[] { ".asy" } },
            { "bash", new string[] { ".sh", ".ksh", ".bash", ".ebuild", ".eclass" } },
            { "bat", new string[] { ".bat", ".cmd" } },
            { "befunge", new string[] { ".befunge" } },
            { "blitzmax", new string[] { ".bmx" } },
            { "boo", new string[] { ".boo" } },
            { "brainfuck", new string[] { ".bf", ".b" } },
            { "c", new string[] { ".c", ".h" } },
            { "cfm", new string[] { ".cfm", ".cfml", ".cfc" } },
            { "cheetah", new string[] { ".tmpl", ".spt" } },
            { "cl", new string[] { ".cl", ".lisp", ".el" } },
            { "clojure", new string[] { ".clj", ".cljs" } },
            { "cmake", new string[] { ".cmake", "CMakeLists.txt" } },
            { "coffeescript", new string[] { ".coffee" } },
            { "console", new string[] { ".sh-session" } },
            { "control", new string[] { "control" } },
            { "cpp", new string[] { ".cpp", ".hpp", ".c++", ".h++", ".cc", ".hh", ".cxx", ".hxx", ".pde" } },
            { "csharp", new string[] { ".cs" } },
            { "css", new string[] { ".css" } },
            { "cython", new string[] { ".pyx", ".pxd", ".pxi" } },
            { "d", new string[] { ".d", ".di" } },
            { "delphi", new string[] { ".pas" } },
            { "diff", new string[] { ".diff", ".patch" } },
            { "dpatch", new string[] { ".dpatch", ".darcspatch" } },
            { "duel", new string[] { ".duel", ".jbst" } },
            { "dylan", new string[] { ".dylan", ".dyl" } },
            { "erb", new string[] { ".erb" } },
            { "erl", new string[] { ".erl-sh" } },
            { "erlang", new string[] { ".erl", ".hrl" } },
            { "evoque", new string[] { ".evoque" } },
            { "factor", new string[] { ".factor" } },
            { "felix", new string[] { ".flx", ".flxh" } },
            { "fortran", new string[] { ".f", ".f90" } },
            { "gas", new string[] { ".s", ".S" } },
            { "genshi", new string[] { ".kid" } },
            { "glsl", new string[] { ".vert", ".frag", ".geo" } },
            { "gnuplot", new string[] { ".plot", ".plt" } },
            { "go", new string[] { ".go" } },
            { "groff", new string[] { ".(1234567)", ".man" } },
            { "haml", new string[] { ".haml" } },
            { "haskell", new string[] { ".hs" } },
            { "html", new string[] { ".html", ".htm", ".xhtml", ".xslt" } },
            { "hx", new string[] { ".hx" } },
            { "hybris", new string[] { ".hy", ".hyb" } },
            { "ini", new string[] { ".ini", ".cfg" } },
            { "io", new string[] { ".io" } },
            { "ioke", new string[] { ".ik" } },
            { "irc", new string[] { ".weechatlog" } },
            { "jade", new string[] { ".jade" } },
            { "java", new string[] { ".java" } },
            { "js", new string[] { ".js" } },
            { "jsp", new string[] { ".jsp" } },
            { "lhs", new string[] { ".lhs" } },
            { "llvm", new string[] { ".ll" } },
            { "logtalk", new string[] { ".lgt" } },
            { "lua", new string[] { ".lua", ".wlua" } },
            { "make", new string[] { ".mak", "Makefile", "makefile", "Makefile.", "GNUmakefile" } },
            { "mako", new string[] { ".mao" } },
            { "maql", new string[] { ".maql" } },
            { "mason", new string[] { ".mhtml", ".mc", ".mi", "autohandler", "dhandler" } },
            { "markdown", new string[] { ".md" } },
            { "modelica", new string[] { ".mo" } },
            { "modula2", new string[] { ".def", ".mod" } },
            { "moocode", new string[] { ".moo" } },
            { "mupad", new string[] { ".mu" } },
            { "mxml", new string[] { ".mxml" } },
            { "myghty", new string[] { ".myt", "autodelegate" } },
            { "nasm", new string[] { ".asm", ".ASM" } },
            { "newspeak", new string[] { ".ns2" } },
            { "objdump", new string[] { ".objdump" } },
            { "objectivec", new string[] { ".m" } },
            { "objectivej", new string[] { ".j" } },
            { "ocaml", new string[] { ".ml", ".mli", ".mll", ".mly" } },
            { "ooc", new string[] { ".ooc" } },
            { "perl", new string[] { ".pl", ".pm" } },
            { "php", new string[] { ".php", ".php(345)" } },
            { "postscript", new string[] { ".ps", ".eps" } },
            { "pot", new string[] { ".pot", ".po" } },
            { "pov", new string[] { ".pov", ".inc" } },
            { "prolog", new string[] { ".prolog", ".pro", ".pl" } },
            { "properties", new string[] { ".properties" } },
            { "protobuf", new string[] { ".proto" } },
            { "py3tb", new string[] { ".py3tb" } },
            { "pytb", new string[] { ".pytb" } },
            { "python", new string[] { ".py", ".pyw", ".sc", "SConstruct", "SConscript", ".tac" } },
            { "r", new string[] { ".R" } },
            { "rb", new string[] { ".rb", ".rbw", "Rakefile", ".rake", ".gemspec", ".rbx", ".duby" } },
            { "rconsole", new string[] { ".Rout" } },
            { "rebol", new string[] { ".r", ".r3" } },
            { "redcode", new string[] { ".cw" } },
            { "rhtml", new string[] { ".rhtml" } },
            { "rst", new string[] { ".rst", ".rest" } },
            { "sass", new string[] { ".sass" } },
            { "scala", new string[] { ".scala" } },
            { "scaml", new string[] { ".scaml" } },
            { "scheme", new string[] { ".scm" } },
            { "scss", new string[] { ".scss" } },
            { "smalltalk", new string[] { ".st" } },
            { "smarty", new string[] { ".tpl" } },
            { "sourceslist", new string[] { "sources.list" } },
            { "splus", new string[] { ".S", ".R" } },
            { "sql", new string[] { ".sql" } },
            { "sqlite3", new string[] { ".sqlite3-console" } },
            { "squidconf", new string[] { "squid.conf" } },
            { "ssp", new string[] { ".ssp" } },
            { "tcl", new string[] { ".tcl" } },
            { "tcsh", new string[] { ".tcsh", ".csh" } },
            { "tex", new string[] { ".tex", ".aux", ".toc" } },
            { "text", new string[] { ".txt" } },
            { "v", new string[] { ".v", ".sv" } },
            { "vala", new string[] { ".vala", ".vapi" } },
            { "vbnet", new string[] { ".vb", ".bas" } },
            { "velocity", new string[] { ".vm", ".fhtml" } },
            { "vim", new string[] { ".vim", ".vimrc" } },
            { "xml", new string[] { ".xml", ".xsl", ".rss", ".xslt", ".xsd", ".wsdl", ".xaml" } },
            { "xquery", new string[] { ".xqy", ".xquery" } },
            { "xslt", new string[] { ".xsl", ".xslt" } },
            { "yaml", new string[] { ".yaml", ".yml" } },
            { "zpt", new string[] { ".zpt" } },
            { "zsh", new string[] { ".zsh", ".bash" } },
            { "json", new string[] { ".json" } },
            { "jsx", new string[] { ".jsx" } },
            { "tsx", new string[] { ".tsx" } },
            { "graphql", new string[] { ".graphql" } },
            { "vue", new string[] { ".vue" } },
            { "svelte", new string[] { ".svelte" } },
            { "solidity", new string[] { ".sol" } },
            { "reason", new string[] { ".re" } },
            { "perl6", new string[] { ".p6", ".pl6" }}
        };
        }

        private static Dictionary<string, string> InvertDictionary(Dictionary<string, string[]> original)
        {
            var inverted = new Dictionary<string, string>();

            foreach (var kvp in original)
            {
                foreach (var value in kvp.Value)
                {
                    // 같은 value에 대한 key가 이미 존재할 경우, 최신 key로 업데이트합니다.
                    inverted[value] = kvp.Key;
                }
            }

            return inverted;
        }

        private static Dictionary<string, string> map = null;
        internal static Dictionary<string, string> GetMap()
        {
            if (map != null) return map;
            map = InvertDictionary(GetMapDictionary());
            return map;
        }
    }
}