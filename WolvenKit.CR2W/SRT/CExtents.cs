using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.CR2W.Types;

namespace WolvenKit.CR2W.SRT
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CExtents
    {
        public float[] m_cMin { get; set; } = new float[3];
        public float[] m_cMax { get; set; } = new float[3];

        public void Write(BinaryWriter bw)
        {
            for (int i = 0; i < 3; i++)
            {
                bw.Write(m_cMin[i]);
            }
            for (int i = 0; i < 3; i++)
            {
                bw.Write(m_cMax[i]);
            }
        }
    }
}
