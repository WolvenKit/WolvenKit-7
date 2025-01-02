using RED.CRC32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using WolvenKit.Common;
using WolvenKit.Common.FNV1A;
using WolvenKit.Common.Model;
using WolvenKit.Common.Services;
using WolvenKit.CR2W.Types;
using WolvenKit.CR2W.Types.Utils;

namespace WolvenKit.CR2W
{
    [ DataContract(Namespace = "") ]
    public partial class CR2WFile : ObservableObject, IWolvenkitFile
    {
        #region Constants
        private const long MAGIC_SIZE = 4;
        private const long FILEHEADER_SIZE = 36;
        private const long TABLEHEADER_SIZE = 12 * 10;
        private readonly int[] TABLES_SIZES = new int[6] { 8, 8, 16, 24, 24, 24 };


        #endregion

        #region Constructor
        public CR2WFile(LoggerService logger=null)
        {
            names = new List<CR2WNameWrapper>();            //block 2
            imports = new List<CR2WImportWrapper>();        //block 3
            properties = new List<CR2WPropertyWrapper>();   //block 4
            chunks = new List<CR2WExportWrapper>();         //block 5
            buffers = new List<CR2WBufferWrapper>();        //block 6
            embedded = new List<CR2WEmbeddedWrapper>();     //block 7

            m_fileheader = new CR2WFileHeader(){
                version = 162,
            };

            Logger = logger ?? new LoggerService();

            StringDictionary = new Dictionary<uint, string>();
            m_tableheaders = new CR2WTable[10];
        }

        
        #endregion

        #region Fields
        
        // constants
        private const uint MAGIC = 0x57325243; // "W2RC"
        private const uint DEADBEEF = 0xDEADBEEF;

        // IO
        [DataMember(Order = 1, Name = "Header")]
        private CR2WFileHeader m_fileheader;

        private CR2WTable[] m_tableheaders;
        private Byte[] m_strings;
        public Dictionary<uint, string> StringDictionary { get; private set; }

        // misc
        private uint headerOffset = 0;
        public bool m_hasInternalBuffer;
        //private Stream m_stream; //handle this better?
        // private string m_filePath;

        public CR2WFile AdditionalCr2WFile;
        public byte[] AdditionalCr2WFileBytes;
        #endregion

        #region Properties
        public LoggerService Logger { get; }

        // Tables
        public List<CR2WNameWrapper> names { get; private set; }
        public List<CR2WImportWrapper> imports { get; set; }
        public List<CR2WPropertyWrapper> properties { get; set; }

        //[DataMember(Order = 2)]
        public List<CR2WExportWrapper> chunks { get; set; }
        public List<CR2WBufferWrapper> buffers { get; set; }
        public List<CR2WEmbeddedWrapper> embedded { get; set; }

        public void GenerateChunksDict() => chunksdict = chunks.ToDictionary(_ => _.ChunkIndex, _ => _);
        public Dictionary<int, CR2WExportWrapper> chunksdict { get; private set; }

        public List<LocalizedString> LocalizedStrings = new List<LocalizedString>();
        public List<string> UnknownTypes = new List<string>();
        //public byte[] bufferdata { get; set; }
        [DataMember(Order = 0)]
        public string FileName { get; set; }

       
        /// <summary>
        ///     LocalizedStringSource
        /// </summary>
        public ILocalizedStringSource LocalizedStringSource { get; set; }

        /// <summary>
        ///     EditorController
        /// </summary>
        public IVariableEditor EditorController { get; set; }

        #endregion

        #region Supporting Functions
		/// <summary>
        /// Creates dictionary of chunks by it's REDNames.
        /// </summary>
        /// <param name="name_prefix">Includes only chunks with the same prefix in name (useful to filter by type). Leave empty to include all chunks.</param>
        /// <returns>Dictionary[REDName] = CR2WExportWrapper</returns>
        public Dictionary<string, CR2WExportWrapper> GetChunksMapByPrefix(string name_prefix)
        {
            Dictionary<string, CR2WExportWrapper> ret = new Dictionary<string, CR2WExportWrapper>();
            foreach (var chunk in chunks)
            {
                if (string.IsNullOrEmpty(name_prefix) || chunk.REDName.StartsWith(name_prefix)) {
                    ret[chunk.REDName] = chunk;
                }
            }
            return ret;
        }
        // Does not reindex /TODO
        public CR2WExportWrapper CreateChunk(string type, int chunkindex=0, CR2WExportWrapper parent = null, CR2WExportWrapper virtualparent = null, CVariable cvar = null)
        {
            var chunk = new CR2WExportWrapper(this, type, parent);
            chunk.CreateDefaultData();
            chunk.data.VarChunkIndex = chunkindex;

            if (parent!=null)
            {
                chunk.ParentChunk = parent;
            }
            if (virtualparent != null)
            {
                chunk.MountChunkVirtually(virtualparent);
            }

            chunks.Insert(chunkindex, chunk);
            return chunk;
        }

        // Does not reindex /TODO
        public CR2WExportWrapper CreateChunk(CVariable cvar, int chunkindex=0, CR2WExportWrapper parent = null, CR2WExportWrapper virtualparent = null)
        {
            // checks to see if the variable from which the chunk is built is properly constructed
            if (cvar == null || cvar.REDName != cvar.REDType || cvar.ParentVar != null)
                throw new NotImplementedException();

            var chunk = new CR2WExportWrapper(this, cvar.REDType, parent);
            chunk.CreateDefaultData(cvar);
            chunk.data.VarChunkIndex = chunkindex;

            if (parent != null)
            {
                chunk.ParentChunk = parent;
            }
            if (virtualparent != null)
            {
                chunk.MountChunkVirtually(virtualparent);
            }

            chunks.Insert(chunkindex, chunk);
            return chunk;
        }

        public enum EChunkDisplayMode
        {
            Linear,
            Parent,
            VirtualParent
        }

