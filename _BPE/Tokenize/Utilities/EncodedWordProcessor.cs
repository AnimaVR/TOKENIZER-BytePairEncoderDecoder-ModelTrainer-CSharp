using System.Collections.Generic;
using System.Linq;

namespace BytePairEncoding
{

    public class EncodedWordProcessor
    {
        private readonly BPE _bpe;

        public EncodedWordProcessor(BPE bpe)
        {
            _bpe = bpe;
        }


        public List<int> ProcessWord(string word)
        {
            List<string> wordList = word.Select(ch => ch.ToString()).ToList();
            List<int> encodedTokens = new();

            string wordJoined = string.Join("", wordList);

            for (int i = 0; i < wordList.Count; i++)
            {
                var (bestToken, bestLength) = _bpe.juggler.FindBestToken(wordJoined, i);

                TokenJuggler.HandleSpecialTokens(ref bestToken);
                _bpe.juggler.ReplaceUnknownToken(bestToken);

                int tokenValue = _bpe.token2id.FirstOrDefault(kv => kv.Key == bestToken).Value;
                encodedTokens.Add(tokenValue);

                i += bestLength - 1;
            }

            return encodedTokens;
        }

        public void AddSpaceToken(List<int> encodedTokens)
        {
            var spaceTokenValue = _bpe.token2id.FirstOrDefault(kv => kv.Key == "<SPACE>").Value;
            if (spaceTokenValue != default)
            {
                encodedTokens.Add(spaceTokenValue);
            }
        }

        public void RemoveTrailingSpace(List<int> encodedTokens)
        {
            var spaceTokenValue = _bpe.token2id.FirstOrDefault(kv => kv.Key == "<SPACE>").Value;
            if (encodedTokens.Count > 0 && encodedTokens[^1] == spaceTokenValue)
            {
                encodedTokens.RemoveAt(encodedTokens.Count - 1);
            }
        }


        
    }
}

