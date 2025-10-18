using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;
using WolvenKit.Common.Model;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
	public class CMeshComponent : CMeshTypeComponent
	{
		[Ordinal(1)] [RED("mesh")] 		public CHandle<CMesh> Mesh { get; set;}

		public CMeshComponent(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new CMeshComponent(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

        public override string GetPreview()
        {
            string mesh_preview = ""; 
            if (Name != null)
            {
                mesh_preview += "[" + Name.val + "] ";
            }
            if (Mesh != null && !Mesh.ChunkHandle && !string.IsNullOrEmpty(Mesh.DepotPath))
            {
                mesh_preview += Mesh.DepotPath;
            }
            return mesh_preview;
        }

    }
}
