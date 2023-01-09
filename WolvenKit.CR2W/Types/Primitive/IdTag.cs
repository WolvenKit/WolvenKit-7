using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.CR2W.Reflection;

namespace WolvenKit.CR2W.Types
{
    [REDMeta()]
    public class IdTag : CVariable, IREDPrimitive
    {
        public byte _type { get; set; }
        public byte[] _guid { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string GuidString
        {
            get { return new Guid(_guid).ToString(); }
            set
            {
                if (Guid.TryParse(value, out Guid g))
                {
                    _guid = g.ToByteArray();
                }
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string TypeString
        {
            get { return Convert.ToString(_type); }
            set
            {
                if(Byte.TryParse(value, out byte b))
                {
                    _type = b;
                }
            }
        }

        public IdTag(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }

        public override void Read(BinaryReader file, uint size)
        {
            _type = file.ReadByte();
            _guid = file.ReadBytes(16);
            SetIsSerialized();
        }

        public override void Write(BinaryWriter file)
        {
            file.Write(_type);
            file.Write(_guid);
        }

        public override CVariable Copy(CR2WCopyAction context)
        {
            var var = (IdTag)base.Copy(context);
            var._type = _type;
            var._guid = _guid;
            return var;
        }

        public override CVariable SetValue(object val)
        {
            switch (val)
            {
                case byte[] ba:
                    _guid = ba;
                    SetIsSerialized();
                    break;
                case byte b:
                    _type = b;
                    SetIsSerialized();
                    break;
                case IdTag cvar:
                    _guid = cvar._guid;
                    _type = cvar._type;
                    SetIsSerialized();
                    break;
                case string s64:
                    try
                    {
                        _guid = Convert.FromBase64String(s64);
                        SetIsSerialized();
                    }
                    catch (Exception e) { }
                    break;
                case long l:
                case int i:
                    try
                    {
                        _type = Convert.ToByte(val);
                        SetIsSerialized();
                    } catch (Exception e) {}
                    break;
            }

            return this;
        }

        public override string ToString()
        {
            if (_guid == null)
            {
                var buffer = new byte[16];
                for (int i = 0; i < 16; i += 1)
                {
                    buffer[i] = 0x0;
                }
                return $"[ {_type} ] {new Guid(buffer)}";
            }
            else
            {
                return $"[ {_type} ] {new Guid(_guid)}";
            }
        }

        public object GetValueObject()
        {
            if (_guid == null)
            {
                var buffer = new byte[16];
                for (int i = 0; i < 16; i += 1)
                {
                    buffer[i] = 0x0;
                }
                return $"{Convert.ToInt32(_type)}:{Convert.ToBase64String(buffer)}";
            }
            else
            {
                return $"{Convert.ToInt32(_type)}:{Convert.ToBase64String(_guid)}";
            }
        }
    }
}