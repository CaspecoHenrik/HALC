namespace HALC
{
    class ProbabilityDecompressor
    {
        private byte[] _data;

        public ProbabilityDecompressor(byte[] data)
        {
            _data = data;
        }

        public byte[] Decompress()
        {
            return _data;
        }
    }
}
