using System.Collections.Generic;

namespace BytePairEncoding
{
    public class Encoder
    {
        public BPE _bpe;
        private readonly EncodedWordProcessor _wordprocessor;

        public Encoder(BPE bpe)
        {
            _bpe = bpe;
            _wordprocessor = new EncodedWordProcessor(_bpe);
        }


// we need a new way to encode that allows us to use the tokens we have that are "thetoken<SPACE> or <SPACE>thetoken" we need to allow the space to be part of the token - currently we cant get a matching token for those as they are never encoded because of the way we split the words then add the space special token - we just need to go through the text including the spaces, creating tokens along the way....

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
