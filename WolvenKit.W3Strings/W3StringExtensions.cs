using System;
using System.Collections.Generic;
using System.IO;

namespace WolvenKit.W3Strings
{
    public static class W3StringExtensions
    {
        public class ReadBit6Result
        {
            public readonly int value;
            public readonly int length;

            public ReadBit6Result(int value, int length)
            {
                this.value = value;
                this.length = length;
            }
        }

        public static ReadBit6Result ReadBit6(this BinaryReader stream)
        {
            var first = stream.ReadByte();
            var length = 1;
            var result = first & 0x3F;

            if ((first & 0x40) != 0)
            {
                var shift = 6;
                // at most 4 more bytes, the last one uses all 8 bits
                for (var i = 0; i < 4; i++)
                {
                    var b = stream.ReadByte();
                    length += 1;
                    if (i == 3)
                    {
                        result |= b << shift;
                        break;
                    }

                    result |= (b & 0x7F) << shift;
                    shift += 7;
                    if ((b & 0x80) == 0)
                        break;
                }
            }

            if ((first & 0x80) != 0)
                result = -result;

            return new ReadBit6Result(result, length);
        }

        // first byte:
        //      01111111
        //      ^^^^^^^^
        //      |||    |
        //      |||____|_ 6 bit value
        //      ||_______ flag: next byte required
        //      |________ flag: signed value

        // next bytes:
        //      11111111
        //      ^^^^^^^^
        //      ||     |
        //      ||_____|_ 7 bit value
        //      |________ flag: next byte required

        //let b = stream.read_u8();

        //// lower 6 bit
        //let mut value = (b & 0b0011_1111) as u32;

        //if b & 0b0100_0000 == 64 {
        //    let mut shift = 6;          // first byte 6 lowest bits
        //    loop {
        //        let next = stream.read_u8();

        //        value |= ((next & 0b0111_1111) as u32) << shift;
        //        shift += 7;             // each next byte 7 higher bits

        //        if (next & 0b1000_0000 == 0) || (shift >= 27) {
        //            break;
        //        }
        //    }
        //}

        //// is signed?
        //if b & 0b1000_0000 == 128 { -1 * value as i32 } else { value as i32 }

        public static UInt32 WriteBit6(this BinaryWriter stream, int c)
        {
            var value = (uint)Math.Abs((long)c);

            var b = (byte)(value & 0x3F);
            if (value > 0x3F)
                b |= 0x40;
            // the engine sets the sign flag for zero as well
            if (c <= 0)
                b |= 0x80;
            stream.Write(b);
            UInt32 written = 1;

            value >>= 6;
            while (value > 0)
            {
                b = (byte)(value & 0x7F);
                value >>= 7;
                if (value > 0)
                    b |= 0x80;
                stream.Write(b);
                written += 1;
            }
            return written;
        }
    }
}