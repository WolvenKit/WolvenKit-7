using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
	public class VirtualAnimationPoseFK : CVariable
	{
		[Ordinal(1)] [RED("time")] 		public CFloat Time { get; set;}

		[Ordinal(2)] [RED("controlPoints")] 		public Vector ControlPoints { get; set;}

		[Ordinal(3)] [RED("indices", 2,0)] 		public CArray<CInt32> Indices { get; set;}

#if NGE_VERSION
		[Ordinal(4)] [RED("transforms", 138,0)] 		public CArray<EngineQsTransform> Transforms { get; set;}
#else
		[Ordinal(4)] [RED("transforms", 133,0)] 		public CArray<EngineQsTransform> Transforms { get; set;}
#endif

		public VirtualAnimationPoseFK(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new VirtualAnimationPoseFK(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

	}
}