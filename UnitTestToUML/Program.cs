// 
//  Program.cs
// 
//  Author:
//   Jim Borden  <jim.borden@couchbase.com>
// 
//  Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using McMaster.Extensions.CommandLineUtils;

using Newtonsoft.Json;

namespace UnitTestToUML
{
    [HelpOption]
    class Program
    {
        #region Properties

        [Option(CommandOptionType.SingleValue, Description = "The path to the folder to read Apple unit tests from",
            ValueName = "path", ShortName = "a")]
        public string ApplePath { get; set; }

        [Option(CommandOptionType.SingleValue, Description = "The path to the file to read the program configuration from",
            ValueName = "path", ShortName = "c")]
        public string ConfigPath { get; set; }

        [Option(CommandOptionType.SingleValue, Description = "The path to the folder to read C# unit tests from",
            ValueName = "path", ShortName = "s")]
        public string CSharpPath { get; set; }

        [Option(CommandOptionType.SingleValue, Description = "The path to the folder to read Java unit tests from",
            ValueName = "path", ShortName = "j")]
        public string JavaPath { get; set; }

        [Option(CommandOptionType.SingleValue, Description = "The path to the directory to write UML output to",
            ValueName = "path", ShortName = "u")]
        public string UMLDirectory { get; set; }

        [Option(CommandOptionType.NoValue, Description = "Also write per platform unit test lists", ShortName = "v")]
        public bool Verbose { get; set; }

        #endregion

        #region Private Methods

        static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private void AppendToUML(StreamWriter fout, string packageName, int testCount, Dictionary<string, List<string>> missing)
        {
            fout.WriteLine($"namespace {packageName} {{");
            foreach (var pair in missing) {
                fout.WriteLine($"    class {pair.Key} {{");
                foreach (var method in pair.Value) {
                    fout.WriteLine($"        +{method}");
                }

                fout.WriteLine("    }");
                fout.WriteLine();
            }

            fout.WriteLine("}");
            fout.WriteLine();
            fout.WriteLine($"note top of {packageName}");
            fout.WriteLine($"{testCount} total tests scanned");
            fout.WriteLine("end note");
            fout.WriteLine();
        }

        private void DiffInto(Dictionary<string, List<string>> result, Dictionary<string, List<string>> left, Dictionary<string, List<string>> right, 
            IReadOnlyDictionary<string, string> map, IReadOnlyList<string> skip, string tag) 
        {
            foreach (var pair in left) {
                if (skip.Contains(pair.Key) || !pair.Value.Any()) {
                    continue;
                }

                if (!right.ContainsKey(pair.Key)) {
                    var all = pair.Value.Select(x => skip.Contains(x) ? null : $"{x}({tag})").Where(x => x != null).ToList();
                    if (all.Any()) {
                        result[pair.Key] = all;
                    }
                    
                    continue;
                }

                var rightList = right[pair.Key];
                foreach (var method in pair.Value) {
                    if (skip.Contains(method)) {
                        continue;
                    }

                    if (!rightList.Contains(method)) {
                        if (map.ContainsKey($"{pair.Key}.{method}")) {
                            var mapped = map[$"{pair.Key}.{method}"];
                            if (!rightList.Contains(mapped.Split('.').Last())) {
                                result.GetOrAdd(pair.Key, () => new List<string>()).Add($"{method}({tag})");
                            }
                        } else if (map.ContainsKey(method)) {
                            var mapped = map[method];
                            if (!rightList.Contains(mapped.Split('.').Last())) {
                                result.GetOrAdd(pair.Key, () => new List<string>()).Add($"{method}({tag})");
                            }
                        } else {
                            result.GetOrAdd(pair.Key, () => new List<string>()).Add($"{method}({tag})");
                        }
                    }
                }
            }
        }

