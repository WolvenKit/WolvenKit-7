using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;

namespace WolvenKit.CR2W.Types
{
    [DataContract(Namespace = "")]
    [REDMeta]
    public class CMeshletMesh : CResource
    {
        [Ordinal(1)] [RED("bounds")] public Box Bounds { get; set; }

        [Ordinal(2)] [RED("header")] public SMeshletDataHeader Header { get; set; }

        [Ordinal(3)] [RED("data")] public DeferredDataBuffer Data { get; set; }

        public CMeshletMesh(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }

        public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new CMeshletMesh(cr2w, parent, name);

        public override void Read(BinaryReader file, uint size) => base.Read(file, size);

        public override void Write(BinaryWriter file) => base.Write(file);
    }
}
