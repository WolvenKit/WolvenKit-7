using System.Text;

namespace WolvenKit.W3Strings
{
    public class Latin1DecoderFallback : DecoderFallback
    {
        public override int MaxCharCount => 1;

        public override DecoderFallbackBuffer CreateFallbackBuffer() => new Latin1DecoderFallbackBuffer();

        private class Latin1DecoderFallbackBuffer : DecoderFallbackBuffer
        {
            private byte[] bytes;
            private int position;

            public override int Remaining => bytes == null ? 0 : bytes.Length - position;

            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                bytes = bytesUnknown;
                position = 0;
                return bytes.Length > 0;
            }

            public override char GetNextChar() => Remaining > 0 ? (char)bytes[position++] : '\0';

            public override bool MovePrevious()
            {
                if (position == 0)
                    return false;
                position--;
                return true;
            }

            public override void Reset()
            {
                bytes = null;
                position = 0;
            }
        }
    }
}
