using System;

namespace HALC
{
    public class RedundancyCompressor
    {
        private int _pointer = 0;
        private byte[] _uncompressed;
        private ByteArrayBuilder _builder;

        public RedundancyCompressor(byte[] uncompressed)
        {
            _uncompressed = uncompressed;
            _builder = new ByteArrayBuilder();
        }

        private bool HasMoreBytes()
        {
            return BytesLeft() > 0;
        }

        private int BytesLeft()
        {
            return _uncompressed.Length - _pointer;
        }

        private int GetRepeatCount()
        {
            var val = _uncompressed[_pointer];
            var pointer = _pointer + 1;
            while (pointer < _uncompressed.Length && _uncompressed[pointer] == val)
            {
                pointer++;
            }

            return pointer - _pointer;
        }

        public byte[] Compress()
        {
            _builder.Append(HALC.RedundancyMagicSignature);

            var literalBuffer = new ByteArrayBuilder();

            while (HasMoreBytes())
            {
                var repeatCount = GetRepeatCount();

                // try RLE first
                if (repeatCount > 3)
                {
                    if (literalBuffer.Length > 0)
                    {
                        UseLiteral(literalBuffer.GetBytes());
                        literalBuffer = new ByteArrayBuilder();
                    }
                    UseRLE(repeatCount);
                }
                else
                {
                    var previousOccurance = GetPreviousOccurance();
                    if (previousOccurance.BestMatchLength > 5)
                    {
                        if (literalBuffer.Length > 0)
                        {
                            UseLiteral(literalBuffer.GetBytes());
                            literalBuffer = new ByteArrayBuilder();
                        }
                        UseLongPointer(previousOccurance);
                    }
                    else
                    {
                        literalBuffer.Append(_uncompressed[_pointer]);
                        _pointer++;
                    }
                }
            }

            if (literalBuffer.Length > 0)
            {
                UseLiteral(literalBuffer.GetBytes());
            }

            return _builder.GetBytes();
        }

        private int GetMatchLength(int pointer)
        {
            var bytes = 0;
            while ((_pointer + bytes) < _uncompressed.Length && (pointer + bytes) < _pointer && _uncompressed[_pointer + bytes] == _uncompressed[pointer + bytes])
            {
                bytes++;
            }

            return bytes;
        }

        private PreviousOccurance GetPreviousOccurance()
        {
            var offset = 1;
            int bestMatchLength = -1;
            int bestMatchOffset = 0;
            var currentByte = _uncompressed[_pointer];

            while (offset <= HALC.MaxLongPointerOffset && (_pointer - offset) > 0)
            {
                if (currentByte == _uncompressed[_pointer - offset])
                {
                    var matchLength = GetMatchLength(_pointer - offset);
                    if (matchLength > bestMatchLength)
                    {
                        bestMatchLength = matchLength;
                        bestMatchOffset = offset;
                    }
                }
                offset += 1;
            }

            return new PreviousOccurance(bestMatchOffset, bestMatchLength);
        }

        private void UseLongPointer(PreviousOccurance previousOccurance)
        {
            //Debug.WriteLine("RedundancyCompressor: Writing LongPointer. Offset={0}, Length={1}", previousOccurance.BestMatchOffset, previousOccurance.BestMatchLength);

            byte commandByte = (byte) ((byte) HALC.Command.LongPointer | (previousOccurance.BestMatchOffset >> 20));
            byte offsetByte1 = (byte) (previousOccurance.BestMatchOffset >> 12);
            byte offsetByte2 = (byte) (previousOccurance.BestMatchOffset >> 4);
            byte offsetLengthByte = (byte) ((byte) (previousOccurance.BestMatchOffset << 4) |
                                            (previousOccurance.BestMatchLength >> 8));
            byte LengthByte = (byte) previousOccurance.BestMatchLength;

            _builder.Append(commandByte);
            _builder.Append(offsetByte1);
            _builder.Append(offsetByte2);
            _builder.Append(offsetLengthByte);
            _builder.Append(LengthByte);

            _pointer += previousOccurance.BestMatchLength;
        }

        private void UseLiteral(byte[] literalBytes)
        {
            //Debug.WriteLine("RedundancyCompressor: Writing Literal. Length={0}", literalBytes.Length);

            var written = 0;

            while (written < literalBytes.Length)
            {
                int count = Math.Min(literalBytes.Length - written, HALC.MaxLiteralLength);
                byte commandByte = (byte) ((byte) HALC.Command.Literal | (count >> 8));
                byte lengthByte = (byte) (count & 0xFF);
                
                _builder.Append(commandByte);
                _builder.Append(lengthByte);
                _builder.Append(literalBytes, written, count);

                written += count;
            }

            // don't advance pointer since we already have
        }

        private void UseRLE(int repeatCount)
        {
            //Debug.WriteLine("RedundancyCompressor: Writing RLE. Length={0}, Byte={1}", repeatCount, _uncompressed[_pointer]);

            int count = Math.Min(repeatCount, HALC.MaxRLELength);
            byte commandByte = (byte) ((byte)HALC.Command.RLE | (count >> 8));
            byte lengthByte = (byte) (count & 0xFF);
            byte value = _uncompressed[_pointer];

            _builder.Append(commandByte);
            _builder.Append(lengthByte);
            _builder.Append(value);

            _pointer += count;
        }
    }

    internal class PreviousOccurance
    {
        public int BestMatchOffset;
        public int BestMatchLength;

        public PreviousOccurance(int bestMatchOffset, int bestMatchLength)
        {
            BestMatchOffset = bestMatchOffset;
            BestMatchLength = bestMatchLength;
        }
    }
}
