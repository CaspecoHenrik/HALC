using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HALC
{
    class ProbabilityCompressor
    {
        private byte[] _data;
        private ByteArrayBuilder _builder;

        public ProbabilityCompressor(byte[] data)
        {
            _data = data;
            _builder = new ByteArrayBuilder();
        }

        public byte[] Compress()
        {
            _builder.Append(HALC.ProbabilityMagicSignature);

            // build huffman tree
            var histogram = GetHistogram();
            var nodes = histogram.Select(kvp => new Frequency(kvp.Key, kvp.Value)).OrderByDescending(f => f.Count).ToList();
            while (nodes.Count() > 1)
            {
                var left = nodes[0];
                var right = nodes[1];
                var newNode = new Frequency(left, right);
                nodes.Remove(left);
                nodes.Remove(right);
                nodes.Add(newNode);
                nodes = nodes.OrderByDescending(f => f.Count).ToList();
            }
            
            // output tree
            var bitBuilder = new BitArrayBuilder();
            nodes[0].Output(bitBuilder);
            _builder.Append(bitBuilder.GetBytes());

            // output huffman codes for each byte
            bitBuilder = new BitArrayBuilder();
            foreach (var b in _data)
            {
                
            }

            return _builder.GetBytes();
        }

        private Dictionary<byte, int> GetHistogram()
        {
            var histogram = new Dictionary<byte, int>();
            foreach (var b in _data)
            {
                histogram[b]++;
            }

            return histogram;
        }
    }

    internal class Frequency
    {
        public byte B;
        public int Count;
        public bool IsLeaf = false;
        public Frequency Left;
        public Frequency Right;

        public Frequency(Frequency left, Frequency right)
        {
            Count = left.Count + right.Count;
            Left = left;
            Right = right;
        }

        public Frequency(byte b, int count)
        {
            B = b;
            Count = count;
            IsLeaf = true;
        }

        public void Output(BitArrayBuilder bitBuilder)
        {
            if (!IsLeaf)
            {
                bitBuilder.Append(false);
                Left.Output(bitBuilder);
                Right.Output(bitBuilder);
            }
            else
            {
                bitBuilder.Append(true);
                bitBuilder.Append(B);
            }
        }
    }
}
