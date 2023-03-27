using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using WolvenKit.Common.Model;
using WolvenKit.CR2W.Reflection;

namespace WolvenKit.CR2W.Types
{
    [REDMeta()]
    public class CByteArray : CVariable, IByteSource, IREDPrimitive
    {
        public string InternalType { get; set; }
        public override string REDType => string.IsNullOrEmpty(InternalType) ? base.REDType : InternalType;

        public CByteArray(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) {
            if (name == "flatCompiledData")
            {
                InternalType = "array:2,0,Uint8";
            }
        }

        public byte[] Bytes { get; set; }
        public byte[] GetBytes() => Bytes;

        public override void Read(BinaryReader file, uint size)
        {
            var arraysize = file.ReadUInt32();
            Bytes = file.ReadBytes((int) arraysize);
            SetIsSerialized();
        }

        public override void Write(BinaryWriter file)
        {
            if (Bytes != null && Bytes.Length != 0)
            {
                file.Write((uint)Bytes.Length);
                file.Write(Bytes);
            }
            else
            {
                file.Write(0x00);
            }
        }

        public override CVariable SetValue(object val)
        {
            switch (val)
            {
                case byte[] bytes:
                    Bytes = bytes;
					SetIsSerialized();
                    break;
                case CByteArray cvar:
                    this.Bytes = cvar.Bytes;
					SetIsSerialized();
                    break;
            }

            return this;
        }

        public override CVariable Copy(CR2WCopyAction context)
        {
            var copy = (CByteArray) base.Copy(context);

            if (Bytes == null) return copy;
            
            var newbytes = new byte[Bytes.Length];
            Bytes.CopyTo(newbytes, 0);
            copy.Bytes = newbytes;
            
            return copy;
        }

        public override string ToString()
        {
            if (Bytes == null)
                Bytes = Array.Empty<byte>();

            return  Bytes.Length + " bytes, MD5: " + MD5String();
        }

        public object GetValueObject() => Bytes;

        public string MD5String()
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(Bytes, 0, Bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);

            using (var MD5Instance = System.Security.Cryptography.MD5.Create())
            {
                var hashResult = MD5Instance.ComputeHash(stream);
                return BitConverter.ToString(hashResult).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}