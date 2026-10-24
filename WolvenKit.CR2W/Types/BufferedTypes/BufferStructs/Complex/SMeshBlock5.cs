using System.Collections.Generic;
using System.IO;

using System.Diagnostics;
using System;
using System.Linq;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;

namespace WolvenKit.CR2W.Types
{
    [REDMeta(EREDMetaInfo.REDStruct)]
    public class SMeshBlock5 : CVariable
    {
        private const int fixedbuffersize = 46;
        // since version 164 CPhysicsDestructionResource serializes 3 uints and a byte more per collision
        private const int fixedbuffersize164 = 59;

        [Ordinal(0)] [RED] public CUInt16 bytesize { get; set; }
        [Ordinal(1)] [RED] public CBytes unk1 { get; set; }

        public SMeshBlock5(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name)
        {
            bytesize = new CUInt16(cr2w, this, nameof(bytesize)) { IsSerialized = true };
            unk1 = new CBytes(cr2w, this, nameof(unk1)) { IsSerialized = true };
        }

        public override void Read(BinaryReader file, uint size)
        {
            bytesize.Read(file, 2);

            var expectedsize = cr2w.GetFileHeader().version >= 164 ? fixedbuffersize164 : fixedbuffersize;
            if ((int)bytesize.val != expectedsize)
                throw new NotImplementedException();

            unk1.Read(file, (uint)bytesize.val - 2);
            SetIsSerialized();
        }

        public override void Write(BinaryWriter file)
        {
            bytesize.Write(file);
            unk1.Write(file);
        }

        public static CVariable Create(CR2WFile cr2w, CVariable parent, string name)
        {
            return new SMeshBlock5(cr2w, parent, name);
        }

        public override string ToString()
        {
            return "";
        }
    }
}