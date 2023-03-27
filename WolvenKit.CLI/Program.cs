using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;
using WolvenKit.Common.Extensions;
using WolvenKit.CR2W;
using WolvenKit.CR2W.JSON;

namespace WolvenKit.CLI
{
    public class Options
    {
        [Option("input", Required = true, HelpText = "CR2W/JSON file input path")]
        public string InputPath { get; set; }

        [Option("output", Required = false, HelpText = "CR2W/JSON file output path")]
        public string OutputPath { get; set; }
        [Option("cr2w2info", Required = false, HelpText = "Dump CR2W partial info into JSON")]
        public bool DumpCR2W { get; set; }

        [Option("cr2w2json", Required = false, HelpText = "Export CR2W to JSON")]
        public bool ExportJSON { get; set; }

        [Option("json2cr2w", Required = false, HelpText = "Import JSON to CR2W")]
        public bool ImportJSON { get; set; }

        [Option("bytes_as_list", Required = false, HelpText = "Output byte array vars as int list (by default as base64 string)")]
        public bool BytesAsIntList { get; set; } = false;

        [Option("ignore_embedded_cr2w", Required = false, HelpText = "Do NOT serialize embedded cr2w bytearrays - flatCompiledData, etc (serialized by default if possible)")]
        public bool IgnoreEmbeddedCR2W { get; set; } = false;

        [Option("verbose", Required = false, HelpText = "Print verbose info")]
        public bool Verbose { get; set; } = false;
    }
    class Program
    {
        public static void PrintColor(ConsoleColor color, string text)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = oldColor;
        }
        public static void Print(string text) => PrintColor(ConsoleColor.Yellow, text);
        public static void PrintError(string text) => PrintColor(ConsoleColor.Red, text);
        public static void PrintOK(string text) => PrintColor(ConsoleColor.Green, text);
        static void Main(string[] args)
        {
            Debug.WriteLine($"args = {string.Join(";", args)}");
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                                    .WithParsed(RunCLIOptions)
                                    .WithNotParsed(HandleParseError);
            /*if (args.Length > 0)
            {
                if (args[0] == "-cr2w2json" && args.Length > 1)
                {
                    if (File.Exists(args[1]))
                    {
                        if (args.Length == 2)
                        {
                            args = args.Append(args[1] + ".json").ToArray();
                        }
                        PrintOK($"Exporting JSON..\nInput CR2W: {args[1]}\nOutput JSON: {args[2]}");
                        Stopwatch watch = new Stopwatch();
                        watch.Start();

                        if (!CR2WJsonTool.ExportJSON(args[1], args[2]))
                        {
                            PrintError($"ERROR exporting JSON!");
                        }
                        TimeSpan ts = watch.Elapsed;
                        PrintOK($"Finished in {(int)ts.TotalSeconds}.{(int)ts.TotalMilliseconds} s");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        System.Environment.Exit(0);
                    }
                } else if (args[0] == )
            }*/

        }

        static void RunCLIOptions(Options opts)
        {
            Print($"Input = {opts.InputPath}, Output = {opts.OutputPath}");
            if (!File.Exists(opts.InputPath))
            {
                PrintError($"File does not exist: {opts.InputPath}");
                System.Environment.Exit(-1);
            }
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var ToolOptions = new CR2WJsonToolOptions(opts.Verbose, opts.BytesAsIntList, opts.IgnoreEmbeddedCR2W);
            if (opts.ExportJSON)
            {
                if (string.IsNullOrEmpty(opts.OutputPath))
                    opts.OutputPath = opts.InputPath + ".json";

                Print($"Exporting JSON..\nInput CR2W: {opts.InputPath}\nOutput JSON: {opts.OutputPath}");
                if (!CR2WJsonTool.ExportJSON(opts.InputPath, opts.OutputPath, ToolOptions))
                {
                    PrintError($"ERROR exporting JSON!");
                }
            } else if (opts.ImportJSON)
            {
                if (string.IsNullOrEmpty(opts.OutputPath))
                    opts.OutputPath = opts.InputPath.TrimEnd(".json");

                Print($"Importing JSON..\nInput JSON: {opts.InputPath}\nOutput CR2W: {opts.OutputPath}");
                if (!CR2WJsonTool.ImportJSON(opts.InputPath, opts.OutputPath, ToolOptions))
                {
                    PrintError($"ERROR importing JSON!");
                }
            }
            else if (opts.DumpCR2W)
            {
                if (string.IsNullOrEmpty(opts.OutputPath))
                    opts.OutputPath = opts.InputPath + ".info.json";

                Print($"Dumping info to JSON..\nInput CR2W: {opts.InputPath}\nOutput JSON: {opts.OutputPath}");
                if (!CR2WScripts.DumpInfo(opts.InputPath, opts.OutputPath))
                {
                    PrintError($"ERROR dumping JSON!");
                }
            }
            else
            {
                Print($"No action specified.");
            }
            TimeSpan ts = watch.Elapsed;
            PrintOK($"Finished in {(int)ts.TotalSeconds}.{(int)ts.TotalMilliseconds} s");
            System.Environment.Exit(0);
        }
        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }
    }
}