        /// <summary>
        /// Deletes the chunk and/or its children chunks.
        /// Reindexes the hierarchy.
        /// </summary>
        /// <param name="toberemovedchunks">The chunks dealt with.</param>
        /// <param name="onlychildren">The method can remove the chunk and descendance, or only the descendance.</param>
        /// <param name="recursionmode">The method can remove only the chunk (Linear), the real children (Parent), the virtual children (VirtualParent).</param>
        /// <param name="purgereferrers">Try to not leave null pointers</param>
        /// <param name="reentrant">Called for children chunks</param>
        /// <param name="passedoldparentinghierarchy">Passed for children chunks</param>
        /// <returns>Number of chunks removed</returns>
        public int RemoveChunks(
            List<CR2WExportWrapper> toberemovedchunks,
            bool onlychildren = false,
            EChunkDisplayMode recursionmode = EChunkDisplayMode.VirtualParent,
            bool purgereferrers = false,
            bool reentrant = false,
            Dictionary<CR2WExportWrapper, (CR2WExportWrapper oldchunkparent, CR2WExportWrapper oldchunkvparent)> passedoldparentinghierarchy= null)
        {
            int removed = onlychildren ? 0 : 1;

            // To reindex later, we need a middle-ground copy, between deep and shallow, of the parenting hierarchy.
            var oldparentinghierarchy = new Dictionary<CR2WExportWrapper, (CR2WExportWrapper oldchunkparent, CR2WExportWrapper oldchunkvparent)>();
            if (!reentrant)
            {
                foreach (var achunk in chunks)
                {
                    oldparentinghierarchy.Add(achunk, (achunk.ParentChunk, achunk.VirtualParentChunk));
                }
            }

            foreach(var chunk in toberemovedchunks)
            {
                // Nullify references toward this chunk
                if ((!onlychildren && !reentrant) || reentrant)
                {
                    foreach (var referrer in chunk.AdReferences)
                    {
                        if (purgereferrers && referrer.ParentVar is IArrayAccessor)
                        {
                            referrer.ParentVar.RemoveVariable(referrer as IEditableVariable);
                        }
                        else
                        {
                            // This leaves a dangling cptr/chandle in AdReferences,
                            // which needs to be adressed later.
                            referrer.Reference = null;
                        }
                    }
                }

                //Recurse
                int i = 0;
                while (recursionmode != EChunkDisplayMode.Linear)
                {
                    if (i >= chunks.Count())
                        break;
                    var achunk = chunks[i++];

                    // Remove children chunks
                    if (recursionmode == EChunkDisplayMode.Parent)
                    {
                        if (!reentrant)
                        {
                            if (oldparentinghierarchy[achunk].oldchunkparent == chunk)
                            {
                                removed += RemoveChunks(
                                    new List<CR2WExportWrapper>() { achunk },
                                    false,
                                    recursionmode,
                                    true,
                                    purgereferrers,
                                    oldparentinghierarchy);
                                i = 0;
                            }
                        }
                        else
                        {
                            if (passedoldparentinghierarchy[achunk].oldchunkparent == chunk)
                            {
                                removed += RemoveChunks(
                                    new List<CR2WExportWrapper>() { achunk },
                                    false,
                                    recursionmode,
                                    true,
                                    purgereferrers,
                                    passedoldparentinghierarchy);
                                i = 0;
                            }
                        }
                    }
                    else if (recursionmode == EChunkDisplayMode.VirtualParent)
                    {
                        if (!reentrant)
                        {
                            if (oldparentinghierarchy[achunk].oldchunkvparent == chunk)
                            {
                                removed += RemoveChunks(
                                    new List<CR2WExportWrapper>() { achunk },
                                    false,
                                    recursionmode,
                                    true,
                                    purgereferrers,
                                    oldparentinghierarchy);
                                i = 0;
                            }
                        }
                        else
                        {
                            if (passedoldparentinghierarchy[achunk].oldchunkvparent == chunk)
                            {
                                removed += RemoveChunks(
                                    new List<CR2WExportWrapper>() { achunk },
                                    false,
                                    recursionmode,
                                    true,
                                    purgereferrers,
                                    passedoldparentinghierarchy);
                                i = 0;
                            }
                        }
                    }
                }

                // Actually remove the chunk
                if ((!reentrant && !onlychildren) || reentrant)
                {
                    chunks.Remove(chunk);
                }
            }



            // Reindex the tree from the root function call
            if (!reentrant && removed > 0)
            {
                foreach (var achunk in chunks)
                {
                    achunk.ParentChunk = oldparentinghierarchy[achunk].oldchunkparent;
                }

                //Update UI
                OnPropertyChanged(nameof(chunks));
            }

            return removed;
        }

        public int GetLastChildrenIndexRecursive(CR2WExportWrapper chunk)
        {
            if (chunk.VirtualChildrenChunks.Count() == 0)
            {
                return chunk.ChunkIndex;
            }
            else
            {
                return GetLastChildrenIndexRecursive(chunk.VirtualChildrenChunks.Where(
                    _ => _.ChunkIndex == chunk.VirtualChildrenChunks.Max(p => p.ChunkIndex)).FirstOrDefault());
            }
        }

        public int GetStringIndex(string name, bool addnew = false)
        {
            for (var i = 0; i < names.Count; i++)
            {
                if (names[i].Str == name || (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(names[i].Str)))
                    return i;
            }

            throw new NotImplementedException();
            //return -1;
        }

        public string GetLocalizedString(uint val)
        {
            if (LocalizedStringSource != null)
                return LocalizedStringSource.GetLocalizedString(val);

            return null;
        }

        public CR2WFileHeader GetFileHeader() => m_fileheader;
        public CR2WTable[] GetTableHeaders() => m_tableheaders;

        public CVariable ReadVariable(BinaryReader file, CVariable parent)
        {

            // Read Name
            var nameId = file.ReadUInt16();
            if (nameId == 0)
            {
                return null;
            }
            var varname = names[nameId].Str;

            // Read Type
            var typeId = file.ReadUInt16();
            var typename = names[typeId].Str;

            // Read Size
            var sizepos = file.BaseStream.Position;
            var size = file.ReadUInt32();

            // Read Value

            // CDPR changed the type of CPtr<IBehTreeNodeDefinition> RootNode
            // to CHandle<IBehTreeNodeDefinition> RootNode in CBehTree in patch1
            if (typename == "ptr:IBehTreeNodeDefinition" && varname == "rootNode" && parent.REDType == "CBehTree")
                typename = "handle:IBehTreeNodeDefinition";

            var parsedvar = CR2WTypeManager.Create(typename, varname, this, parent);
            // The "size" variable read is something a bit strange : it takes itself into account.
            //try
            //{
                parsedvar.Read(file, size - 4);
            //}
            //catch
            //{
            //    System.Console.WriteLine("hohoho");
            //}

            var afterVarPos = file.BaseStream.Position;

            var bytesleft = size - (afterVarPos - sizepos);
            if (bytesleft > 0)
            {
                var unreadBytes = file.ReadBytes((int)bytesleft);
                //?? why not throw ?? throw new InvalidParsingException($"Parsing Variable read too short. Difference: {bytesleft}");
            }
            else if (bytesleft < 0)
            {
                throw new InvalidParsingException($"Parsing Variable read too far. Difference: {bytesleft}");
            }

            return parsedvar;
        }

