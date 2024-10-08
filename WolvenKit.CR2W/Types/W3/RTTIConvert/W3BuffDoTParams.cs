using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
	public class W3BuffDoTParams : W3BuffCustomParams
	{
		[Ordinal(1)] [RED("isEnvironment")] 		public CBool IsEnvironment { get; set;}

		[Ordinal(2)] [RED("isPerk20Active")] 		public CBool IsPerk20Active { get; set;}
#if NGE_VERSION
        [Ordinal(3)] [RED("isFromBomb")] public CBool isFromBomb { get; set; }
#endif

		public W3BuffDoTParams(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new W3BuffDoTParams(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

	}
}