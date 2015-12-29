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
            var nodes = histogram.Select(kvp => new Frequency(kvp.Key, kvp.Value)).OrderBy(f => f.Count).ToList();
            var indexedNodes = nodes.ToDictionary(f => f.B, f => f);
            while (nodes.Count() > 1)
            {
                var left = nodes[0];
                var right = nodes[1];
                var newNode = new Frequency(left, right);
                nodes.Remove(left);
                nodes.Remove(right);
                nodes.Add(newNode);
                nodes = nodes.OrderBy(f => f.Count).ToList();
            }
            
            // output tree
            var bitBuilder = new BitArrayBuilder();
            nodes[0].OutputAndBuildPaths(bitBuilder, new List<bool>());
            _builder.Append(bitBuilder.GetBytes());

            // output huffman codes for each byte
            bitBuilder = new BitArrayBuilder();
            foreach (var b in _data)
            {
                var node = indexedNodes[b];
                foreach (var p in node.Path)
                {
                    bitBuilder.Append(p);
                }
            }
            _builder.Append(bitBuilder.GetBytes());

            return _builder.GetBytes();
        }

        private Dictionary<byte, int> GetHistogram()
        {
            var histogram = new Dictionary<byte, int>();
            foreach (var b in _data)
            {
                if (!histogram.ContainsKey(b))
                {
                    histogram.Add(b, 0);
                }
                
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
        public List<bool> Path; 

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

        public void OutputAndBuildPaths(BitArrayBuilder bitBuilder, List<bool> path)
        {
            if (!IsLeaf)
            {
                bitBuilder.Append(false);
                var leftPath = new List<bool>(path);
                leftPath.Add(false);
                Left.OutputAndBuildPaths(bitBuilder, leftPath);
                var rightPath = new List<bool>(path);
                rightPath.Add(true);
                Right.OutputAndBuildPaths(bitBuilder, rightPath);
            }
            else
            {
                Path = path;
                bitBuilder.Append(true);
                bitBuilder.Append(B);
            }
        }
    }
}