        public static void WriteVariable(BinaryWriter file, IEditableVariable ivar)
        {
            if (ivar is CVariable cvar)
            {
                file.Write(cvar.GetnameId());
                file.Write(cvar.GettypeId());

                var pos = file.BaseStream.Position;
                file.Write((uint)0); // size placeholder


                cvar.Write(file);
                var endpos = file.BaseStream.Position;

                file.Seek((int)pos, SeekOrigin.Begin);
                var actualsize = (uint)(endpos - pos);
                file.Write(actualsize); // Write size
                file.Seek((int)endpos, SeekOrigin.Begin);
            }
            else
                throw new SerializationException();
        }
        #endregion

        #region Read
        public (List<CR2WImportWrapper>, bool, List<CR2WBufferWrapper>) ReadImportsAndBuffers(BinaryReader file)
        {
            #region Read Headers
            // read file header
            var id = file.BaseStream.ReadStruct<uint>();
            if (id != MAGIC)
                throw new FormatException($"Not a CR2W file, Magic read as 0x{id:X8}");

            m_fileheader = file.BaseStream.ReadStruct<CR2WFileHeader>();
            if (m_fileheader.version > 163 || m_fileheader.version < 159)
                throw new FormatException($"Unknown Version {m_fileheader.version}. Supported versions: 159 - 163.");

            var dt = new CDateTime(m_fileheader.timeStamp, null, "");

            m_tableheaders = file.BaseStream.ReadStructs<CR2WTable>(10);
            m_hasInternalBuffer = m_fileheader.bufferSize > m_fileheader.fileSize;

            // read strings
            m_strings = ReadStringsBuffer(file.BaseStream);

            // read tables
            names = ReadTable<CR2WName>(file.BaseStream, 1).Select(_ => new CR2WNameWrapper(_, this)).ToList();
            imports = ReadTable<CR2WImport>(file.BaseStream, 2).Select(_ => new CR2WImportWrapper(_, this)).ToList();
            properties = ReadTable<CR2WProperty>(file.BaseStream, 3).Select(_ => new CR2WPropertyWrapper(_)).ToList();
            chunks = ReadTable<CR2WExport>(file.BaseStream, 4).Select(_ => new CR2WExportWrapper(_, this)).ToList();
            buffers = ReadTable<CR2WBuffer>(file.BaseStream, 5).Select(_ => new CR2WBufferWrapper(_)).ToList();
            embedded = ReadTable<CR2WEmbedded>(file.BaseStream, 6).Select(_ => new CR2WEmbeddedWrapper(_)
            {
                ParentFile = this,
                ParentImports = imports,
                Handle = StringDictionary[_.path],
            }).ToList();

            #endregion

            return (imports, m_hasInternalBuffer, buffers);
        }

        public EFileReadErrorCodes Read(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                return Read(br);
            }
        }

        public EFileReadErrorCodes Read(BinaryReader file)
        {
            //m_stream = file.BaseStream;

            Stopwatch stopwatch1 = new Stopwatch();
            stopwatch1.Start();

            #region Read Headers

            var startpos = file.BaseStream.Position;
            Logger?.LogProgress(1, "Reading headers...");

            // read file header
            var id = file.BaseStream.ReadStruct<uint>();
            if (id != MAGIC)
                return EFileReadErrorCodes.NoCr2w;

            m_fileheader = file.BaseStream.ReadStruct<CR2WFileHeader>();
            if (m_fileheader.version > 163 || m_fileheader.version < 158)
                return EFileReadErrorCodes.UnsupportedVersion;

            var dt = new CDateTime(m_fileheader.timeStamp, null, "");

            // Tables [7-9] are not used in cr2w so far.
            m_tableheaders = file.BaseStream.ReadStructs<CR2WTable>(10);
            m_hasInternalBuffer = m_fileheader.bufferSize > m_fileheader.fileSize;

            // read strings - block 1 (index 0)
            m_strings = ReadStringsBuffer(file.BaseStream);
            
            // read the other tables
            names = ReadTable<CR2WName>(file.BaseStream, 1).Select(_ => new CR2WNameWrapper(_, this)).ToList(); // block 2
            imports = ReadTable<CR2WImport>(file.BaseStream, 2).Select(_ => new CR2WImportWrapper(_, this)).ToList(); // block 3
            properties = ReadTable<CR2WProperty>(file.BaseStream, 3).Select(_ => new CR2WPropertyWrapper(_)).ToList(); // block 4
            chunks = ReadTable<CR2WExport>(file.BaseStream, 4).Select(_ => new CR2WExportWrapper(_, this)).ToList(); // block 5
            buffers = ReadTable<CR2WBuffer>(file.BaseStream, 5).Select(_ => new CR2WBufferWrapper(_)).ToList(); // block 6
            embedded = ReadTable<CR2WEmbedded>(file.BaseStream, 6).Select(_ => new CR2WEmbeddedWrapper(_)
            {
                ParentFile = this,
                ParentImports = imports,
                Handle = StringDictionary[_.path],
            }).ToList(); // block 7

            Logger?.LogProgress(100);
            #endregion

            #region Read Data

            Logger?.LogProgress(1, "Reading chunks...");
            // Read object data //block 5
            for (int i = 0; i < chunks.Count; i++)
            {
                CR2WExportWrapper chunk = chunks[i];

                chunk.ReadData(file);

                int percentprogress = (int)((float)i / (float)chunks.Count * 100.0);
                Logger?.LogProgress(percentprogress, $"Reading chunk {chunk.REDName}...");
            }
            // Read buffer data //block 6
            if (m_hasInternalBuffer)
            {
                for (int i = 0; i < buffers.Count; i++)
                {
                    CR2WBufferWrapper buffer = buffers[i];
                    buffer.ReadData(file);

                    int percentprogress = (int)((float)i / (float)buffers.Count * 100.0);
                    Logger?.LogProgress(percentprogress);
                    Logger?.LogString($"Reading buffer data[{i}]: {buffer.Data.Length} bytes");
                }
            }
            // Read embedded files //block 7
            for (int i = 0; i < embedded.Count; i++)
            {
                CR2WEmbeddedWrapper emb = embedded[i];
                emb.ReadData(file);

                int percentprogress = (int)((float)i / (float)embedded.Count * 100.0);
                Logger?.LogProgress(percentprogress, $"Reading embedded file {emb.ClassName}...");
                Logger?.LogString($"Reading embedded file[{i}]: {emb.ClassName}");
            }
            #endregion



            // some w2ls have additional CLayerInfo packed behind the file
            var endpos = file.BaseStream.Position;
            var readbytes = endpos - startpos;
            if (readbytes != file.BaseStream.Length)
            {
                var bytesleft = file.BaseStream.Length - readbytes;
                AdditionalCr2WFileBytes = file.ReadBytes((int) bytesleft);


            }


            Logger?.LogString($"File {FileName} loaded in: {stopwatch1.Elapsed}\n");

            stopwatch1.Stop();
            //m_stream = null;
            return 0;
        }

