using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
    [DataContract(Namespace = "")]
    [REDMeta]
    public class STempSkill : CVariable
    {
        [Ordinal(1)] [RED("skillType")] public CEnum<ESkill> SkillType { get; set; }

        [Ordinal(2)] [RED("skillLevel")] public CInt32 SkillLevel { get; set; }

        [Ordinal(3)] [RED("skillSlot")] public CInt32 SkillSlot { get; set; }

        [Ordinal(3)] [RED("equipped")] public CBool Equipped { get; set; }

        [Ordinal(3)] [RED("temporary")] public CBool Temporary { get; set; }

        public STempSkill(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }

        public static CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new STempSkill(cr2w, parent, name);

        public override void Read(BinaryReader file, uint size) => base.Read(file, size);

        public override void Write(BinaryWriter file) => base.Write(file);

    }
}
