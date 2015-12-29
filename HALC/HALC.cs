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
            ShortPointer = 0x80,    // CCOO OOOO, OOOO OOLL, LLLL LLLL => Offset 12 bits, Length 10 bits
            LongPointer = 0xC0      // CCOO OOOO, OOOO OOOO, OOOO OOOO, OOOO LLLL, LLLL LLLL => Offset 26 bits, Length 12 bits
        }

        public const int LiteralCommandLength = 2;
        public const int RLECommandLength = 3;
        public const int ShortPointerCommandLength = 3;
        public const int LongPointerCommandLength = 5;

        public const int MaxRLELength = 16383;
        public const int MaxLiteralLength = 16383;
        public const int MaxShortPointerOffset = 4095;
        public const int MaxShortPointerLength = 1023;
        public const int MaxLongPointerOffset = 67108863;
        public const int MaxLongPointerLength = 4095;

        public static byte[] Compress(byte[] uncompressed)
        {
            var redundancyCompressor = new RedundancyCompressor(uncompressed);
            var data = redundancyCompressor.Compress();
            //var probabilityCompressor = new ProbabilityCompressor(data);
            //return probabilityCompressor.Compress();
            return data;
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
