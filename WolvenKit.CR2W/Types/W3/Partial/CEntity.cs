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
	public partial class CEntity : CNode
	{
		//[Ordinal(1)] [RED("components", 2,0)] 		public CArray<CPtr<CComponent>> Components { get; set;}

		[Ordinal(2)] [RED("template")] 		public CHandle<CEntityTemplate> Template { get; set;}

		[Ordinal(3)] [RED("streamingDataBuffer")] 		public SharedDataBuffer StreamingDataBuffer { get; set;}

		[Ordinal(4)] [RED("streamingDistance")] 		public CUInt8 StreamingDistance { get; set;}

		[Ordinal(5)] [RED("entityStaticFlags")] 		public CEnum<EEntityStaticFlags> EntityStaticFlags { get; set;}

		[Ordinal(6)] [RED("autoPlayEffectName")] 		public CName AutoPlayEffectName { get; set;}

		[Ordinal(7)] [RED("entityFlags")] 		public CUInt8 EntityFlags { get; set;}

		[Ordinal(8)] [RED("name")] 		public CString Name { get; set;}

		[Ordinal(9)] [RED("forceAutoHideDistance")] 		public CUInt16 ForceAutoHideDistance { get; set;}

		public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new CEntity(cr2w, parent, name);

        public override string GetPreview()
        {
            if (Template != null && !Template.ChunkHandle && !string.IsNullOrEmpty(Template.DepotPath))
            {
                return (Template.ClassName ?? "") + ":" + Template.DepotPath;
            }
            else if (StreamingDataBuffer != null && StreamingDataBuffer.Bufferdata != null)
            {
                var extraCR2W = new CR2WFile();
                if (extraCR2W.Read(StreamingDataBuffer.Bufferdata.Bytes) == EFileReadErrorCodes.NoError)
                {
                    for (int i = 0; i < extraCR2W.chunks.Count; i += 1)
                    {
                        string chunk_preview = extraCR2W.chunks[i].Preview;
                        if (!string.IsNullOrEmpty(chunk_preview))
                        {
                            return chunk_preview;
                        }
                    }
                } 
            }
            return base.GetPreview();
        }

    }
}
