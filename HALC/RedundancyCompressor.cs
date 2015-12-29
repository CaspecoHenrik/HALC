using System;
using System.Diagnostics;

namespace HALC
{
    public class RedundancyCompressor
    {
        private int _compressionPointer = 0;
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
            return _uncompressed.Length - _compressionPointer;
        }

        private int GetRepeatCount()
        {
            var val = _uncompressed[_compressionPointer];

            // smart lookahead
            var lookAheadPointer = _compressionPointer + HALC.RLECommandLength - 1;
            if (lookAheadPointer >= _uncompressed.Length || _uncompressed[lookAheadPointer] != val)
            {
                return 0;
            }

            var pointer = _compressionPointer + 1;
            while (pointer < _uncompressed.Length && _uncompressed[pointer] == val)
            {
                pointer++;
            }

            return pointer - _compressionPointer;
        }

        public byte[] Compress()
        {
            _builder.Append(HALC.RedundancyMagicSignature);

            var literalBuffer = new ByteArrayBuilder();

            while (HasMoreBytes())
            {
                var repeatCount = GetRepeatCount();

                // try RLE first
                if (repeatCount > HALC.RLECommandLength)
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
                    if (previousOccurance.BestMatchLength > HALC.ShortPointerCommandLength &&
                        previousOccurance.BestMatchOffset <= HALC.MaxShortPointerOffset &&
                        previousOccurance.BestMatchLength <= HALC.MaxShortPointerLength)
                    {
                        if (literalBuffer.Length > 0)
                        {
                            UseLiteral(literalBuffer.GetBytes());
                            literalBuffer = new ByteArrayBuilder();
                        }
                        UseShortPointer(previousOccurance);
                    }
                    else if (previousOccurance.BestMatchLength > HALC.LongPointerCommandLength)
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
                        literalBuffer.Append(_uncompressed[_compressionPointer]);
                        _compressionPointer++;
                    }
                }
            }

            if (literalBuffer.Length > 0)
            {
                UseLiteral(literalBuffer.GetBytes());
            }

            return _builder.GetBytes();
        }

        private int GetMatchLength(int pointer, byte currentByte, out int searchPointer)
        {
            var bytes = 1;
            searchPointer = pointer + 1;

            while ((_compressionPointer + bytes) < _uncompressed.Length && (pointer + bytes) < _compressionPointer && _uncompressed[_compressionPointer + bytes] == _uncompressed[pointer + bytes])
            {
                if (_uncompressed[pointer + bytes] == currentByte)
                {
                    searchPointer = pointer + bytes;
                }

                bytes++;
            }

            return bytes;
        }

        private PreviousOccurance GetPreviousOccurance()
        {
            int bestMatchLength = -1;
            int bestMatchOffset = 0;
            var currentByte = _uncompressed[_compressionPointer];

            var searchPointer = Math.Max(0, _compressionPointer - HALC.MaxLongPointerOffset);
            while (searchPointer <= (_compressionPointer - HALC.ShortPointerCommandLength))
            {
                if (currentByte == _uncompressed[searchPointer])
                {
                    int newSearchPointer;
                    var matchLength = GetMatchLength(searchPointer, currentByte, out newSearchPointer);
                    if (matchLength > bestMatchLength)
                    {
                        bestMatchLength = matchLength;
                        bestMatchOffset = _compressionPointer - searchPointer;
                    }
                    searchPointer = newSearchPointer;
                }
                else
                {
                    searchPointer++;
                }
            }

            return new PreviousOccurance(bestMatchOffset, bestMatchLength);
        }

        private void UseShortPointer(PreviousOccurance previousOccurance)
        {
            Debug.WriteLine("RedundancyCompressor: Writing ShortPointer @{0}. Offset={1}, Length={2}", _compressionPointer, previousOccurance.BestMatchOffset, previousOccurance.BestMatchLength);
            Debug.Assert(
                _compressionPointer - previousOccurance.BestMatchOffset >= 0 &&
                previousOccurance.BestMatchOffset <= HALC.MaxShortPointerOffset &&
                previousOccurance.BestMatchLength <= HALC.MaxShortPointerLength,
                "Invalid ShortPointer.");

            byte commandByte = (byte)((byte)HALC.Command.ShortPointer | (previousOccurance.BestMatchOffset >> 6));
            byte offsetLengthByte = (byte)((byte)(previousOccurance.BestMatchOffset << 2) |
                                            (previousOccurance.BestMatchLength >> 8));
            byte LengthByte = (byte)previousOccurance.BestMatchLength;

            _builder.Append(commandByte);
            _builder.Append(offsetLengthByte);
            _builder.Append(LengthByte);

            _compressionPointer += previousOccurance.BestMatchLength;
        }

        private void UseLongPointer(PreviousOccurance previousOccurance)
        {
            Debug.WriteLine("RedundancyCompressor: Writing LongPointer @{0}. Offset={1}, Length={2}", _compressionPointer, previousOccurance.BestMatchOffset, previousOccurance.BestMatchLength);
            Debug.Assert(
                _compressionPointer - previousOccurance.BestMatchOffset >= 0 &&
                previousOccurance.BestMatchOffset <= HALC.MaxLongPointerOffset &&
                previousOccurance.BestMatchLength <= HALC.MaxLongPointerLength,
                "Invalid LongPointer.");

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

            _compressionPointer += previousOccurance.BestMatchLength;
        }

        private void UseLiteral(byte[] literalBytes)
        {
            Debug.WriteLine("RedundancyCompressor: Writing Literal @{0}. Length={1}", _compressionPointer, literalBytes.Length);

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
            Debug.WriteLine("RedundancyCompressor: Writing RLE @{0}. Length={1}, Byte={2}", _compressionPointer, repeatCount, _uncompressed[_compressionPointer]);

            int count = Math.Min(repeatCount, HALC.MaxRLELength);
            byte commandByte = (byte) ((byte)HALC.Command.RLE | (count >> 8));
            byte lengthByte = (byte) (count & 0xFF);
            byte value = _uncompressed[_compressionPointer];

            _builder.Append(commandByte);
            _builder.Append(lengthByte);
            _builder.Append(value);

            _compressionPointer += count;
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
