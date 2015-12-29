using System;
using NUnit.Framework;

namespace HALCTests
{
    [TestFixture]
    class DecompressorTests
    {
        [Test]
        public void TestSimpleRLE_ReturnsCorrect()
        {
            var compressed = new byte[] {(byte)'R', (byte)'C', (byte) HALC.HALC.Command.RLE | (HALC.HALC.CommandMask ^ 0xFF), 0xFF, 42};
            var uncompressed = HALC.HALC.Decompress(compressed);

            Assert.AreEqual(HALC.HALC.MaxRLELength, uncompressed.Length);
            Assert.AreEqual(42, uncompressed[0]);
            Assert.AreEqual(42, uncompressed[HALC.HALC.MaxRLELength - 1]);
        }

        [Test]
        public void TestSimpleLiteral_ReturnsCorrect()
        {
            var compressed = new byte[] {(byte) 'R', (byte) 'C', (byte) HALC.HALC.Command.Literal, 4, 1, 2, 3, 4};
            var uncompressed = HALC.HALC.Decompress(compressed);

            Assert.AreEqual(4, uncompressed.Length);
            Assert.AreEqual(new Byte[] {1, 2, 3, 4}, uncompressed);
        }

        [Test]
        public void TestLiteralAndLongPointer_ReturnsCorrect()
        {
            var compressed = new byte[] { (byte)'R', (byte)'C', (byte)HALC.HALC.Command.Literal, 4, 1, 2, 3, 4, (byte)HALC.HALC.Command.LongPointer, 0, 0, 64, 4};
            var uncompressed = HALC.HALC.Decompress(compressed);

            Assert.AreEqual(8, uncompressed.Length);
            Assert.AreEqual(new Byte[] { 1, 2, 3, 4, 1, 2, 3, 4 }, uncompressed);
        }
    }
}
