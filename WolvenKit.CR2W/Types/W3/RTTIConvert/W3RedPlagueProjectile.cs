using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
    [DataContract(Namespace = "")]
    [REDMeta]
    public class W3RedPlagueProjectile : W3LeshyRootProjectile
    {
        [Ordinal(1)] [RED("surface")] public CGameplayFXSurfacePost Surface { get; set; }

        [Ordinal(2)] [RED("damageAction")] public CHandle<W3DamageAction> DamageAction { get; set; }

        public W3RedPlagueProjectile(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }

        public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new W3RedPlagueProjectile(cr2w, parent, name);

        public override void Read(BinaryReader file, uint size) => base.Read(file, size);

        public override void Write(BinaryWriter file) => base.Write(file);

    }
}
