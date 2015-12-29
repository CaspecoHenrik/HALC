using NUnit.Framework;

namespace HALCTests
{
    [TestFixture]
    public class CompressorTests
    {
        [Test]
        public void CompressEmptyArray_StartsWithMagic()
        {
            var emptyArray = new byte[100];
            var compressed = HALC.HALC.Compress(emptyArray);

            Assert.AreEqual(HALC.HALC.RedundancyMagicSignature[0], compressed[0]);
            Assert.AreEqual(HALC.HALC.RedundancyMagicSignature[1], compressed[1]);
        }

        [Test]
        public void CompressEmptyArray_ReturnsCompressedData()
        {
            var emptyArray = new byte[1024];
            var compressed = HALC.HALC.Compress(emptyArray);

            Assert.NotNull(compressed);
            Assert.Less(compressed.Length, emptyArray.Length);
        }
    }
}
