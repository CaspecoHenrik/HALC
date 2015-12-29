namespace PerformanceTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = new HALCTests.CompressAndDecompressTests();
            t.CompressTestFiles_ReturnsSmallerArray();
        }
    }
}
