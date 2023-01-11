using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JsonSubTypes;
using Newtonsoft.Json;
using WolvenKit.Common.Model;
using WolvenKit.CR2W;
using WolvenKit.CR2W.JSON;
using WolvenKit.CR2W.Types;

namespace WolvenKit.CR2W.JSON
{
    public class CR2WJsonToolOptions
    {
        public CR2WJsonToolOptions() { }
        public CR2WJsonToolOptions(bool verbose, bool bytesAsIntList, bool ignoreEmbeddedCR2W)
        {
            Verbose = verbose;
            BytesAsIntList = bytesAsIntList;
            IgnoreEmbeddedCR2W = ignoreEmbeddedCR2W;
        }

        public bool Verbose { get; set; } = false;
        public bool BytesAsIntList { get; set; } = false;
        public bool IgnoreEmbeddedCR2W { get; set; } = false;
    }

    public class CR2WJsonTool
    {
        /* Predefined static names */
        public static string m_varChunkHandle = "_chunkHandle";
        public static string m_varReference = "_reference";
        public static string m_varClassName = "_className";
        public static string m_varDepotPath = "_depotPath";
        public static string m_varFlags = "_flags";
        public static string m_varFlag = "_flag";
        public static string m_varType = "_type";
        public static string m_varName = "_name";
        public static string m_varValue = "_value";
        public static string m_varVariant = "_variant";

        public static void PrintColor(ConsoleColor color, string text)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = oldColor;
        }
        public static void Print(string text) => PrintColor(ConsoleColor.Yellow, text);
        public static void PrintError(string text)
        {
            Debug.WriteLine(text);
            PrintColor(ConsoleColor.Red, text);
        }
        public static void PrintOK(string text) => PrintColor(ConsoleColor.Green, text);

        public CR2WJsonTool() { }

        public static string LogIndent(int level) => new string(' ', level * 2);

        public static CR2WFile ReadCR2W(string cr2wPath)
        {
            using (var fs = new FileStream(cr2wPath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                // read main cr2w
                var cr2w = new CR2WFile();
                EFileReadErrorCodes ret;
                ret = cr2w.Read(reader);
                if (ret == EFileReadErrorCodes.NoError)
                {
                    PrintOK($"[ReadCR2W] OK ({fs.Length} bytes)");
                    return cr2w;
                }
                else
                {
                    PrintError($"[ReadCR2W] ERROR ({ret})");
                    return null;
                }
            }
        }

        public static void WriteCR2W(CR2WFile cr2w, string cr2wPath)
        {
            using (var fs = new FileStream(cr2wPath, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new BinaryWriter(fs))
            {
                cr2w.Write(writer);
            }
        }

        public static bool ImportJSON(string jsonPath, string cr2wPath, CR2WJsonToolOptions options)
        {
            var jsonText = File.ReadAllText(jsonPath);
            if (string.IsNullOrEmpty(jsonText))
            {
                return false;
            }

            var deserializeSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore
            };
            deserializeSettings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                                                .Of(typeof(CR2WJsonObject))
                                                .SetFallbackSubtype(typeof(CR2WJsonMap))
                                                .RegisterSubtypeWithProperty(typeof(CR2WJsonScalar), "_value")
                                                .RegisterSubtypeWithProperty(typeof(CR2WJsonArray), "_elements")
                                                .RegisterSubtypeWithProperty(typeof(CR2WJsonChunkMap), "_parentKey")
                                                .RegisterSubtypeWithProperty(typeof(CR2WJsonData), "_extension")
                                                .Build());

            var jsonRoot = JsonConvert.DeserializeObject<CR2WJsonData>(jsonText, deserializeSettings);
            if (jsonRoot == null)
            {
                return false;
            }

            var cr2w = DewalkCR2W(jsonRoot, 0, options);
            if (cr2w == null)
            {
                return false;
            }

            WriteCR2W(cr2w, cr2wPath);
            return true;
        }

