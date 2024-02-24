using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace BytePairEncoding
{
    public class TokenJuggler
    {
        readonly BPE _bpe;
        
        public TokenJuggler(BPE bpe)
        {
            _bpe = bpe;
        }

        public (string, int) FindBestToken(string wordJoined, int startIdx)
        {
            string bestToken = wordJoined[startIdx].ToString();
            int bestLength = 1;

            foreach (DictionaryEntry pair in _bpe.mergePairs)
            {

                var key = pair.Key as string;
                if (key == null) continue;

                if (key.Length > bestLength && startIdx + key.Length <= wordJoined.Length)
                {
                    string substring = wordJoined.Substring(startIdx, key.Length);
                    if (substring == key)
                    {
                        bestLength = key.Length;
                        bestToken = pair.Value as string ?? "";
                    }
                }
            }

            return (bestToken, bestLength);
        }

        public static void HandleSpecialTokens(ref string token)
        {
            if (token == "\n")
            {
                token = "<NEWLINE>";
            }
        }

        public void ReplaceUnknownToken(string token)
        {
            if (!_bpe.token2id.Any(kv => kv.Key == token))
            {
                var unkTokenValue = _bpe.token2id.FirstOrDefault(kv => kv.Key == "<UNK>").Value;

                _bpe.token2id.Add(new KeyValuePair<string, int>(token, unkTokenValue));
            }
        }
    }
}
