using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace HALCTests
{
    [TestFixture]
    public class CompressAndDecompressTests
    {
        [Test]
        public void CompressAndDecompressEmptyArray_ResultsInIdenticalArray()
        {
            var emptyArray = new byte[1024];
            var compressed = HALC.HALC.Compress(emptyArray);
            var uncompressed = HALC.HALC.Decompress(compressed);

            Assert.AreEqual(emptyArray.Length, uncompressed.Length);
            Assert.AreEqual(emptyArray, uncompressed);
        }

        [Test]
        public void CompressAndDecompressRLEArray_ResultsInIdenticalArray()
        {
            var rleArray = new byte[] {0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 2};
            var compressed = HALC.HALC.Compress(rleArray);
            var uncompressed = HALC.HALC.Decompress(compressed);

            Assert.AreEqual(rleArray.Length, uncompressed.Length);
            Assert.AreEqual(rleArray, uncompressed);
        }

        [Test]
        public void CompressTestFiles_ReturnsSmallerArray()
        {
            TestFile("funny.jpg");
            //TestFile("test.exe");
            TestFile("text.txt");
        }

        private void TestFile(string filename)
        {
            byte[] data;

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HALCTests.TestFiles." + filename))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                data = reader.ReadBytes((int) stream.Length);
            }

            var compressed = HALC.HALC.Compress(data);

            Debug.WriteLine("Compressed {0} from {1} to {2} ({3}%)", filename, data.Length, compressed.Length, 100 * compressed.Length / data.Length);

            var uncompressed = HALC.HALC.Decompress(compressed);
            Assert.Less(compressed.Length, data.Length);
            Assert.AreEqual(data, uncompressed);
        }
    }
}
