using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
    [DataContract(Namespace = "")]
    [REDMeta]
    public class CR4PhotomodeMenu : CR4MenuBase
    {
        [Ordinal(1)] [RED("_cachedTutorialVisibility")] public CBool CachedTutorialVisibility { get; set; }

        [Ordinal(2)] [RED("_fxOnScreenshotSaved")] public CScriptedFlashFunction FxOnScreenshotSaved { get; set; }

        [Ordinal(3)] [RED("_setCurrentFrameScale")] public CScriptedFlashFunction SetCurrentFrameScale { get; set; }

        public CR4PhotomodeMenu(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }

        public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new CR4PhotomodeMenu(cr2w, parent, name);

        public override void Read(BinaryReader file, uint size) => base.Read(file, size);

        public override void Write(BinaryWriter file) => base.Write(file);

    }
}
