using System.Collections.Generic;

namespace BytePairEncoding
{
    public class Encoder
    {
        public BPE _bpe;

        EncodedSequenceProcessor _sequenceprocessor;

        // private readonly EncodedWordProcessor _wordprocessor;

        public Encoder(BPE bpe)
        {
            _bpe = bpe;

            _sequenceprocessor = new EncodedSequenceProcessor(_bpe);

            //     _wordprocessor = new EncodedWordProcessor(_bpe);
        }


        // this is a proper char level encoder that uses spaces properly.

        public int[] Encode(string text)
        {
            List<int> encodedTokens = _sequenceprocessor.ProcessSequence(text);
            return encodedTokens.ToArray();
        }


        // this encoder uses spaces as NOT part of the token (<SPACE> is its own special thing)

        /*
        public int[] Encode(string text)
        {
            string[] words = text.Split(' ');

            List<int> encodedTokens = new ();

            foreach (var word in words)
            {
                List<int> wordTokens = _wordprocessor.ProcessWord(word);

                encodedTokens.AddRange(wordTokens);

                 _bpe.juggler.AddSpaceToken(encodedTokens);
            }

            _bpe.juggler.RemoveTrailingSpace(encodedTokens);

            return encodedTokens.ToArray();
        } 
        */
    }
}
