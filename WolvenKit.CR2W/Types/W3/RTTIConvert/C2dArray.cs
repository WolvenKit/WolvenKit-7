using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
	public class C2dArray : CResource
	{
#if NGE_VERSION
        [Ordinal(1)] [RED("headers", 13,0)] 		public CArray<CString> Headers { get; set;}

		[Ordinal(2)] [RED("data", 13,0, 13,0)] 		public CArray<CArray<CString>> Data { get; set;}
#else
		[Ordinal(1)] [RED("headers", 12,0)] 		public CArray<CString> Headers { get; set;}

		[Ordinal(2)] [RED("data", 12,0, 12,0)] 		public CArray<CArray<CString>> Data { get; set;}
#endif

        public C2dArray(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new C2dArray(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

	}
}
