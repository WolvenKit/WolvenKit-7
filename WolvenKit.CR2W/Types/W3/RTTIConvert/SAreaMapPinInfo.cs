using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
	public class SAreaMapPinInfo : CVariable
	{
		[Ordinal(1)] [RED("areaType")] 		public CInt32 AreaType { get; set;}

		[Ordinal(2)] [RED("position")] 		public Vector Position { get; set;}

		[Ordinal(3)] [RED("worldPath")] 		public CString WorldPath { get; set;}

#if NGE_VERSION
		[Ordinal(4)] [RED("localisationName")] 		public CName LocalisationName { get; set;}

		[Ordinal(5)] [RED("localisationDescription")] 		public CName LocalisationDescription { get; set;}
#else
		[Ordinal(4)] [RED("requiredChunk")] 		public CName RequiredChunk { get; set;}

		[Ordinal(5)] [RED("localisationName")] 		public CName LocalisationName { get; set;}

		[Ordinal(6)] [RED("localisationDescription")] 		public CName LocalisationDescription { get; set;}
#endif

		public SAreaMapPinInfo(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new SAreaMapPinInfo(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

	}
}