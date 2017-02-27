#region Copyright MIT License
/*
 * Copyright © 2016 Sergey Shevelev, François St-Arnaud and John Wood
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * Based on work by François St-Arnaud (http://codeproject.com/SolGen)
 */
#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SolGen
{
    /// <summary>
    /// A utility for fixing project references and generating solution files.
    /// </summary>
    internal class SolGenMain
    {
        private const string GeneratedSolutionFileNameSuffixConfigPropertyName = "GeneratedSolutionFileNameSuffix";
        private const string BuildConfigurationsConfigPropertyName = "BuildConfigurations";

        private readonly Dictionary<string, object> _args;

        public static void Main(string[] args)
        {
            SolGenMain program = new SolGenMain(args);
            program.Run();
        }

        private SolGenMain(string[] args)
        {
            _args = ParseArgs(args);
        }

        private void Run()
        {
            string buildConfigurationsString = GetArg("configs", ConfigurationManager.AppSettings[BuildConfigurationsConfigPropertyName] ?? string.Empty);
            string [] buildConfigurations = buildConfigurationsString.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                string [] projectFiles = Directory.GetFiles(".", "*.*proj", SearchOption.TopDirectoryOnly);
                if (projectFiles.Length == 0)
                {
                    Console.Error.WriteLine("No project files found");
                    return;
                }

                if (projectFiles.Length > 1)
                {
                    Console.Error.WriteLine("Multiple project files found");
                    return;
                }

                string filePath = Path.GetFullPath(projectFiles[0]);
                var solutionPath = GetSolutionFileName(Path.GetDirectoryName(filePath) + "\\..", Path.GetFileNameWithoutExtension(filePath));

                var maker = new SolutionMaker(solutionPath, buildConfigurations);
                maker.AddProject(Path.GetFullPath(projectFiles[0]));
                maker.CreateSolution();

                string vsPath = Environment.GetEnvironmentVariable("SOLGEN_VS_PATH");
                if (string.IsNullOrEmpty(vsPath))
                {
                    Process.Start(solutionPath);
                }
                else
                {
                    Process.Start(vsPath, solutionPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("A problem occurred running SolGen and the generation did not complete.");
                Console.WriteLine("Exception: " + e.Message + " (" + e.GetType().Name + ")");
                Console.WriteLine(e.StackTrace);
            }
        }

        private static string GetSolutionFileName(string directory, string filename)
        {
            string fileNameSuffix = ConfigurationManager.AppSettings[GeneratedSolutionFileNameSuffixConfigPropertyName] ?? string.Empty;
            
            return Path.GetFullPath(Path.Combine(directory, "Generated." + filename + fileNameSuffix + ".sln"));
        }

        private T GetArg<T>(string argName, T defaultValue)
        {
            object value;

            if (_args.TryGetValue(argName, out value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            return defaultValue;
        }

        private static Dictionary<string, object> ParseArgs(string[] args)
        {
            var ret = new Dictionary<string, object>();

            foreach (string arg in args)
            {
                Match m = Regex.Match(arg, "^/(?<name>[a-zA-Z_0-9]+)(:(?<value>.*))?$");

                if (m.Success)
                {
                    string value = m.Groups["value"].Success ? m.Groups["value"].Value : "true";
                    ret[m.Groups["name"].Value.ToLower()] = value;
                }
                else
                {
                    object obj;
                    List<string> list;
                    if (!ret.TryGetValue("", out obj))
                    {
                        list = new List<string>();
                        ret[""] = list;
                    }
                    else
                    {
                        list = (List<string>)obj;
                    }

                    list.Add(arg);
                }
            }

            return ret;
        }
    }
}
