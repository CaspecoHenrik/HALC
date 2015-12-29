using System;
using System.IO;
using System.Text;

namespace HALC
{
    class ByteArrayBuilder
    {
        private MemoryStream _memoryStream;
        private BinaryWriter _streamWriter;

        public ByteArrayBuilder()
        {
            _memoryStream = new MemoryStream();
            _streamWriter = new BinaryWriter(_memoryStream);
        }

        public long Length
        {
            get { return _memoryStream.Length; }
        }

        public void Append(byte data)
        {
            _streamWriter.Write(data);
        }

        public void Append(UInt16 data)
        {
            _streamWriter.Write(data);
        }

        public void Append(string data)
        {
            _streamWriter.Write(Encoding.ASCII.GetBytes(data));
        }

        public void Append(byte[] data, int index, int count)
        {
            _streamWriter.Write(data, index, count);
        }

        public void Append(byte[] data)
        {
            _streamWriter.Write(data);
        }

        public byte[] GetBytes()
        {
            return _memoryStream.ToArray();
        }
    }
}
