using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
	public class SMeshCookedData : CVariable
	{
		[Ordinal(1)] [RED("collisionInitPositionOffset")] 		public Vector CollisionInitPositionOffset { get; set;}

		[Ordinal(2)] [RED("dropOffset")] 		public Vector DropOffset { get; set;}

#if NGE_VERSION
		[Ordinal(3)] [RED("bonePositions", 47,0)] 		public CArray<Vector> BonePositions { get; set;}

		[Ordinal(4)] [RED("renderLODs", 47,0)] 		public CArray<CFloat> RenderLODs { get; set;}

		[Ordinal(5)] [RED("renderChunks", 47,0)] 		public CByteArray RenderChunks { get; set;}
#else
		[Ordinal(3)] [RED("bonePositions", 46,0)] 		public CArray<Vector> BonePositions { get; set;}

		[Ordinal(4)] [RED("renderLODs", 46,0)] 		public CArray<CFloat> RenderLODs { get; set;}

		[Ordinal(5)] [RED("renderChunks", 46,0)] 		public CByteArray RenderChunks { get; set;}
#endif

		[Ordinal(6)] [RED("renderBuffer")] 		public DeferredDataBuffer RenderBuffer { get; set;}

		[Ordinal(7)] [RED("quantizationScale")] 		public Vector QuantizationScale { get; set;}

		[Ordinal(8)] [RED("quantizationOffset")] 		public Vector QuantizationOffset { get; set;}

		[Ordinal(9)] [RED("vertexBufferSize")] 		public CUInt32 VertexBufferSize { get; set;}

		[Ordinal(10)] [RED("indexBufferSize")] 		public CUInt32 IndexBufferSize { get; set;}

		[Ordinal(11)] [RED("indexBufferOffset")] 		public CUInt32 IndexBufferOffset { get; set;}
#if NGE_VERSION
		[Ordinal(12)] [RED("blasLODsOffsets", 47,0)] 		public CArray<CUInt32> BlasLODsOffsets { get; set;}

		[Ordinal(13)] [RED("blasBuffer")] 		public DeferredDataBuffer BlasBuffer { get; set;}
#endif

		public SMeshCookedData(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new SMeshCookedData(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

	}
}