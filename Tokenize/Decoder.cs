using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BytePairEncoding
{
    public class Decoder
    {
        private readonly BPE _bpe;

        public Decoder(BPE bpe)
        {
            _bpe = bpe;
        }

        public string Decode(int[] ids)
        {
            var tokens = ConvertIdsToTokens(ids);
            tokens = ExpandTokens(tokens);
            tokens.RemoveAll(token => token == "<PAD>");
            return string.Join("", tokens);
        }

        private List<string> ConvertIdsToTokens(int[] ids)
        {
            return ids.Select(id => _bpe.token2id.FirstOrDefault(kv => kv.Value == id).Key ?? "<UNK>").ToList();
        }

        private List<string> ExpandTokens(List<string> tokens)
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
                        tokens[i] = "\n";
                        i++;
                        break;
                    default:
                        i = TryExpandToken(tokens, i);
                        break;
                }
            }

            return tokens;
        }

        private int TryExpandToken(List<string> tokens, int index)
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