        private void OnExecute()
        {
            while (UMLDirectory == null) {
                UMLDirectory = Prompt.GetString("Please enter the path to the directory to write the UML to: ");
            }

            var options = ReadConfig();

            var csharpCbl = new Dictionary<string, List<string>>();
            var csharpLiteCore = new Dictionary<string, List<string>>();

            ReadCSharp(csharpCbl, csharpLiteCore, options.SkipFiles);
            if (Verbose) {
                WriteUML("csharp.puml", csharpCbl, csharpLiteCore);
            }

            var appleCbl = new Dictionary<string, List<string>>();
            var appleLiteCore = new Dictionary<string, List<string>>();
            if (ApplePath != null) {
                ReadApple(appleCbl, appleLiteCore, options.SkipFiles);
                if (Verbose) {
                    WriteUML("apple.puml", appleCbl, appleLiteCore);
                }
            }

            var javaCbl = new Dictionary<string, List<string>>();
            var javaLiteCore = new Dictionary<string, List<string>>();
            if (JavaPath != null) {
                ReadJava(javaCbl, javaLiteCore, options.SkipFiles);
                if (Verbose) {
                    WriteUML("java.puml", javaCbl, javaLiteCore);
                }
            }

            var diff = new Diff();
            if (ApplePath != null) {
                DiffInto(diff.AppleMissing, csharpCbl, appleCbl, options.AppleFunctionMap, options.AppleSkip, "From Net");
                DiffInto(diff.AppleMissing, javaCbl, appleCbl,
                    new TranslationDictionary(options.FromJavaFunctionMap, options.AppleFunctionMap), options.AppleSkip, "From Java");
            }

            DiffInto(diff.CSharpMissing, appleCbl, csharpCbl, options.FromAppleFunctionMap, options.CSharpSkip, "From Apple");
            DiffInto(diff.CSharpMissing, javaCbl, csharpCbl, options.FromJavaFunctionMap, options.CSharpSkip, "From Java");

            if (JavaPath != null) {
                DiffInto(diff.JavaMissing, appleCbl, javaCbl,
                    new TranslationDictionary(options.FromAppleFunctionMap, options.JavaFunctionMap), options.JavaSkip, "From Apple");
                DiffInto(diff.JavaMissing, csharpCbl, javaCbl, options.JavaFunctionMap, options.JavaSkip, "From Net");
            }

            var appleCount = 0;
            var javaCount = 0;
            var netCount = 0;
            foreach (var entry in csharpCbl) {
                netCount += entry.Value.Count;
            }

            foreach (var entry in appleCbl) {
                appleCount += entry.Value.Count;
            }

            foreach (var entry in javaCbl) {
                javaCount += entry.Value.Count;
            }

            using (var fout =
                new StreamWriter(
                    File.Open(Path.Combine(UMLDirectory, "diff.puml"), FileMode.Create, FileAccess.Write, FileShare.None),
                    Encoding.UTF8, 8192, false)) {
                fout.WriteLine("@startuml");
                fout.WriteLine("skinparam backgroundColor #DDDDFF");
                AppendToUML(fout, "Apple", appleCount, diff.AppleMissing);
                AppendToUML(fout, "NET", netCount, diff.CSharpMissing);
                AppendToUML(fout, "Java", javaCount, diff.JavaMissing);
                fout.WriteLine("@enduml");
            }
        }

        private void ReadApple(Dictionary<string, List<string>> cblTests,
            Dictionary<string, List<string>> liteCoreTests, IReadOnlyList<string> skipFiles)
        {
            if (ApplePath == null) {
                return;
            }

            var di = new DirectoryInfo(ApplePath);
            if (!di.Exists) {
                throw new DirectoryNotFoundException("Path to Apple tests not found");
            }

            var testSig = new Regex("\\s*-\\s*\\(void\\)\\s*(test\\S+)\\s*{", RegexOptions.Compiled);
            foreach (var file in di.EnumerateFiles("*.m")) {
                if (skipFiles.Contains(file.Name)) {
                    continue;
                }

                using (var fin = new StreamReader(File.OpenRead(file.FullName))) {
                    string testName = null;
                    string nextLine;
                    while ((nextLine = fin.ReadLine()?.Trim()) != null) {
                        if (testName == null && nextLine.StartsWith("@implementation")) {
                            var implName = nextLine.Split(' ').Last(x => x != "{");
                            if (implName.EndsWith("Test")) {
                                testName = implName;
                                cblTests[testName] = new List<string>();
                            }
                        } else if (testSig.IsMatch(nextLine)) {
                            var groupValue = testSig.Match(nextLine).Groups[1].Value;
                            var nextTest = Char.ToUpperInvariant(groupValue[0]) + groupValue.Substring(1);
                            cblTests[testName].Add(nextTest);
                        }
                    }
                }
            }
        }

        private Config ReadConfig()
        {
            if (ConfigPath == null) {
                return new Config();
            }

            using (var jsonReader =
                new JsonTextReader(new StreamReader(File.OpenRead(ConfigPath), Encoding.UTF8, false, 8192, false))
                    { CloseInput = true }) {
                var serializer = JsonSerializer.CreateDefault();
                var retVal = serializer.Deserialize<Config>(jsonReader);
                retVal.Populate();
                return retVal;
            }
        }

