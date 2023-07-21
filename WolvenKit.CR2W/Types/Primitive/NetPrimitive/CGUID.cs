﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using WolvenKit.Common.Model;
using WolvenKit.CR2W.Reflection;

namespace WolvenKit.CR2W.Types
{
    [REDMeta()]
    public class CGUID : CVariable, IREDPrimitive
    {
        public byte[] guid;

        public CGUID(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name)
        {
            guid = new byte[16];
        }

        [DataMember]
        public string GuidString
        {
            get { return ToString(); }
            set
            {
                Guid g;
                if (Guid.TryParse(value, out g))
                {
                    guid = g.ToByteArray();
                    SetIsSerialized();
                }
            }
        }

        public override void Read(BinaryReader file, uint size)
        {
            guid = file.ReadBytes(16);
            SetIsSerialized();
        }

        public override void Write(BinaryWriter file)
        {
            file.Write(guid);
        }

        public override CVariable SetValue(object val)
        {
            switch (val)
            {
                case byte[] o:
                    guid = o;
					SetIsSerialized();
                    break;
                case CGUID cvar:
                    this.guid = cvar.guid;
					SetIsSerialized();
                    break;
                case string str:
                    GuidString = str;
                    // ^ SetIsSerialized in setter on success
                    break;
            }

            return this;
        }

        public override CVariable Copy(CR2WCopyAction context)
        {
            var var = (CGUID) base.Copy(context);
            var.guid = guid;
            return var;
        }

        public object GetValueObject() => guid;

        public override string ToString()
        {
            if (guid == null || guid.Length == 0)
            {
                guid = new byte[16];
            }
            return new Guid(guid).ToString();
        }
    }
}