        public CR2WFile GetAdditionalCr2wFile()
        {
            if (AdditionalCr2WFileBytes == null) return null;
            using (var ms2 = new MemoryStream(AdditionalCr2WFileBytes))
            using (var br2 = new BinaryReader(ms2))
            {
                AdditionalCr2WFile = new CR2WFile();
                AdditionalCr2WFile.Read(br2);
                return AdditionalCr2WFile;
            }
        }

        private T[] ReadTable<T>(Stream stream, int index) where T : struct
        {
            //stream.Seek(m_tableheaders[index].offset, SeekOrigin.Begin);

            var hash = new Crc32Algorithm(false);
            var table = stream.ReadStructs<T>(m_tableheaders[index].itemCount, hash);

            return table;
        }
        private byte[] ReadStringsBuffer(Stream stream)
        {
            var start = m_tableheaders[0].offset;
            var m_strings_size = m_tableheaders[0].itemCount;
            var crc = m_tableheaders[0].crc32;

            var m_temp = new byte[m_strings_size];
            stream.Read(m_temp, 0, m_temp.Length);

            
            uint offset = 0;
            var tempstring = new List<byte>();
            for (uint i = 0; i < m_strings_size; i++)
            {
                byte b = m_temp[i];
                if (b == 0)
                {
                    var text = Encoding.GetEncoding("iso-8859-1").GetString(tempstring.ToArray());
                    StringDictionary.Add(offset, text);
                    tempstring.Clear();
                    offset = i + 1;
                }
                else
                {
                    tempstring.Add(b);
                }
            }

            return m_temp;
        }
        #endregion

        #region Write
        public void Write(BinaryWriter file)
        {
            //m_stream = file.BaseStream;

            // update data
            //m_fileheader.timeStamp = CDateTime.Now.ToUInt64();    //this will change any vanilla assets simply by opening and saving in wkit
            //m_fileheader.numChunks = (uint)chunks.Count;          //this is weird, I don't think it actually is the number of chunks

            #region Update Tables

            #region Stringtable
            StringDictionary.Clear();


            List<byte> newstrings;
            List<string> nameslist;
            List<SImportEntry> importslist;
            (StringDictionary, newstrings, nameslist, importslist) = GenerateStringtable();

            uint stringbuffer_offset = 160; // always 160
            m_tableheaders[0].offset = stringbuffer_offset;
            m_strings = newstrings.ToArray();

            m_tableheaders[0].itemCount = (uint)m_strings.Length;
            m_tableheaders[0].crc32 = Crc32Algorithm.Compute(m_strings);

            

            #endregion

            var inverseDictionary = StringDictionary.ToDictionary(x => x.Value, x => x.Key);

            #region Names
            names.Clear();
            foreach (var name in nameslist)
            {
                //var encodedstring = Encoding.GetEncoding("iso-8859-1")
                var newoffset = inverseDictionary[name];
                var hash = FNV1A32HashAlgorithm.HashString(name, Encoding.GetEncoding("iso-8859-1"), true);
                names.Add(new CR2WNameWrapper(new CR2WName()
                {
                    //me: Why hash and not name??
                    hash = hash,
                    value = newoffset
                }, this));
            }
            #endregion
            #region Imports
            imports.Clear();
            foreach (var import in importslist)
            {
                var nw = names.First(_ => _.Str == import.Item1);
                ushort flag = (ushort)import.Item3;

                imports.Add(new CR2WImportWrapper(
                    new CR2WImport()
                    {
                        className = (ushort)names.IndexOf(nw),
                        depotPath = inverseDictionary[import.Item2],
                        flags = (ushort)import.Item3    //TODO finish all flags
                    }, this));
            }
            #endregion
            #region Embedded 
            for (var i = 0; i < embedded.Count; i++)
            {
                var emb = embedded[i];
                // update path index
                var handleoffset = inverseDictionary[emb.Handle];
                if (handleoffset != emb.Embedded.path)
                {
                    emb.SetPath(handleoffset);
                    // check path hash
                    var pathhash = FNV1A64HashAlgorithm.HashString(emb.Handle);
                    if (pathhash != emb.Embedded.pathHash)
                        emb.SetPathHash(pathhash);
                }
                // uddate import index
                int realidx = (int)emb.Embedded.importIndex - 1;
                if (imports[realidx].ClassNameStr != emb.ImportClass || emb.ImportPath != imports[realidx].DepotPathStr)
                {
                    var importindex = imports.FindIndex(_ => _.ClassNameStr == emb.ImportClass && _.DepotPathStr == emb.ImportPath);
                    if (importindex != -1)
                        emb.SetImportIndex((uint)importindex);
                }
            }
            #endregion
            #endregion


            headerOffset = 0;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // first write the file to memory
                // this also sets m_fileheader.fileSize and m_fileheader.bufferSize, offsets
                WriteData(bw);

                // Write headers once to allocate the space for it
                WriteHeader(file);

                headerOffset = (uint)file.BaseStream.Position;

                // Write buffers
                ms.Seek(0, SeekOrigin.Begin);
                ms.WriteTo(file.BaseStream);
            }

            #region Update Offsets
            for (var i = 0; i < chunks.Count; i++)
            {
                var newoffset = chunks[i].Export.dataOffset + headerOffset;
                chunks[i].SetOffset(newoffset);
                chunks[i].SetType( (ushort)GetStringIndex(chunks[i].REDType));
            }
            if (m_hasInternalBuffer)
            {
                for (var i = 0; i < buffers.Count; i++)
                {
                    var newoffset = buffers[i].Buffer.offset + headerOffset;
                    buffers[i].SetOffset(newoffset);
                }
            }
            for (var i = 0; i < embedded.Count; i++)
            {
                var newoffset = embedded[i].Embedded.dataOffset + headerOffset;
                embedded[i].SetOffset(newoffset);
            }
            m_fileheader.fileSize += headerOffset;
            m_fileheader.bufferSize += headerOffset;
            #endregion

            foreach (var chunk in chunks)
            {
                FixExportCRC32(chunk.Export);
            }

            foreach (var buffer in buffers)
            {
                FixBufferCRC32(buffer.Buffer);
            }