        private void ReadCSharp(Dictionary<string, List<string>> cblTests, Dictionary<string, List<string>> liteCoreTests, IReadOnlyList<string> skipFiles)
        {
            while (CSharpPath == null) {
                CSharpPath = Prompt.GetString("Please enter the path to the folder to read C# unit tests from: ");
            }

            var di = new DirectoryInfo(CSharpPath);
            if (!di.Exists) {
                throw new DirectoryNotFoundException("Path to C# tests not found");
            }

            var classSig = new Regex("\\s*public (?:sealed )?class (\\S+)", RegexOptions.Compiled);
            var useNextLine = false;
            foreach (var file in di.EnumerateFiles("*.cs")) {
                if (skipFiles.Contains(file.Name)) {
                    continue;
                }

                using (var fin = new StreamReader(File.OpenRead(file.FullName))) {
                    string testName = null;
                    string nextLine;
                    while ((nextLine = fin.ReadLine()?.Trim()) != null) {
                        if (testName == null && classSig.IsMatch(nextLine)) {
                            var implName = classSig.Match(nextLine).Groups[1].Value;
                            if (implName.EndsWith("Test")) {
                                testName = implName;
                                cblTests[testName] = new List<string>();
                            }
                        } else if (useNextLine) {
                            var split = nextLine.Split(' ');
                            if (split.Length < 3) {
                                continue;
                            }

                            var nextMethod = split.First(x => x.StartsWith("Test"));
                            cblTests[testName].Add(nextMethod.Trim('(', ')'));
                            useNextLine = false;
                        } else {
                            useNextLine = nextLine == "[Fact]";
                        }
                    }
                }
            }
        }

        private void ReadJava(Dictionary<string, List<string>> cblTests, Dictionary<string, List<string>> liteCoreTests, IReadOnlyList<string> skipFiles)
        {
            if (JavaPath == null) {
                return;
            }

            var di = new DirectoryInfo(JavaPath);
            if (!di.Exists) {
                throw new DirectoryNotFoundException("Path to Java tests not found");
            }

            var useNextLine = false;
            foreach (var file in di.EnumerateFiles("*.java")) {
                if (skipFiles.Contains(file.Name)) {
                    continue;
                }

                using (var fin = new StreamReader(File.OpenRead(file.FullName))) {
                    string testName = null;
                    string nextLine;
                    while ((nextLine = fin.ReadLine()?.Trim()) != null) {
                        if (testName == null && nextLine.StartsWith("public class")) {
                            var implName = nextLine.Split(' ')[2];
                            if (implName.EndsWith("Test")) {
                                testName = implName;
                                cblTests[testName] = new List<string>();
                            }
                        } else if (useNextLine) {
                            var split = nextLine.Split(' ');
                            if (split.Length < 3) {
                                continue;
                            }

                            var nextMethod = split.First(x => x.StartsWith("test"));
                            cblTests[testName].Add(Char.ToUpperInvariant(nextMethod[0]) + nextMethod.Substring(1).Trim('(', ')'));
                            useNextLine = false;
                        } else {
                            useNextLine = nextLine == "@Test";
                        }
                    }
                }
            }
        }

        private void WriteUML(string filename, Dictionary<string, List<string>> cblTests, Dictionary<string, List<string>> liteCoreTests)
        {
            using (var fout =
                new StreamWriter(File.Open(Path.Combine(UMLDirectory, filename), FileMode.Create, FileAccess.Write, FileShare.None),
                    Encoding.UTF8, 8192, false)) {
                fout.WriteLine("@startuml");
                fout.WriteLine("skinparam backgroundColor #DDDDFF");

                fout.WriteLine("package \"CBL Tests\" {");
                foreach (var pair in cblTests) {
                    if (!pair.Value.Any()) {
                        continue;
                    }

                    fout.WriteLine($"    class {pair.Key} {{");
                    foreach (var method in pair.Value) {
                        fout.WriteLine($"        +{method}()");
                    }

                    fout.WriteLine("    }");
                    fout.WriteLine();
                }

                fout.WriteLine("}");
                fout.WriteLine();
                fout.WriteLine("package \"LiteCore Tests\" {");
                foreach (var pair in liteCoreTests) {
                    if (!pair.Value.Any()) {
                        continue;
                    }

                    fout.WriteLine($"    class {pair.Key} {{");
                    foreach (var method in pair.Value) {
                        fout.WriteLine($"        +{method}()");
                    }

                    fout.WriteLine("    }");
                    fout.WriteLine();
                }

                fout.WriteLine("}");
                fout.WriteLine("@enduml");
                fout.Flush();
            }
        }

        #endregion
    }
}
