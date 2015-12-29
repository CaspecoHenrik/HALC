namespace HALC
{
    public class RedundancyDecompressor
    {
        private int _pointer = 0;
        private byte[] _compressed;
        private ByteArrayBuilder _builder;

        public RedundancyDecompressor(byte[] compressed)
        {
            _compressed = compressed;
            _builder = new ByteArrayBuilder();
        }

        private bool HasMoreBytes()
        {
            return BytesLeft() > 0;
        }

        private int BytesLeft()
        {
            return _compressed.Length - _pointer;
        }

        public byte[] Decompress()
        {
            if (_compressed[0] != HALC.RedundancyMagicSignature[0] || _compressed[1] != HALC.RedundancyMagicSignature[1])
            {
                throw new InvalidImageException("Missing signature");
            }
            _pointer += 2;

            while (HasMoreBytes())
            {
                switch (GetCommand())
                {
                    case HALC.Command.Literal:
                        UseLiteral();
                        break;

                    case HALC.Command.RLE:
                        UseRLE();
                        break;

                    case HALC.Command.Unused:
                        throw new InvalidImageException("Unknown command");
                        break;

                    case HALC.Command.LongPointer:
                        UseLongPointer();
                        break;
                }
            }

            return _builder.GetBytes();
        }

        private void UseLongPointer()
        {
            if (BytesLeft() < 5)
            {
                throw new InvalidImageException("Truncated LongPointer command");
            }

            int offset = _compressed[_pointer] & (HALC.CommandMask ^ 0xFF);
            offset <<= 8;
            offset |= _compressed[_pointer + 1];
            offset <<= 8;
            offset |= _compressed[_pointer + 2];
            offset <<= 4;
            offset |= (byte) (_compressed[_pointer + 3] >> 4);

            int length = _compressed[_pointer + 3] & 0x0F;
            length <<= 8;
            length |= _compressed[_pointer + 4];

            var uncompressed = _builder.GetBytes();
            _builder.Append(uncompressed, uncompressed.Length - offset, length);

            _pointer += 5;

            //Debug.WriteLine("RedundancyDecompressor: Used LongPointer. Offset={0}, Length={1}", offset, length);
        }

        private void UseLiteral()
        {
            if (BytesLeft() < 2)
            {
                throw new InvalidImageException("Truncated Literal command");
            }

            int count = _compressed[_pointer] & (HALC.CommandMask ^ 0xFF);
            count <<= 8;
            count |= _compressed[_pointer + 1];

            if (BytesLeft() < (2 + count))
            {
                throw new InvalidImageException("Truncated Literal data");
            }

            _builder.Append(_compressed, _pointer + 2, count);

            _pointer += 2 + count;

            //Debug.WriteLine("RedundancyDecompressor: Used Literal. Length={0}", count);
        }

        private void UseRLE()
        {
            if (BytesLeft() < 3)
            {
                throw new InvalidImageException("Truncated RLE command");
            }

            int count = _compressed[_pointer] & (HALC.CommandMask ^ 0xFF);
            count <<= 8;
            count |= _compressed[_pointer + 1];
            byte value = _compressed[_pointer + 2];

            var data = new byte[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = value;
            }
            _builder.Append(data);

            _pointer += 3;

            //Debug.WriteLine("RedundancyDecompressor: Used RLE. Length={0}, Byte={1}", count, value);
        }

        private HALC.Command GetCommand()
        {
            return (HALC.Command) (_compressed[_pointer] & HALC.CommandMask);
        }
    }
}