            // Write headers again with fixed offsets
            //m_fileheader.crc32 = CalculateHeaderCRC32();
            WriteHeader(file);
            m_fileheader.crc32 = CalculateHeaderCRC32();
            WriteFileHeader(file);




            if (AdditionalCr2WFileBytes != null)
            {
                file.BaseStream.Seek(0, SeekOrigin.End);
                file.Write(AdditionalCr2WFileBytes);
            }









            //m_stream = null;

            void FixExportCRC32(CR2WExport export) //FIXME do I wanna keep the ref?
            {
                file.BaseStream.Seek(export.dataOffset, SeekOrigin.Begin);
                var m_temp = new byte[export.dataSize];
                file.BaseStream.Read(m_temp, 0, m_temp.Length);
                export.crc32 = Crc32Algorithm.Compute(m_temp);
            }

            void FixBufferCRC32(CR2WBuffer buffer) //FIXME do I wanna keep the ref?
            {
                //This might throw errors, the way it should be checked for is by reading
                //the object tree to find the deferred data buffers that will point to a buffer.
                //The flag of the parent object indicates where to read the data from.
                //For now this is a crude workaround.
                if (m_hasInternalBuffer)
                {
                    file.BaseStream.Seek(buffer.offset, SeekOrigin.Begin);
                    var m_temp = new byte[buffer.diskSize];
                    file.BaseStream.Read(m_temp, 0, m_temp.Length);
                    buffer.crc32 = Crc32Algorithm.Compute(m_temp);
                }
                else
                {
                    /*var path = String.Format("{0}.{1}.buffer", m_filePath, buffer.index);
                    if (!File.Exists(path))
                    {
                        return;
                    }
                    m_temp = File.ReadAllBytes(path);
                    buffer.crc32 = Crc32Algorithm.Compute(m_temp);*/
                }
            }

