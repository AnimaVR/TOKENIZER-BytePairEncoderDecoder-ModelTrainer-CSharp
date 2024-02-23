using System.Collections.Generic;

namespace BytePairEncoding
{
    public class Encoder
    {
        public BPE _bpe;
        EncodedWordProcessor _wordprocessor;

        public Encoder(BPE bpe)
        {
            _bpe = bpe;
            _wordprocessor = new EncodedWordProcessor(_bpe);
        }

        public int[] Encode(string text)
        {
            string[] words = text.Split(' ');

            List<int> encodedTokens = new ();

            foreach (var word in words)
            {
                List<int> wordTokens = _wordprocessor.ProcessWord(word);
                encodedTokens.AddRange(wordTokens);
                _wordprocessor.AddSpaceToken(encodedTokens);
            }

            _wordprocessor.RemoveTrailingSpace(encodedTokens);

            return encodedTokens.ToArray();
        } 

    }
}
