using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
#if NGE_VERSION
    public class CMergedMeshComponent : CDrawableComponent
#else
	public class CMergedMeshComponent : CMeshComponent
#endif
	{
#if NGE_VERSION
        [Ordinal(1)] [RED("objects", 69, 0)] public CArray<GlobalVisID> Objects { get; set; }

        [Ordinal(2)] [RED("meshlet")] public CHandle<CMeshletMesh> Meshlet { get; set; }
#else
		[Ordinal(1)] [RED("objects", 67,0)] 		public CArray<GlobalVisID> Objects { get; set;}

		[Ordinal(2)] [RED("renderMask")] 		public CUInt8 RenderMask { get; set;}
#endif

		[Ordinal(3)] [RED("streamingDistance")] 		public CFloat StreamingDistance { get; set;}

		public CMergedMeshComponent(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new CMergedMeshComponent(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

	}
}