            uint CalculateHeaderCRC32()
            {
                var hash = new Crc32Algorithm(false);
                hash.Append(BitConverter.GetBytes(MAGIC));
                hash.Append(BitConverter.GetBytes(m_fileheader.version));
                hash.Append(BitConverter.GetBytes(m_fileheader.flags));
                hash.Append(BitConverter.GetBytes(m_fileheader.timeStamp));
                hash.Append(BitConverter.GetBytes(m_fileheader.buildVersion));
                hash.Append(BitConverter.GetBytes(m_fileheader.fileSize));
                hash.Append(BitConverter.GetBytes(m_fileheader.bufferSize));
                hash.Append(BitConverter.GetBytes(DEADBEEF));
                hash.Append(BitConverter.GetBytes(m_fileheader.numChunks));
                foreach (var h in m_tableheaders)
                {
                    hash.Append(BitConverter.GetBytes(h.offset));
                    hash.Append(BitConverter.GetBytes(h.itemCount));
                    hash.Append(BitConverter.GetBytes(h.crc32));
                }
                return hash.HashUInt32;
            }
        }

        public void Write(CByteArray ba)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                Write(bw);
                ba.Bytes = ms.ToArray();
            }
        }

        public void Write(ref byte[] ba)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                Write(bw);
                ba = ms.ToArray();
            }
        }

        /// <summary>
        /// How a REDEngine entity is to be serialized
        /// </summary>
        public enum EStringTableMod
        {
            None,
            SkipType,
            SkipName,
            SkipNameAndType,
            TypeFirst
        }

        // Those where before tuples, passed between functions. Got sick of them and made structs.
        // Got lazy and did not rewrite elements in code, hence ItemN attributes. //FIXME unimportant
        public struct SNameArg
        {
            public EStringTableMod Item1;
            public IEditableVariable Item2;

            public SNameArg(EStringTableMod i1, IEditableVariable i2)
            {
                Item1 = i1;
                Item2 = i2;
            }
        };

        // Those where before tuples, passed between functions. Got sick of them and made structs.
        // Got lazy and did not rewrite elements in code, hence ItemN attributes. //FIXME unimportant
        public struct SImportEntry
        {
            public readonly string Item1;
            public readonly string Item2;
            public readonly EImportFlags Item3;

            public SImportEntry(string i1, string i2, EImportFlags i3)
            {
                Item1 = i1;
                Item2 = i2;
                Item3 = i3;
            }
        };

        public (Dictionary<uint, string>, List<byte>, List<string>, List<SImportEntry>) GenerateStringtable()
        {
            Dictionary<uint, string> newstringtable = new Dictionary<uint, string>();

            // Get lists
            (var nameslist, List<SImportEntry> importslist) = GenerateStringtableInner();
            importslist.RemoveAll(item => (item.Item1 == null || item.Item2 == null));
            var stringlist = new HashSet<string>(nameslist);

            foreach (var import in importslist)
            {
                nameslist.Add(import.Item1);
                stringlist.Add(import.Item1);
                stringlist.Add(import.Item2);
            }
            foreach (var emb in embedded)
            {
                if (emb.Handle != null)
                    stringlist.Add(emb.Handle);
            }

            // create new stringslist
            var newstrings = new List<byte>();
            foreach (var str in stringlist)
            {
                if (str != null)
                {
                    var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(str);
                    foreach (var b in bytes)
                    {
                        newstrings.Add(b);
                    }
                }
                newstrings.Add((byte)0);

            }

            // build new stringsDixtionary
            var strings = newstrings.ToArray();
            var stringscount = strings.Length;

            StringBuilder sb = new StringBuilder();
            var tempstring = new List<byte>();
            uint offset = 0;
            for (uint i = 0; i < stringscount; i++)
            {
                byte b = strings[i];
                if (b == 0)
                {
                    var text = Encoding.GetEncoding("iso-8859-1").GetString(tempstring.ToArray());
                    newstringtable.Add(offset, text);

                    sb.Clear();
                    tempstring.Clear();
                    offset = i + 1;
                }
                else
                {
                    sb.Append((char)b);
                    tempstring.Add(b);
                }
            }

            return (newstringtable, newstrings, nameslist.ToList<string>(), importslist);
        }

        private (HashSet<string>, List<SImportEntry>) GenerateStringtableInner()
        {
            var dbg_trace = new List<string>();
            var newnameslist = new Dictionary<string, string>();
            newnameslist.Add("", "");
            var newimportslist = new List<SImportEntry>();
            var newsoftlist = new List<SImportEntry>();
            var guidlist = new HashSet<Guid>();
            var chunkguidlist = new List<Guid>();

            // CDPR changed the type of CPtr<IBehTreeNodeDefinition> RootNode
            // to CHandle<IBehTreeNodeDefinition> RootNode in CBehTree in patch1
            bool skipnamecheck = chunks?.Count > 0 && chunks.First().data is CBehTree;

            foreach (var c in chunks)
            {
                chunkguidlist.Add(c.data.InternalGuid);
                LoopWrapper(new SNameArg(EStringTableMod.SkipName, c.data));
            }
            
            newimportslist.AddRange(newsoftlist);

            return (newnameslist.Values.ToHashSet<string>(), newimportslist);

            void LoopWrapper(SNameArg var)
            {
                if (guidlist.Contains(var.Item2.InternalGuid))
                {
                    return;
                }

                //collection.Add(var);
                dbg_trace.Add($"{var.Item2.REDName}[{var.Item2.REDType}] - {var.Item1}");
                AddStrings(var);

                if (!skipnamecheck)
                {
                    //if (newnameslist.Last().Value != StringDictionary.Values.ToList()[newnameslist.Count - 1])
                    //{


                    //}
                }

                List<SNameArg> nextl = GetVariables(var.Item2);
                if (nextl == null)
                    return;
                foreach (var l in nextl)
                {
                    if (l.Item2 != null)
                        LoopWrapper(l);
                }
            }



        //struct SNameArg zobi ;
        List<SNameArg> GetVariables(IEditableVariable ivar)
        {
            //check for looping references
            if (guidlist.Contains(ivar.InternalGuid))
                return null;
            else
                guidlist.Add(ivar.InternalGuid);

            var returnedVariables = new List<SNameArg>();

            // if variable is generic type or some special case 
            switch(ivar)
            {
                case IArrayAccessor a:
                    switch(a)
                    {
                        case CArray<CName> cacn:
                            returnedVariables.Add(new SNameArg(EStringTableMod.None, a)); ///???
                            break;
                        case CArray<CBool> cacb:
                        case CArray<CUInt16> cacu16:
                        case CArray<CInt16> caci16:
                        case CArray<CUInt32> cacu32:
                        case CArray<CInt32> caci32:
                        case CArray<CUInt64> cacu64:
                        case CArray<CInt64> caci64:
                            break;
                        default:
                            var elements = a.GetEditableVariables();
                            foreach (var item in elements)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, item));
                            break;
                    }
                    break;
                case IPtrAccessor p:
                    if (p.Reference != null)
                        returnedVariables.Add(new SNameArg(EStringTableMod.None, p.Reference.data));
                    break;
                case IHandleAccessor h:
                    if (h.ChunkHandle)
                        if (h.Reference != null)
                            returnedVariables.Add(new SNameArg(EStringTableMod.None, h.Reference.data));
                    break;
                case ISoftAccessor s:
                    break;
                case IBufferVariantAccessor ivariant:
                    EStringTableMod mod = EStringTableMod.None;
                    if (ivariant is CVariantSizeType)
                        mod = EStringTableMod.SkipName;
                    returnedVariables.Add(new SNameArg(mod, ivariant.Variant));
                    break;
                case CVariant cVariant:
                    returnedVariables.Add(new SNameArg(EStringTableMod.SkipName, cVariant.Variant));
                    break;
                case IdHandle i:
                    returnedVariables.Add(new SNameArg(EStringTableMod.None, i));
                    returnedVariables.Add(new SNameArg(EStringTableMod.None, i.handle.Reference?.data));
                    break;
                // check all other CVariables
                case CVariable cvar:
                {
                    // add parent if not already in guidlist
                    // don't add array type parents, don't add IBufferVariantAccessor type parents
                    if (cvar.ParentVar != null
                        && !cvar.ParentVar.GetType().IsGenericType
                        && !(cvar.ParentVar is IBufferVariantAccessor)
                        && !(cvar.ParentVar is SEntityBufferType2)
                        && !guidlist.Contains(cvar.ParentVar.InternalGuid))
                    {
                        returnedVariables.Add(new SNameArg(EStringTableMod.None, cvar.ParentVar));
                    }

                    // add all normal REDProperties
                    returnedVariables.AddRange(cvar.GetExistingVariables(false)
                        .Select(_ => new SNameArg(EStringTableMod.None, _)));

                    // for all buffers
                    #region Buffer Hacks After Variables
                    switch (cvar)
                    {
                        case CFoliageResource cfr:
                            foreach (SFoliageResourceData item in cfr.Trees)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, item.Treetype));
                            foreach (SFoliageResourceData item in cfr.Grasses)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, item.Treetype));
                            break;
                        case CClipMap cm:
                            returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, cm.TerrainTiles));
                            break;
                        case CUmbraScene cus:
                            foreach (SUmbraSceneData item in cus.Tiles.elements) //handled in GetStrings
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, item));
                            break;
                        case CSkeletalAnimationSetEntry csase:
                            foreach (CVariantSizeType item in csase.Entries)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, item));
                            break;
                        case CLayerInfo cli:
                            returnedVariables.Add(new SNameArg(EStringTableMod.None, cli.ParentGroup.Reference?.data));
                            break;
                        case CMaterialInstance cmi:
                            foreach (CVariantSizeNameType iparam in cmi.InstanceParameters)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, iparam));
                            break;
                        case CMesh cm:
                            returnedVariables.Add(new SNameArg(EStringTableMod.None, cm.BoneNames));
                            break;
                        case CBehaviorGraphContainerNode bgcn:
                            foreach (var item in bgcn.Inputnodes)
                                returnedVariables.Add(new SNameArg(EStringTableMod.None, (item.Reference?.data)));
                            returnedVariables.Add(new SNameArg(EStringTableMod.None, bgcn.Unk1));
                            returnedVariables.Add(new SNameArg(EStringTableMod.None, bgcn.Unk2));
                            switch (bgcn)
                            {
                                case CBehaviorGraphStageNode bgsn:
                                    returnedVariables.Add(new SNameArg(EStringTableMod.None, bgsn.Outputnode.Reference?.data));
                                    break;
                                case CBehaviorGraphTopLevelNode bgtln:
                                    returnedVariables.Add(new SNameArg(EStringTableMod.None, bgtln.Outputnode.Reference?.data));
                                    break;
                                case CBehaviorGraphStateNode bgstn:
                                    returnedVariables.Add(new SNameArg(EStringTableMod.None, bgstn.Outputnode.Reference?.data));
                                    break;
                                case CBehaviorGraphStateMachineNode bgsmn:
                                    foreach (var item in bgsmn.Unk3)
                                        returnedVariables.Add(new SNameArg(EStringTableMod.None, (item.Reference?.data)));
                                    foreach (var item in bgsmn.Unk4)
                                        returnedVariables.Add(new SNameArg(EStringTableMod.None, (item.Reference?.data)));

                                    returnedVariables.Add(new SNameArg(EStringTableMod.None, bgsmn.Handle1.Reference?.data));
                                    returnedVariables.Add(new SNameArg(EStringTableMod.None, bgsmn.Outputnode.Reference?.data));

                                    foreach (var item in bgsmn.Inputnodes)
                                        returnedVariables.Add(new SNameArg(EStringTableMod.None, item.Reference?.data));
                                    returnedVariables.Add(new SNameArg(EStringTableMod.None, bgsmn.Unk1));
                                    returnedVariables.Add(new SNameArg(EStringTableMod.None, bgsmn.Unk2));
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case CBehaviorGraph cbg:
                            returnedVariables.Add(new SNameArg(EStringTableMod.None, cbg.Toplevelnode.Reference?.data));
                            foreach (IdHandle item in cbg.Variables1)
                                returnedVariables.Add(new SNameArg(EStringTableMod.None, item));
                            foreach (CHandle<CBehaviorVariable> item in cbg.Descriptions)
                                returnedVariables.Add(new SNameArg(EStringTableMod.None, item.Reference?.data));
                            foreach (IdHandle item in cbg.Vectorvariables1)
                                returnedVariables.Add(new SNameArg(EStringTableMod.None, item));
                            foreach (IdHandle item in cbg.Variables2)
                                returnedVariables.Add(new SNameArg(EStringTableMod.None, item));
                            foreach (IdHandle item in cbg.Vectorvariables2)
                                returnedVariables.Add(new SNameArg(EStringTableMod.None, item));
                            break;
                        case CNode cn:
                            foreach (CHandle<IAttachment> att in cn.AttachmentsChild)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, att));
                            foreach (CHandle<IAttachment> att in cn.AttachmentsReference)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, att));
                            if (cn is CEntity e)
                            {
                                foreach (CPtr<CComponent> component in e.Components)
                                    returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, component));
                                foreach (SEntityBufferType1 buffer in e.BufferV1)
                                    returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, buffer));
                                foreach (SEntityBufferType2 item in e.BufferV2.elements)
                                {
                                    returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, item.componentName));
                                    foreach (CVariantSizeTypeName el in item.variables.elements)
                                        returnedVariables.Add(new SNameArg(EStringTableMod.TypeFirst, el.Variant));
                                }
                            }
                            break;
                        case SAppearanceAttachment aa:
                            foreach (CVariable item in aa.Data.elements)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipName, item));
                            break;
                        case CCutsceneTemplate cct:
                            foreach (CVariantSizeType item in cct.Animevents.elements)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipName, item.Variant));
                            break;
                        case CStorySceneSection csss:
                            foreach (CVariantSizeType item in csss.sceneEventElements)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, item));
                            break;
                        case CStorySceneScript cssscpt:
                            foreach (CVariant item in cssscpt.BufferParameters)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipType, item));
                            break;
                        case CQuestScriptBlock qsb:
                            foreach (CVariant item in qsb.BufferParameters)
                                returnedVariables.Add(new SNameArg(EStringTableMod.SkipType, item));
                            break;
                        case CFXTrackItem cfxti:
                            returnedVariables.Add(new SNameArg(EStringTableMod.SkipNameAndType, cfxti.buffername));
                            break;
                        case CPhysicalCollision cpc:
                            returnedVariables.Add(new SNameArg(EStringTableMod.None, cpc.Collisiontypes));
                            break;
                        default:
                            break;
                    }
                    #endregion
                    break;
                }
                default:
                    break;
            }

            return returnedVariables;
            }

            void AddStrings(SNameArg tvar)
            {
                var var = tvar.Item2;

                if (var is SFoliageInstanceData)
                    return;

                // these CVariables have special requirements, for all others:  switch StringtableMod
                if (!(var is CBufferVLQInt32<CName> ||
                    var is IdHandle ||
                    var is SEntityBufferType1 ||
                    var is SUmbraSceneData)
                    )
                {
                    CheckVarNameAndTypes();
                }

                // array-type and ptr-type variables have special stringmods
                if (var is SUmbraSceneData)
                {
                    var h = (var as SUmbraSceneData).Umbratile;
                    if (!h.ChunkHandle)
                    {
                        AddUniqueToTable(h.ClassName);
                        var importtuple = new SImportEntry(h.ClassName, h.DepotPath, EImportFlags.Default);
                        if (!newimportslist.Contains(importtuple))
                        {
                            newimportslist.Add(importtuple);
                        }
                    }
                }
                //else if (var is CFlags)
                //{
                //    foreach (var flag in (var as CFlags).flags)
                //    {
                //        AddUniqueToTable(flag.Value);
                //    }
                //}
                else if (var is TagList)
                {
                    foreach (var tag in (var as TagList).tags)
                    {
                        AddUniqueToTable(tag.Value);
                    }
                }
                else if (var is IHandleAccessor h)
                {
                    if (!h.ChunkHandle)
                    {
                        AddUniqueToTable(h.ClassName);
                        var flags = EImportFlags.Default;
                        if (h.REDName == "template" && h.ClassName == "CEntityTemplate")
                        {
                            // this is needed because CDPR made one class (SWitcherSign)
                            // which also has a template handle:CEntityTemplate property lol
                            if (h.ParentVar is CEntity)
                                flags = EImportFlags.Template;
                        }
                            
                        if (var.cr2w.embedded.Any(_ => _.ImportPath == h.DepotPath && _.ImportClass == h.ClassName))
                            flags = EImportFlags.Inplace;

                        var importtuple = new SImportEntry(h.ClassName, h.DepotPath, flags);
                        if (!newimportslist.Contains(importtuple))
                        {
                            newimportslist.Add(importtuple);
                        }
                    }
                }
                else if (var is ISoftAccessor s)
                {
                    if (!(string.IsNullOrEmpty(s.ClassName) && string.IsNullOrEmpty(s.DepotPath)))
                    {
                        AddUniqueToTable(s.REDType);
                        var stuple = new SImportEntry(s.ClassName, s.DepotPath, EImportFlags.Soft);
                        if (!newsoftlist.Contains(stuple))
                        {
                            newsoftlist.Add(stuple);
                        }
                    }
                }
                else if (var is CName)
                {
                    var n = var as CName;
                    AddUniqueToTable(n.Value);
                }
                else if (var is IArrayAccessor a)
                {
                    if (var is IBufferAccessor buffer)
                    {
                        foreach (IEditableVariable ivar in buffer.GetEditableVariables())
                        {
                            if (ivar is IHandleAccessor ha)
                            {
                                if (!ha.ChunkHandle)
                                {
                                    AddUniqueToTable(ha.ClassName);
                                    var flags = EImportFlags.Default;
                                    if (ha.REDName == "template")
                                        flags = EImportFlags.Template;
                                    var importtuple = new SImportEntry(ha.ClassName, ha.DepotPath, flags);
                                    if (!newimportslist.Contains(importtuple))
                                    {
                                        newimportslist.Add(importtuple);
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
                        CheckVarNameAndTypes();

                        if (var is CArray<CName> aa)
                        {
                            foreach (var element in aa)
                            {
                                AddUniqueToTable(element.Value);
                            }
                        }
                    }
                }
                else if (var is CBufferVLQInt32<CName>)
                {
                    foreach (var element in (var as CBufferVLQInt32<CName>).elements)
                    {
                        if (element is CName )
                        {
                            AddUniqueToTable((element as CName).Value);
                        }
                    }
                }
                else if (var is IPtrAccessor)
                {
                }
                else if (var is IdHandle)
                {
                    AddUniqueToTable((var as IdHandle).handlename.Value);
                }
                else if (var is SEntityBufferType1)
                {
                    AddUniqueToTable((var as SEntityBufferType1).ComponentName.Value);
                }
                else if (var is IEnumAccessor enumAccessor)
                {
                    foreach (var enumstring in enumAccessor.Value)
                    {
                        AddUniqueToTable(enumstring);
                    }
                }
                else
                {
                    CheckVarNameAndTypes();

                    #region Buffer Hacks Before Variables
                    if (var is CEntity ent)
                    {
                        CHandle<CEntityTemplate> t = ent.Template;
                        if (t != null && !t.ChunkHandle)
                            AddUniqueToTable(t.ClassName);
                    }
                    #endregion
                }


                void CheckVarNameAndTypes()
                {
                    switch (tvar.Item1)
                    {
                        case EStringTableMod.SkipType:
                            AddUniqueToTable(var.REDName);
                            break;
                        case EStringTableMod.SkipName:
                            AddUniqueToTable(var.REDType);
                            break;
                        case EStringTableMod.SkipNameAndType:
                            break;
                        case EStringTableMod.TypeFirst:
                            AddUniqueToTable(var.REDType);
                            AddUniqueToTable(var.REDName);
                            break;
                        case EStringTableMod.None:
                        default:
                            AddUniqueToTable(var.REDName);
                            AddUniqueToTable(var.REDType);
                            break;
                    }
                }
            }

            void AddUniqueToTable(string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    // todo
                }
                else
                {

                    if (!newnameslist.ContainsKey(str))
                    {
                        // hack for CApexClothResource *sigh*
                        if (str == "apexMaterialNames")
                        {
                            if (!newnameslist.ContainsKey("apexBinaryAsset"))
                                newnameslist.Add("apexBinaryAsset", "apexBinaryAsset");
                            if (!newnameslist.ContainsKey("array:100,0,Uint8"))
                                newnameslist.Add("array:100,0,Uint8", "array:100,0,Uint8");
                        }

                        newnameslist.Add(str, str);
                    }
                }
            }
        }

        private void WriteHeader(BinaryWriter file)
        {
            WriteFileHeader(file);

            #region Write Tables
            m_tableheaders[1].itemCount = (uint)names.Count;
            m_tableheaders[1].offset = (uint) file.BaseStream.Position;
            WriteTable<CR2WName>(file.BaseStream, names.Select(_ => _.Name).ToArray(), 1);
            
            m_tableheaders[2].itemCount = (uint)imports.Count;
            m_tableheaders[2].offset = imports.Count > 0 ? (uint) file.BaseStream.Position : 0;
            WriteTable<CR2WImport>(file.BaseStream, imports.Select(_ => _.Import).ToArray(), 2);

            m_tableheaders[3].itemCount = (uint)properties.Count;
            m_tableheaders[3].offset = (uint) file.BaseStream.Position;
            WriteTable<CR2WProperty>(file.BaseStream, properties.Select(_ => _.Property).ToArray(), 3);

            m_tableheaders[4].itemCount = (uint)chunks.Count;
            m_tableheaders[4].offset = (uint) file.BaseStream.Position;
            WriteTable<CR2WExport>(file.BaseStream, chunks.Select(_ => _.Export).ToArray(), 4);

            if (buffers.Count > 0)
            {
                m_tableheaders[5].itemCount = (uint)buffers.Count;
                m_tableheaders[5].offset = (uint)file.BaseStream.Position;
                WriteTable<CR2WBuffer>(file.BaseStream, buffers.Select(_ => _.Buffer).ToArray(), 5);
            }

            if (embedded.Count > 0)
            {
                m_tableheaders[6].itemCount = (uint)embedded.Count;
                m_tableheaders[6].offset = (uint)file.BaseStream.Position;
                WriteTable<CR2WEmbedded>(file.BaseStream, embedded.Select(_ => _.Embedded).ToArray(), 6);
            }
            #endregion
        }

        private void WriteFileHeader(BinaryWriter file)
        {
            file.BaseStream.Seek(0, SeekOrigin.Begin);

            file.BaseStream.WriteStruct<uint>(MAGIC);
            file.BaseStream.WriteStruct<CR2WFileHeader>(m_fileheader);
            file.BaseStream.WriteStructs<CR2WTable>(m_tableheaders); // offsets change if stringtable changes
            WriteStringBuffer(file.BaseStream);
        }

        private void WriteData(BinaryWriter bw)
        {
            // Write chunk data
            for (var i = 0; i < chunks.Count; i++)
            {
                chunks[i].WriteData(bw);
            }

            m_fileheader.fileSize = (uint)bw.BaseStream.Position;

            //Write Buffer data
            if (m_hasInternalBuffer)
            {
                for (var i = 0; i < buffers.Count; i++)
                {
                    buffers[i].WriteData(bw);
                }
            }
            m_fileheader.bufferSize = (uint)bw.BaseStream.Position;

            // Write embedded data
            for (var i = 0; i < embedded.Count; i++)
            {
                embedded[i].WriteData(bw);
            }
        }


        private void WriteStringBuffer(Stream stream)
        {
            m_tableheaders[0].crc32 = Crc32Algorithm.Compute(m_strings);
            stream.Write(m_strings, 0, m_strings.Length);
        }


        private void WriteTable<T>(Stream stream, T[] array, int index) where T : struct
        {
            if (array.Length == 0)
                return;

            var crc = new Crc32Algorithm(false);
            stream.WriteStructs<T>(array, crc);
            m_tableheaders[index].crc32 = crc.HashUInt32;
        }



        #endregion
    }
}

