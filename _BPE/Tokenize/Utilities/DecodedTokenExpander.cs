using System.Collections;
using System.Collections.Generic;

namespace BytePairEncoding
{
    public class DecodedTokenExpander
    {
        private readonly BPE _bpe;
        public DecodedTokenExpander(BPE bpe)
        {
            _bpe = bpe;
        }

        public List<string> ExpandTokens(List<string> tokens)
        {
            for (int i = 0; i < tokens.Count;)
            {
                switch (tokens[i])
                {
                    case "<SPACE>":
                        tokens[i] = " ";
                        i++;
                        break;
                    case "<UNK>":
                        tokens[i] = "";
                        i++;
                        break;
                    case "<NEWLINE>":
                        tokens[i] = "\r\n";
                        i++;
                        break;
                    default:
                        i = TryExpandToken(tokens, i);
                        break;
                }
            }

            return tokens;
        }

        public int TryExpandToken(List<string> tokens, int index)
        {
            foreach (DictionaryEntry mergePair in _bpe.mergePairs)
            {
                if (mergePair.Value != null && (string)mergePair.Value == tokens[index])
                {
                    string originalPair = (string)mergePair.Key;
                    if (!string.IsNullOrEmpty(originalPair))
                    {
                        tokens[index] = originalPair[0].ToString();
                        tokens.Insert(index + 1, originalPair.Substring(1));
                        return index + 2;
                    }
                }
            }
            return index + 1;
        }
    }
}