        protected static CR2WFile DewalkCR2W(CR2WJsonData jsonCR2W, int logLevel, CR2WJsonToolOptions options)
        {
            var cr2w = new CR2WFile();
            if (jsonCR2W == null)
            {
                PrintError($"{LogIndent(logLevel)}[DewalkCR2W] Invaid json node");
                return cr2w;
            }
            PrintOK($"{LogIndent(logLevel)}CR2W: {(jsonCR2W.extension != "" ? jsonCR2W.extension : "<root>")}");
            logLevel += 1;

            /* CR2W Names - skip, will be generated automatically */

            /* CR2W Imports - Skip, will be generated automatically? */

            /* CR2W Embedds - CHECKME ? */
            foreach (var embed in jsonCR2W.embedded)
            {
                var wrapper = new CR2WEmbeddedWrapper
                {
                    ClassName = embed["className"] as string,
                    ImportClass = embed["importClass"] as string,
                    ImportPath = embed["importPath"] as string,
                    Handle = embed["handle"] as string
                };
                cr2w.embedded.Add(wrapper);
            }
            /* CR2W Properties - CHECKME ? */
            cr2w.properties = jsonCR2W.properties;

            /* CR2W Buffers - CHECKME ? */
            cr2w.buffers = jsonCR2W.buffers;

            /* CR2W Chunks */
            var chunkByKey = new Dictionary<string, CR2WExportWrapper>();
            foreach (var kv_chunk in jsonCR2W.chunks)
            {
                var chunk = kv_chunk.Value;
                CR2WExportWrapper cr2wChunk = null;
                if (chunk.parentKey == "")
                {
                    cr2wChunk = cr2w.CreateChunk(chunk.type, cr2w.chunks.Count);
                    if (options.Verbose)
                    {
                        PrintOK($"{LogIndent(logLevel)}[DewalkCR2W] Create top chunk #{cr2w.chunks.Count}: {chunk.chunkKey}");
                    }
                }
                else
                {
                    if (!chunkByKey.ContainsKey(chunk.parentKey))
                    {
                        PrintError($"{LogIndent(logLevel)}[DewalkCR2W] Can't find parent chunk by key: {chunk.parentKey}");
                        continue;
                    }
                    cr2wChunk = cr2w.CreateChunk(chunk.type, cr2w.chunks.Count, chunkByKey[chunk.parentKey], chunkByKey[chunk.parentKey]);
                    // ? data.SetREDFlags(Export.objectFlags);
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}[DewalkCR2W] Create chunk #{cr2w.chunks.Count}: {chunk.chunkKey} with parent = {chunk.parentKey}");
                    }
                }
                if (cr2wChunk == null)
                {
                    PrintError($"{LogIndent(logLevel)}[DewalkCR2W] NULL chunk created: {chunk.chunkKey}");
                    continue;
                }
                chunkByKey[kv_chunk.Key] = cr2wChunk;
            }
            foreach (var kv_chunk in jsonCR2W.chunks)
            {
                var jsonChunk = kv_chunk.Value;
                var cr2wChunk = chunkByKey[kv_chunk.Key];
                PrintOK($"{LogIndent(logLevel)}[DewalkCR2W] Chunk {kv_chunk.Key} ({cr2wChunk.REDType})");
                DewalkNode(cr2wChunk.data, jsonChunk, chunkByKey, jsonCR2W.extension, logLevel + 1, options);
                cr2wChunk.REDObjectFlags = jsonChunk.flags;
                if (jsonChunk.unknownBytes != null && jsonChunk.unknownBytes.Length > 0)
                {
                    cr2wChunk.unknownBytes = new CBytes(cr2w, cr2wChunk.data, "unknownBytes")
                    {
                        Bytes = jsonChunk.unknownBytes
                    };
                }
            }

            /* Additional bytes */
            if (jsonCR2W.additionalBytes != null && jsonCR2W.additionalBytes.Length > 0)
            {
                cr2w.AdditionalCr2WFileBytes = jsonCR2W.additionalBytes;
            }

            return cr2w;
        }

        protected static void DewalkNode(CVariable cvar, CR2WJsonObject node, Dictionary<string, CR2WExportWrapper> chunkByKey, string extension, int logLevel, CR2WJsonToolOptions options)
        {
            if (options.Verbose)
            {
                Print($"{LogIndent(logLevel)}[DewalkNode] {extension}{cvar.GetFullName()} ({cvar.REDType})");
            }

            if (node == null)
            {
                PrintError($"{LogIndent(logLevel)}[DewalkNode] Invaid json node for {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return;
            }
            if (cvar == null)
            {
                PrintError($"{LogIndent(logLevel)}[DewalkNode] Invaid CVariable {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return;
            }
            if (cvar.REDType != node.type && !(node is CR2WJsonData) && !(cvar is CByteArray) && cvar.REDName != "SBlockDataPackedObject")
            {
                PrintError($"{LogIndent(logLevel)}[DewalkNode] Json node type ({node.type}) don't match {extension}{cvar.GetFullName()} ({cvar.REDType})");
            }

            // wrap special types first
            if (cvar is IPtrAccessor)
            {
                DewalkPtrNode(cvar, node as CR2WJsonMap, chunkByKey, extension, logLevel, options);
                return;
            }
            else if (cvar is IHandleAccessor)
            {
                if (!(node is CR2WJsonMap map) || !map.vars.ContainsKey(m_varChunkHandle))
                {
                    PrintError($"{LogIndent(logLevel)}[DewalkNode] {m_varChunkHandle} param not found in {extension}{cvar.GetFullName()} ({cvar.REDType})");
                    return;
                }
                var isChunkHandle = Convert.ToBoolean((map.vars[m_varChunkHandle] as CR2WJsonScalar)?.value);
                if (isChunkHandle)
                {
                    DewalkPtrNode(cvar, map, chunkByKey, extension, logLevel, options);
                }
                else
                {
                    DewalkSoftNode(cvar, map, chunkByKey, extension, logLevel, options);
                }

                return;
            }
            else if (cvar is ISoftAccessor)
            {
                if (!(node is CR2WJsonMap map))
                {
                    PrintError($"{LogIndent(logLevel)}[DewalkNode] {m_varChunkHandle} param not found in {extension}{cvar.GetFullName()} ({cvar.REDType})");
                    return;
                }
                DewalkSoftNode(cvar, map, chunkByKey, extension, logLevel, options);
                return;
            }
            else if (cvar is IVariantAccessor)
            {
                var map = node as CR2WJsonMap;
                var variantName = (map.vars[m_varName] as CR2WJsonScalar).value as string;
                var variantAccessor = cvar as IVariantAccessor;
                variantAccessor.Variant = CR2WTypeManager.Create(map.vars[m_varVariant].type, variantName, cvar.cr2w, cvar);
                variantAccessor.Variant.SetIsSerialized();
                cvar.SetREDName(variantName);
                DewalkNode(variantAccessor.Variant, map.vars[m_varVariant], chunkByKey, extension, logLevel + 1, options);
                return;
            }

            var cvarsByName = cvar.GetEditableVariables().ToDictionary(x => x.REDName, x => x as CVariable);
            switch (node)
            {
                case CR2WJsonScalar scalar:
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}[DewalkNode] PRIMITIVE {extension}{cvar.GetFullName()} ({cvar.REDType}) = {scalar.value}");
                    }

                    if (cvar is IEnumAccessor)
                    {
                        var enumStr = scalar.value as string;
                        cvar.SetValue(enumStr.Split('|').ToList());
                        break;
                    }
                    else if (cvar is IdTag idtag)
                    {
                        var values = (scalar.value as string).Split(':');
                        if (values.Length != 2)
                        {
                            PrintError($"{LogIndent(logLevel)}[DewalkNode] Invalid value {scalar.value} for IdTag {extension}{cvar.GetFullName()} ({cvar.REDType}), expected \"<flag_byte>:<guid_bytes[16] as base64 str>\"");
                            return;
                        }
                        cvar.SetValue(Convert.ToInt32(values[0]));
                        cvar.SetValue(values[1]);
                        break;
                    }
                    else if (cvar is IByteSource || cvar is CGUID)
                    {
                        if (cvar is CByteArray cba)
                        {
                            cba.InternalType = scalar.type;
                        }
                        if (scalar.value is string base64str)
                        {
                            cvar.SetValue(Convert.FromBase64String(base64str));
                        }
                        else if (scalar.value is byte[] byteArray)
                        {
                            cvar.SetValue(byteArray);
                        }
                        else if (scalar.value is Newtonsoft.Json.Linq.JArray jArray)
                        {
                            var byteArray2 = jArray.ToObject<byte[]>();
                            cvar.SetValue(byteArray2);
                        }
                        else
                        {
                            PrintError($"{LogIndent(logLevel)}[DewalkNode] Unknown type {scalar.value.GetType().FullName} for IByteSource {extension}{cvar.GetFullName()} ({cvar.REDType})");
                        }
                        break;
                    }
                    cvar.SetValue(scalar.value);
                    break;
                case CR2WJsonArray array:
                    if (cvar is IArrayAccessor arrayAccessor)
                    {
                        if (options.Verbose)
                        {
                            Print($"{LogIndent(logLevel)}[DewalkNode] ARRAY {extension}{cvar.GetFullName()} ({cvar.REDType}) - {array.elements.Count} items");
                        }

                        var index = 0;
                        // special cases
                        if (cvar.REDType.StartsWith("CPaddedBuffer"))
                        {
                            if (array.bufferPadding == null)
                            {
                                PrintError($"{LogIndent(logLevel)}[DewalkNode] bufferPadding var expected for array: {extension}{cvar.GetFullName()} ({cvar.REDType}), defaulting to 0");
                                array.bufferPadding = 0.0f;
                            }
                            (cvar.accessor[cvar, "padding"] as CFloat)?.SetValue(array.bufferPadding);
                        }
                        foreach (var obj in array.elements)
                        {
                            // auto-renamed on AddVariable
                            CVariable newElement = null;
                            if (arrayAccessor.InnerType == typeof(IReferencable))
                            {
                                newElement = CR2WTypeManager.Create(array.elements[index].type, arrayAccessor.Count.ToString(), cvar.cr2w, cvar);
                            }
                            else
                            {
                                newElement = CR2WTypeManager.Create(arrayAccessor.Elementtype, arrayAccessor.Count.ToString(), cvar.cr2w, cvar);
                            }
                            if (!cvar.CanAddVariable(newElement))
                            {
                                PrintError($"{LogIndent(logLevel)}[DewalkNode] Can't add {newElement.REDType} type to array: {extension}{cvar.GetFullName()} ({cvar.REDType})");
                                break;
                            }
                            cvar.AddVariable(newElement);
                            // renamed later if IVariantAccessor
                            DewalkNode(newElement, obj, chunkByKey, extension, logLevel + 1, options);
                            index += 1;
                        }
                        cvar.SetIsSerialized();
                    }
                    else
                    {
                        PrintError($"{LogIndent(logLevel)}[DewalkNode] CVar is not an array type: {extension}{cvar.GetFullName()} ({cvar.REDType})");
                    }
                    break;
                case CR2WJsonChunkMap cmap:
                    foreach (var subnode in cmap.vars)
                    {
                        if (!cvarsByName.ContainsKey(subnode.Key))
                        {
                            PrintError($"{LogIndent(logLevel)}[DewalkNode] Var {subnode.Key} not found in {extension}{cvar.GetFullName()} ({cvar.REDType})");
                            continue;
                        }
                        DewalkNode(cvarsByName[subnode.Key], subnode.Value, chunkByKey, extension, logLevel + 1, options);
                    }
                    cvar.SetIsSerialized();
                    break;
                case CR2WJsonMap map:
                    foreach (var subnode in map.vars)
                    {
                        // hack for SBlockDataPackedObject..
                        if (subnode.Key == "SBlockDataPackedObject" && cvar is SBlockData blockData)
                        {
                            var enumType = (subnode.Value.type as string).Split(':').First();
                            Enum.TryParse<Enums.BlockDataObjectType>(enumType, out Enums.BlockDataObjectType objType);
                            blockData.packedObjectType = objType;
                            blockData.CreatePackedObject();
                            cvarsByName[subnode.Key] = blockData.SBlockDataPackedObject;
                        }
                        if (!cvarsByName.ContainsKey(subnode.Key))
                        {
                            PrintError($"{LogIndent(logLevel)}[DewalkNode] Var {subnode.Key} not found in {extension}{cvar.GetFullName()} ({cvar.REDType})");
                            continue;
                        }
                        DewalkNode(cvarsByName[subnode.Key], subnode.Value, chunkByKey, extension, logLevel + 1, options);
                    }
                    cvar.SetIsSerialized();
                    break;
                case CR2WJsonData cr2wData:
                    var subCR2W = DewalkCR2W(cr2wData, logLevel + 1, options);
                    byte[] subCR2WBytes = null;
                    subCR2W.Write(ref subCR2WBytes);
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}[DewalkNode] Write CR2W ({subCR2WBytes.Length} bytes) to var {cvar.GetFullName()} ({cvar.REDType})");
                    }

                    cvar.SetValue(subCR2WBytes);
                    break;
            }

            if (!cvar.IsSerialized)
            {
                PrintError($"{LogIndent(logLevel)}[DewalkNode] Not serialized! Failed to set value? ({node.type}) -> {extension}{cvar.GetFullName()} ({cvar.REDType})");
            }
            return;
        }

        protected static bool DewalkPtrNode(CVariable cvar, CR2WJsonMap map, Dictionary<string, CR2WExportWrapper> chunkByKey, string extension, int logLevel, CR2WJsonToolOptions options)
        {
            if (options.Verbose)
            {
                Print($"{LogIndent(logLevel)}[DewalkPtrNode] {extension}{cvar.GetFullName()} ({cvar.REDType})");
            }

            if (!map.vars.ContainsKey(m_varReference))
            {
                PrintError($"{LogIndent(logLevel)}[DewalkPtrNode] \"{m_varReference}\" not found in var {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return false;
            }
            var referenceKey = (map.vars[m_varReference] as CR2WJsonScalar)?.value as string;
            if (string.IsNullOrEmpty(referenceKey))
            {
                PrintError($"{LogIndent(logLevel)}[DewalkPtrNode] Invalid \"{m_varReference}\" in var {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return false;
            }
            else if (!chunkByKey.ContainsKey(referenceKey))
            {
                PrintError($"{LogIndent(logLevel)}[DewalkPtrNode] Chunk with key {referenceKey} not found in {extension} for var {cvar.GetFullName()} ({cvar.REDType})");
                return false;
            }
            cvar.SetValue(chunkByKey[referenceKey]);
            return true;
        }

        protected static bool DewalkSoftNode(CVariable cvar, CR2WJsonMap map, Dictionary<string, CR2WExportWrapper> chunkByKey, string extension, int logLevel, CR2WJsonToolOptions options)
        {
            if (options.Verbose)
            {
                Print($"{LogIndent(logLevel)}[DewalkSoftNode] {extension}{cvar.GetFullName()} ({cvar.REDType})");
            }

            if (!map.vars.ContainsKey(m_varClassName))
            {
                PrintError($"{LogIndent(logLevel)}[DewalkSoftNode] \"{m_varClassName}\" not found in var {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return false;
            }
            var className = (map.vars[m_varClassName] as CR2WJsonScalar)?.value as string;
            if (string.IsNullOrEmpty(className))
            {
                PrintError($"{LogIndent(logLevel)}[DewalkSoftNode] Invalid \"{m_varClassName}\" in var {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return false;
            }

            if (!map.vars.ContainsKey(m_varDepotPath))
            {
                PrintError($"{LogIndent(logLevel)}[DewalkSoftNode] \"{m_varDepotPath}\" not found in var {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return false;
            }
            var depotPath = (map.vars[m_varDepotPath] as CR2WJsonScalar)?.value as string;
            if (string.IsNullOrEmpty(className))
            {
                PrintError($"{LogIndent(logLevel)}[DewalkSoftNode] Invalid \"{m_varDepotPath}\" in var {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return false;
            }

            ushort flags = 0;
            if (map.vars.ContainsKey(m_varFlags))
            {
                flags = Convert.ToUInt16((map.vars[m_varFlags] as CR2WJsonScalar)?.value);
            }
            else
            {
                PrintError($"{LogIndent(logLevel)}[DewalkSoftNode] \"{m_varFlags}\" not found in var {extension}{cvar.GetFullName()} ({cvar.REDType}) => Defaulting to 0");
            }

            if (cvar is IHandleAccessor handleAccessor)
            {
                handleAccessor.ChunkHandle = false;
                handleAccessor.Flags = flags;
                handleAccessor.ClassName = className;
                handleAccessor.DepotPath = depotPath;
                cvar.SetIsSerialized();
            }
            else if (cvar is ISoftAccessor softAccessor)
            {
                softAccessor.Flags = flags;
                softAccessor.ClassName = className;
                softAccessor.DepotPath = depotPath;
                cvar.SetIsSerialized();
            }
            else
            {
                PrintError($"{LogIndent(logLevel)}[DewalkSoftNode] Can't cast to handle/soft accessor {extension}{cvar.GetFullName()} ({cvar.REDType})");
                return false;
            }
            return true;
        }

        public static bool ExportJSON(string cr2wPath, string jsonPath, CR2WJsonToolOptions options)
        {
            var cr2w = ReadCR2W(cr2wPath);
            if (cr2w == null)
            {
                return false;
            }
            var jsonRoot = WalkCR2W(cr2w, /*Path.GetExtension(cr2wPath).Remove(0, 1)*/ "", 0, options);

            /* Write JSON */
            var serializeSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore
            };
            if (options.BytesAsIntList)
            {
                serializeSettings.Converters.Add(new JsonByteArrayConverter());
            }
            serializeSettings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                                            .Of(typeof(CR2WJsonObject))
                                            .SetFallbackSubtype(typeof(CR2WJsonMap))
                                            .RegisterSubtypeWithProperty(typeof(CR2WJsonScalar), "_value")
                                            .RegisterSubtypeWithProperty(typeof(CR2WJsonArray), "_elements")
                                            .RegisterSubtypeWithProperty(typeof(CR2WJsonChunkMap), "_parentKey")
                                            .RegisterSubtypeWithProperty(typeof(CR2WJsonData), "_extension")
                                            .Build());

            var jsonText = JsonConvert.SerializeObject(jsonRoot, Newtonsoft.Json.Formatting.Indented, serializeSettings);
            File.WriteAllText(jsonPath, jsonText);
            // Works badly with abtract classes -> string jsonText2 = System.Text.Json.JsonSerializer.Serialize<CR2WJsonData>(jsonRoot);
            return true;
        }

        protected static CR2WJsonData WalkCR2W(CR2WFile cr2w, string extension, int logLevel, CR2WJsonToolOptions options)
        {
            var jsonCR2W = new CR2WJsonData(extension);
            PrintOK($"{LogIndent(logLevel)}[CR2W] {(extension != "" ? extension : "<root>")}");
            logLevel += 1;
            /* CR2W Names - skip, will be generated automatically */

            /* CR2W Imports */
            foreach (var import in cr2w.imports)
            {
                var importMap = new Dictionary<string, object>
                {
                    [m_varClassName] = import.ClassNameStr,
                    [m_varDepotPath] = import.DepotPathStr,
                    [m_varFlags] = import.Flags
                };
                jsonCR2W.imports.Add(importMap);
            }

            /* CR2W Properties */
            jsonCR2W.properties = cr2w.properties;

            /* CR2W Buffers */
            jsonCR2W.buffers = cr2w.buffers;

            /* CR2W Embedded */
            foreach (var embed in cr2w.embedded)
            {
                var embedMap = new Dictionary<string, object>
                {
                    ["className"] = embed.ClassName,
                    ["importPath"] = embed.ImportPath,
                    ["importClass"] = embed.ImportClass,
                    ["handle"] = embed.Handle,
                    ["rawBytes"] = embed.GetRawEmbeddedData()
                };
                jsonCR2W.embedded.Add(embedMap);
            }
            if (options.Verbose)
            {
                Print($"{LogIndent(logLevel)}[WalkCR2W] {cr2w.chunks.Count} Chunks, {jsonCR2W.imports.Count} Imports, {jsonCR2W.properties.Count} Properties, {jsonCR2W.buffers.Count} Buffers, {jsonCR2W.embedded.Count} Embedded");
            }

            /* CR2W Chunks - add dummy objects with index first */
            var keyByChunk = new Dictionary<CR2WExportWrapper, string>();
            foreach (var chunk in cr2w.chunks)
            {
                var jsonChunk = new CR2WJsonChunkMap(chunk.REDType)
                {
                    chunkKey = $"{extension}{chunk.REDName}"  // unique key for references
                };
                keyByChunk[chunk] = jsonChunk.chunkKey;
                if (options.Verbose)
                {
                    Print($"{LogIndent(logLevel)}Init Chunk ({chunk.REDType}): {jsonChunk.chunkKey}");
                }

                jsonCR2W.chunks[jsonChunk.chunkKey] = jsonChunk;
            }

            /* CR2W Chunks - add vars */
            foreach (var chunk in cr2w.chunks)
            {
                var jsonChunk = jsonCR2W.chunks[keyByChunk[chunk]];
                jsonChunk.parentKey = (chunk.ParentChunk != null && keyByChunk.ContainsKey(chunk.ParentChunk)) ? keyByChunk[chunk.ParentChunk] : "";
                var chunkVars = chunk.data.GetEditableVariables();
                PrintOK($"{LogIndent(logLevel)}[WalkCR2W] Chunk {extension}{jsonChunk.chunkKey} => {chunkVars.Count} vars");
                foreach (var cvar in chunkVars)
                {
                    if (!cvar.IsSerialized)
                    {
                        continue;
                    }

                    var newElement = WalkNode(cvar, extension, logLevel + 1, options);
                    if (newElement != null)
                        jsonChunk.vars[cvar.REDName] = newElement;
                }
                jsonChunk.flags = chunk.REDObjectFlags;
                if (chunk.unknownBytes != null && chunk.unknownBytes.Bytes != null && chunk.unknownBytes.Bytes.Length > 0)
                {
                    jsonChunk.unknownBytes = chunk.unknownBytes.Bytes;
                }
            }
            /* CR2W Additional bytes */
            if (cr2w.AdditionalCr2WFileBytes != null && cr2w.AdditionalCr2WFileBytes.Length > 0)
            {
                jsonCR2W.additionalBytes = cr2w.AdditionalCr2WFileBytes;
            }

            return jsonCR2W;
        }

        protected static CR2WJsonObject WalkNode(IEditableVariable node, string extension, int logLevel, CR2WJsonToolOptions options)
        {
            var nodeTypeName = node.GetType().FullName;
            if (options.Verbose)
            {
                Print($"{LogIndent(logLevel)}[WalkNode] {node.REDName}, Type = {nodeTypeName}]");
            }

            switch (node)
            {
                case IREDPrimitive primitive:
                    // try to parse byte array primitives as cr2w
                    byte[] extraCR2WBytes = null;
                    if (!options.IgnoreEmbeddedCR2W && node is IByteSource byteSource)
                    {
                        extraCR2WBytes = byteSource.GetBytes();
                    }

                    if (extraCR2WBytes != null)
                    {
                        var extraCR2W = new CR2WFile();
                        if (extraCR2W.Read(extraCR2WBytes) == EFileReadErrorCodes.NoError)
                        {
                            if (options.Verbose)
                            {
                                Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> CR2W ({extraCR2WBytes.Length} bytes)");
                            }

                            return WalkCR2W(extraCR2W, $"{extension}{node.REDName}::", logLevel + 1, options);
                        }
                    }
                    var primitiveValue = primitive.GetValueObject();
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> PRIMITIVE = {primitiveValue}");
                    }
                    
                    return primitiveValue == null ? null : new CR2WJsonScalar(node.REDType, primitiveValue);
                case IArrayAccessor arrayAccessor:
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> ARRAY");
                    }

                    var array = new CR2WJsonArray(node.REDType);
                    foreach (var cvar in node.GetEditableVariables())
                    {
                        if (!cvar.IsSerialized)
                        {
                            continue;
                        }

                        if (node.REDType.StartsWith("CPaddedBuffer") && cvar.REDName == "padding")
                        {
                            array.bufferPadding = (cvar as CFloat)?.val;
                            continue;
                        }
                        var newElement = WalkNode(cvar, extension, logLevel + 1, options);
                        if (newElement != null)
                            array.elements.Add(newElement);
                    }
                    return array;
                case ISoftAccessor softAccessor:
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> SOFT {softAccessor.ClassName}:{softAccessor.DepotPath}");
                    }

                    var softMap = new CR2WJsonMap(node.REDType);
                    softMap.vars[m_varClassName] = new CR2WJsonScalar("string", softAccessor.ClassName);
                    softMap.vars[m_varDepotPath] = new CR2WJsonScalar("string", softAccessor.DepotPath);
                    softMap.vars[m_varFlags] = new CR2WJsonScalar("uint16", softAccessor.Flags);
                    return softMap;
                case IHandleAccessor handleAccessor:
                    var handleMap = new CR2WJsonMap(node.REDType);
                    handleMap.vars[m_varChunkHandle] = new CR2WJsonScalar("bool", handleAccessor.ChunkHandle);
                    if (handleAccessor.ChunkHandle)
                    {
                        var handleReferenceValue = handleAccessor.Reference == null ? "NULL" : $"{extension}{handleAccessor.Reference.REDName}";
                        if (options.Verbose)
                        {
                            Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> PTR HANDLE {handleReferenceValue}");
                        }

                        handleMap.vars[m_varReference] = new CR2WJsonScalar("string", handleReferenceValue);
                    }
                    else
                    {
                        if (options.Verbose)
                        {
                            Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> SOFT HANDLE {handleAccessor.ClassName}:{handleAccessor.DepotPath}");
                        }

                        handleMap.vars[m_varClassName] = new CR2WJsonScalar("string", handleAccessor.ClassName);
                        handleMap.vars[m_varDepotPath] = new CR2WJsonScalar("string", handleAccessor.DepotPath);
                        handleMap.vars[m_varFlags] = new CR2WJsonScalar("uint16", handleAccessor.Flags);
                    }
                    return handleMap;
                case IPtrAccessor ptrAccessor:
                    var ptrMap = new CR2WJsonMap(node.REDType);
                    var ptrReferenceValue = ptrAccessor.Reference == null ? "NULL" : $"{extension}{ptrAccessor.Reference.REDName}";
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> PTR {ptrReferenceValue}");
                    }

                    ptrMap.vars[m_varReference] = new CR2WJsonScalar("string", ptrReferenceValue);
                    return ptrMap;
                case IVariantAccessor variantAccessor:
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> VARIANT {variantAccessor.Variant.REDName} ({variantAccessor.Variant.REDType})");
                    }

                    var variantMap = new CR2WJsonMap(node.REDType);
                    variantMap.vars[m_varVariant] = WalkNode(variantAccessor.Variant, extension, logLevel + 1, options);
                    variantMap.vars[m_varName] = new CR2WJsonScalar("string", node.REDName);
                    return variantMap;
                case IEnumAccessor enumAccessor:
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> ENUM = {string.Join("|", enumAccessor.Value)}");
                    }

                    return new CR2WJsonScalar(node.REDType, string.Join("|", enumAccessor.Value));
                default:
                    if (options.Verbose)
                    {
                        Print($"{LogIndent(logLevel)}{node.REDName} ({node.REDType}) -> default MAP");
					}

                    var REDMapType = node.REDType;
                    // hack for SBlockDataPackedObject..
                    if (node.REDName == "SBlockDataPackedObject" && node.ParentVar is SBlockData packedObj)
                    {
                        REDMapType = $"{packedObj.packedObjectType.ToString()}:{REDMapType}";
                    }
                    var map = new CR2WJsonMap(REDMapType);
                    foreach (var cvar in node.GetEditableVariables())
                    {
                        if (!cvar.IsSerialized)
                        {
                            continue;
                        }

                        var newElement = WalkNode(cvar, extension, logLevel + 1, options);
                        if (newElement != null)
                            map.vars[cvar.REDName] = newElement;
                    }
                    return map;
            }
        }
    }
}
