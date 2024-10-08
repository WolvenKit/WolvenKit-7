using System;
using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;


namespace WolvenKit.CR2W.Types
{
    [REDMeta()]
    public class CFloat : CVariable, IREDPrimitive
    {
        public CFloat()
        {
            
        }
        public CFloat(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }

        [DataMember]
        public float val { get; set; }

        public override void Read(BinaryReader file, uint size)
        {
            val = file.ReadSingle();
            SetIsSerialized();
        }

        public override void Write(BinaryWriter file)
        {
            file.Write(val);
        }

        public override CVariable SetValue(object val)
        {
            switch (val)
            {
                case double d:
                    this.val = Convert.ToSingle(d);
                    SetIsSerialized();
                    break;
                case float f:
                    this.val = f;
					SetIsSerialized();
                    break;
                case CFloat cvar:
                    this.val = cvar.val;
					SetIsSerialized();
                    break;
            }

            return this;
        }

        public override CVariable Copy(CR2WCopyAction context)
        {
            var var = (CFloat) base.Copy(context);
            var.val = val;
            return var;
        }

        public object GetValueObject() => val;

        public override string ToString()
        {
            return val.ToString();
        }
    }
}
