namespace HALC
{
    internal class BitArrayBuilder
    {
        private ByteArrayBuilder _byteArrayBuilder;

        public BitArrayBuilder()
        {
            _byteArrayBuilder = new ByteArrayBuilder();
        }

        public void Append(bool bit)
        {
            _byteArrayBuilder.Append(bit ? (byte)1 : (byte)0);
        }

        public void Append(byte b)
        {
            Append((b & (1 << 7)) != 0);
            Append((b & (1 << 6)) != 0);
            Append((b & (1 << 5)) != 0);
            Append((b & (1 << 4)) != 0);
            Append((b & (1 << 3)) != 0);
            Append((b & (1 << 2)) != 0);
            Append((b & (1 << 1)) != 0);
            Append((b & (1 << 0)) != 0);
        }

        public byte[] GetBytes()
        {
            var bytes = _byteArrayBuilder.GetBytes();
            var outBytes = new ByteArrayBuilder();
            byte currentByte = 0;
            var currentBit = 7;

            for (int i = 0; i < _byteArrayBuilder.Length; i++)
            {
                currentByte |= (byte)(bytes[i] << currentBit);
                currentBit--;
                if (currentBit < 0)
                {
                    outBytes.Append(currentByte);
                    currentByte = 0;
                    currentBit = 7;
                }
            }

            if (currentBit < 7)
            {
                outBytes.Append(currentByte);
            }

            return outBytes.GetBytes();
        }
    }
}