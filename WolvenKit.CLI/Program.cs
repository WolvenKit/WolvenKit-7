using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;
using WolvenKit.Common.Extensions;
using WolvenKit.CR2W;
using WolvenKit.CR2W.Types;
using WolvenKit.CR2W.JSON;
using WolvenKit.CR2W.SRT;

namespace WolvenKit.CLI
{
    public class Options
    {
        [Option("input", Required = true, HelpText = "CR2W/JSON/SRT file input path")]
        public string InputPath { get; set; }

        [Option("output", Required = false, HelpText = "CR2W/JSON file output path")]
        public string OutputPath { get; set; }

        [Option("cr2w2info", Required = false, HelpText = "Dump CR2W partial info into JSON")]
        public bool DumpCR2W { get; set; }

        [Option("srt2json", Required = false, HelpText = "Export SRT (SpeedTree) into JSON")]
        public bool ExportSRT { get; set; }

        [Option("json2srt", Required = false, HelpText = "Import JSON into SRT (SpeedTree)")]
        public bool ImportSRT { get; set; }

        [Option("cr2w2json", Required = false, HelpText = "Export CR2W to JSON")]
        public bool ExportJSON { get; set; }

        [Option("json2cr2w", Required = false, HelpText = "Import JSON to CR2W")]
        public bool ImportJSON { get; set; }

        [Option("bytes_as_list", Required = false, HelpText = "Output byte array vars as int list (by default as base64 string)")]
        public bool BytesAsIntList { get; set; } = false;

        [Option("guids_as_strings", Required = false, HelpText = "Output CGUID vars as string values (by default as bytearray)")]
        public bool GuidAsString { get; set; } = false;

        [Option("ignore_embedded_cr2w", Required = false, HelpText = "Do NOT serialize embedded cr2w bytearrays - flatCompiledData, etc (serialized by default if possible)")]
        public bool IgnoreEmbeddedCR2W { get; set; } = false;

        [Option("cr2w_lipsync_to_raw", Required = false, HelpText = "Convert oldgen CR2W lipsync to nextgen raw format")]
        public bool LipsyncConvertCR2WToRaw { get; set; } = false;

