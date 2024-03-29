using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;

namespace WolvenKit.CR2W.Types
{
    [DataContract(Namespace = "")]
    [REDMeta]
    public class SMeshletDataHeader : CVariable
    {
        [Ordinal(1)] [RED("offset")] public Vector Offset { get; set; }

        [Ordinal(2)] [RED("quantizationOffset")] public Vector QuantizationOffset { get; set; }

        [Ordinal(3)] [RED("quantizationScale")] public Vector QuantizationScale { get; set; }

        [Ordinal(4)] [RED("numPackedVertices")] public CUInt32 NumPackedVertices { get; set; }

        [Ordinal(5)] [RED("numPackedIndices")] public CUInt32 NumPackedIndices { get; set; }

        [Ordinal(5)] [RED("numPackedMeshlets")] public CUInt32 NumPackedMeshlets { get; set; }

        public SMeshletDataHeader(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }

        public static CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new SMeshletDataHeader(cr2w, parent, name);

        public override void Read(BinaryReader file, uint size) => base.Read(file, size);

        public override void Write(BinaryWriter file) => base.Write(file);
    }
}
