using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using WolvenKit.CR2W.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace WolvenKit.CR2W.Types
{
    /// <summary>
    /// Handles are Int32 that store 
    /// if > 0: a reference to a chunk inside the cr2w file (aka Soft)
    /// if < 0: a reference to a string in the imports table (aka Pointer)
    /// Exposed are 
    /// if ChunkHandle: 
    /// if ImportHandle: A string Handle, string Filetype and ushort Flags
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [REDMeta()]
    public class CHandle<T> : CVariable, IHandleAccessor where T : CVariable
    {
        public CHandle(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name)
        {
        }

        #region Properties
        [DataMember(EmitDefaultValue = false)]
        public bool ChunkHandle { get; set; }

        // Soft
        [DataMember(EmitDefaultValue = false)]
        public string DepotPath { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ClassName { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public ushort Flags { get; set; }

        // Pointer
        [DataMember(EmitDefaultValue = false)]
        public CR2WExportWrapper Reference { get; set; }

        public string ReferenceType => REDType.Split(':').Last();
        #endregion

        #region Methods

        public void ChangeHandleType()
        {
            ChunkHandle = !ChunkHandle;
            // change to external path
            if (ChunkHandle)
            {
                
            }
            // change to chunk handle
            else
            {
                
            }
        }

        public override void Read(BinaryReader file, uint size)
        {
            SetValueInternal(file.ReadInt32());
            SetIsSerialized();
        }

        private void SetValueInternal(int val)
        {
            if (val >= 0)
                this.ChunkHandle = true;


            if (ChunkHandle)
            {
                if (val == 0)
                {
                    Reference = null;
                }
                else if (val < 0 || val >= cr2w.chunks.Count) {
                    Reference = null;
                }
                else
                {
                    Reference = cr2w.chunks[val - 1];
                    //Populate the reverse-lookups
                    Reference.AdReferences.Add(this);
                    cr2w.chunks[LookUpChunkIndex()].AbReferences.Add(this);
                    //Soft mount the chunk except root chunk
                    if (Reference.ChunkIndex != 0)
                    {
                        Reference.MountChunkVirtually(LookUpChunkIndex());
                    }
                }
            }
            else
            {
                DepotPath = cr2w.imports[-val - 1].DepotPathStr;

                var filetype = cr2w.imports[-val - 1].Import.className;
                ClassName = cr2w.names[filetype].Str;

                Flags = cr2w.imports[-val - 1].Import.flags;
            }
        }

        /// <summary>
        /// Call after the stringtable was generated!
        /// </summary>
        /// <param name="file"></param>
        public override void Write(BinaryWriter file)
        {
            int val = 0;
            if (ChunkHandle)
            {
                if (Reference != null)
                    val = Reference.ChunkIndex + 1;
            }
            else
            {
                var import = cr2w.imports.FirstOrDefault(_ => _.DepotPathStr == DepotPath && _.ClassNameStr == ClassName);
                val = -cr2w.imports.IndexOf(import) - 1;
            }
            file.Write(val);
        }

        /// <summary>
        /// Sets new handle: string if depot path (must be "classname:path"), CR2WExportWrapper or chunkIndex if chunk handle
        /// </summary>
        public override CVariable SetValue(object val)
        {
            switch (val)
            {
                case string s:
                    string[] parts = s.Split(':');
                    this.ClassName = parts[0];
                    this.DepotPath = parts[1];
                    this.ChunkHandle = false;
                    SetIsSerialized();
                    break;
                case CR2WExportWrapper wrapper:
                    SetValueInternal(wrapper.ChunkIndex + 1); // TODO check why +1 is needed
                    SetIsSerialized();
                    break;
                case int o:
                    SetValueInternal(o);
					SetIsSerialized();
                    break;
                case IHandleAccessor cvar:
                    this.ChunkHandle = cvar.ChunkHandle;
                    this.DepotPath = cvar.DepotPath;
                    this.ClassName = cvar.ClassName;
                    this.Flags = cvar.Flags;

                    this.Reference = cvar.Reference;
					SetIsSerialized();
                    break;
                case ISoftAccessor soft:
                    this.ChunkHandle = false;
                    this.DepotPath = soft.DepotPath;
                    this.ClassName = soft.ClassName;
                    this.Flags = soft.Flags;
                    SetIsSerialized();
                    break;
            }

            return this;
        }

        public override CVariable Copy(CR2WCopyAction context)
        {
            var copy = (CHandle<T>)base.Copy(context);
            copy.ChunkHandle = ChunkHandle;

            // Soft
            copy.DepotPath = DepotPath;
            copy.ClassName = ClassName;
            copy.Flags = Flags;

            // Ptr
            if (ChunkHandle && Reference != null)
            {
                CR2WExportWrapper newref = context.TryLookupReference(Reference, copy);
                if (newref != null)
                    copy.Reference = newref;
            }
            
            return copy;
        }

        public override string ToString()
        {
            if (ChunkHandle)
            {
                if (Reference == null)
                    return "NULL";
                else
                    return $"{Reference.REDType} #{Reference.ChunkIndex}";
            }

            return ClassName + ": " + DepotPath;
        }
        public override string REDLeanValue()
        {
            if (Reference == null)
                return "";
            return $"{Reference.ChunkIndex}";
        }

        #endregion
    }
}
