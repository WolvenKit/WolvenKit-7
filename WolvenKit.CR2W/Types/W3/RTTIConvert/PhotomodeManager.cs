using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
    [DataContract(Namespace = "")]
    [REDMeta]
    public class PhotomodeManager : CVariable
    {
        [Ordinal(1)] [RED("m_photomodeEnabled")] public CBool M_photomodeEnabled { get; set; }

        [Ordinal(2)] [RED("m_photomodeEnabledStep1")] public CBool M_photomodeEnabledStep1 { get; set; }

        [Ordinal(3)] [RED("m_photomodeEnabledStep2")] public CBool M_photomodeEnabledStep2 { get; set; }

        [Ordinal(4)] [RED("m_lastActiveCam")] public CHandle<CCustomCamera> M_lastActiveCam { get; set; }

        [Ordinal(5)] [RED("m_lastActiveContext")] public CName M_lastActiveContext { get; set; }

        public PhotomodeManager(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }

        public static CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new PhotomodeManager(cr2w, parent, name);

        public override void Read(BinaryReader file, uint size) => base.Read(file, size);

        public override void Write(BinaryWriter file) => base.Write(file);

    }
}
