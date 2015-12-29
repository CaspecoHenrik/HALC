namespace HALC
{
    public class HALC
    {
        public const string RedundancyMagicSignature = "RC";
        public const string ProbabilityMagicSignature = "PC";

        public const int CommandMask = 0xC0;
        public enum Command : byte
        {
            Literal = 0x00,         // CCLL LLLL, LLLL LLLL => Length 14 bits
            RLE = 0x40,             // CCLL LLLL, LLLL LLLL, BBBB BBBB => Length 14 bits
            Unused = 0x80,
            LongPointer = 0xC0      // CCOO OOOO, OOOO OOOO, OOOO OOOO, OOOO LLLL, LLLL LLLL => Offset 26 bits, Length 12 bits
        }

        public const int MaxRLELength = 16383;
        public const int MaxLiteralLength = 16383;
        public const int MaxLongPointerOffset = 67108863;
        public const int MaxLongPointerLength = 4095;

        public static byte[] Compress(byte[] uncompressed)
        {
            var redundancyCompressor = new RedundancyCompressor(uncompressed);
            var data = redundancyCompressor.Compress();
            var probabilityCompressor = new ProbabilityCompressor(data);
            return probabilityCompressor.Compress();
        }

        public static byte[] Decompress(byte[] compressed)
        {
            var redundancyDecompressor = new RedundancyDecompressor(compressed);
            var data = redundancyDecompressor.Decompress();
            var probabilityDecompressor = new ProbabilityDecompressor(data);
            return probabilityDecompressor.Decompress();
        }
    }
}