        [Option("verbose", Required = false, HelpText = "Print verbose info")]
        public bool Verbose { get; set; } = false;
    }
    class Program
    {
        public static uint GetValueOrZeroUInt32(CUInt32 var)
        {
            return (var != null) ? var.val : (uint)0;
        }
        public static ushort GetValueOrZeroUInt16(CUInt16 var)
        {
            return (var != null) ? var.val : (ushort)0;
        }
        public static sbyte GetValueOrZeroInt8(CInt8 var)
        {
            return (var != null) ? var.val : (sbyte)0;
        }
        public static float GetValueOrZeroFloat(CFloat var)
        {
            return (var != null) ? var.val : (float)0.0;
        }
        public static bool Run_LipsyncConvertCR2WToRaw(String inputPath, String outputPath)
        {
            var cr2w = CR2WJsonTool.ReadCR2W(inputPath);
            if (cr2w == null)
            {
                return false;
            }
            if (cr2w.chunks[0].REDType != "CSkeletalAnimation")
            {
                PrintError($"CR2W Root chunk is {cr2w.chunks[0].REDType}, expected CSkeletalAnimation!");
                return false;
            }
            CSkeletalAnimation anim = cr2w.chunks[0].data as CSkeletalAnimation;
            if (anim.AnimBuffer == null || anim.AnimBuffer.Reference.data == null)
            {
                PrintError($"CR2W AnimBuffer is null!");
                return false;
            }
            CAnimationBufferBitwiseCompressed animBuffer = anim.AnimBuffer.Reference.data as CR2W.Types.CAnimationBufferBitwiseCompressed;

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(anim.FramesPerSecond.val);
                writer.Write(anim.Duration.val);

                if (animBuffer.Bones != null)
                {
                    var bonesList = animBuffer.Bones.Elements;
                    writer.WriteVLQInt32(bonesList.Count);
                    for (int i = 0; i < bonesList.Count; i += 1)
                    {
                        var list = new List<SAnimationBufferBitwiseCompressedData> { bonesList[i].Position, bonesList[i].Orientation };
                        foreach (var compressedData in list)
                        {
                            writer.Write(GetValueOrZeroFloat(compressedData.Dt));
                            writer.Write(GetValueOrZeroInt8(compressedData.Compression));
                            writer.Write((sbyte)0);  // alignment!
                            writer.Write(GetValueOrZeroUInt16(compressedData.NumFrames));
                            writer.Write(GetValueOrZeroUInt32(compressedData.DataAddr));
                            writer.Write(GetValueOrZeroUInt32(compressedData.DataAddrFallback));
                        }
                    }
                } else
                {
                    writer.WriteVLQInt32(0);
                }

                if (animBuffer.Tracks != null)
                {
                    var tracksList = animBuffer.Tracks.Elements;
                    writer.WriteVLQInt32(tracksList.Count);
                    for (int i = 0; i < tracksList.Count; i += 1)
                    {
                        writer.Write(GetValueOrZeroFloat(tracksList[i].Dt));
                        writer.Write(GetValueOrZeroInt8(tracksList[i].Compression));
                        writer.Write((sbyte)0);  // alignment!
                        writer.Write(GetValueOrZeroUInt16(tracksList[i].NumFrames));
                        writer.Write(GetValueOrZeroUInt32(tracksList[i].DataAddr));
                        writer.Write(GetValueOrZeroUInt32(tracksList[i].DataAddrFallback));
                    }
                } else
                {
                    writer.WriteVLQInt32(0);
                }

                if (animBuffer.Data != null)
                {
                    var data = animBuffer.Data;
                    writer.WriteVLQInt32(data.Bytes.Length);
                    if (data.Bytes.Length > 0)
                        writer.Write(data.Bytes);
                } else
                {
                    writer.WriteVLQInt32(0);
                }
                writer.Write(GetValueOrZeroUInt32(animBuffer.NumFrames));
                writer.Write(GetValueOrZeroFloat(animBuffer.Dt));
            }
            return true;
        }
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
            var ToolOptions = new CR2WJsonToolOptions(opts.Verbose, opts.BytesAsIntList, opts.IgnoreEmbeddedCR2W, opts.GuidAsString);
            if (opts.ExportJSON)
            {
                if (string.IsNullOrEmpty(opts.OutputPath))
                    opts.OutputPath = opts.InputPath + ".json";

                Print($"Exporting CR2W->JSON..\nInput CR2W: {opts.InputPath}\nOutput JSON: {opts.OutputPath}");
                if (!CR2WJsonTool.ExportJSON(opts.InputPath, opts.OutputPath, ToolOptions))
                {
                    PrintError($"ERROR exporting JSON!");
                }
            } else if (opts.ImportJSON)
            {
                if (string.IsNullOrEmpty(opts.OutputPath))
                    opts.OutputPath = opts.InputPath.TrimEnd(".json");

                Print($"Importing JSON->CR2W..\nInput JSON: {opts.InputPath}\nOutput CR2W: {opts.OutputPath}");
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
            else if (opts.ExportSRT)
            {
                if (string.IsNullOrEmpty(opts.OutputPath))
                    opts.OutputPath = opts.InputPath + ".json";

                Print($"Exporting SRT->JSON..\nInput SRT: {opts.InputPath}\nOutput JSON: {opts.OutputPath}");
                var SRT = new Srtfile();
                SRT.ReadFromSRT(opts.InputPath);
                SRT.WriteToJsonFile(opts.OutputPath);
            }
            else if (opts.ImportSRT)
            {
                if (string.IsNullOrEmpty(opts.OutputPath))
                    opts.OutputPath = opts.InputPath.TrimEnd(".json");

                Print($"Importing JSON->SRT..\nInput JSON: {opts.InputPath}\nOutput SRT: {opts.OutputPath}");
                var SRT = Srtfile.ReadFromJSON(opts.InputPath);
                SRT.WriteToSRTFile(opts.OutputPath);
            }
            else if (opts.LipsyncConvertCR2WToRaw)
            {
                if (string.IsNullOrEmpty(opts.OutputPath))
                    opts.OutputPath = opts.InputPath.TrimEnd(".json") + ".raw";
                Run_LipsyncConvertCR2WToRaw(opts.InputPath, opts.OutputPath);
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
