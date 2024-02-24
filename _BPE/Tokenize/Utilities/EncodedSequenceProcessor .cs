using System.Collections.Generic;
using System.Linq;

namespace BytePairEncoding
{

    public class EncodedSequenceProcessor
    {
        private readonly BPE _bpe;

        public EncodedSequenceProcessor(BPE bpe)
        {
            _bpe = bpe;
        }
        public List<int> ProcessSequence(string sequence)
        {
            sequence = sequence.Replace("\r\n", "\n");

            List<int> encodedTokens = new();
            int i = 0;

            while (i < sequence.Length)
            {
                var (bestToken, bestLength) = _bpe.juggler.FindBestToken(sequence, i);

                TokenJuggler.HandleSpecialTokens(ref bestToken);

                var tokenEntry = _bpe.token2id.FirstOrDefault(kv => kv.Key == bestToken);

                if (!string.IsNullOrEmpty(tokenEntry.Key))
                {
                    encodedTokens.Add(tokenEntry.Value);
                }
                else
                {
                    var unkTokenId = _bpe.token2id.FirstOrDefault(kv => kv.Key == "<UNK>").Value;
                    encodedTokens.Add(unkTokenId);
                }

                i += bestLength;
            }

            return encodedTokens;
        }


       
    }
}

