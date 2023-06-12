using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class Decoder
    {
        public BPE bpe;
        public List<KeyValuePair<string, int>> token2id;
        public OrderedDictionary vocab;
        public OrderedDictionary mergePairs;
        public int tokenCount;
        public Decoder(BPE bpe)
        {
            this.bpe = bpe;
            this.token2id = bpe.token2id;
            this.vocab = bpe.vocab;
            this.mergePairs = bpe.mergePairs;
            this.tokenCount = bpe.tokenCount;
        }
      
        public string Decode(int[] ids)
        {
            List<string> tokens = ConvertIdsToTokens(ids);
            tokens = ExpandTokens(tokens);
            tokens.RemoveAll(token => token == "<PAD>");
            return string.Join("", tokens);
        }

        private List<string> ConvertIdsToTokens(int[] ids)
        {
            List<string> tokens = new List<string>();

            foreach (int id in ids)
            {
                var tokenKV = token2id.FirstOrDefault(kv => kv.Value == id);
                tokens.Add(tokenKV.Key ?? "<UNK>");
            }

            return tokens;
        }

        private List<string> ExpandTokens(List<string> tokens)
        {
            int i = 0;
            while (i < tokens.Count)
            {
                if (tokens[i] == "<SPACE>")
                {
                    tokens[i] = " ";
                }
                else if (tokens[i] == "<UNK>")
                {
                    tokens[i] = "";  
                }
                else if (tokens[i] == "<NEWLINE>")
                {
                    tokens[i] = "\n";  
                }
                else
                {
                    bool foundPair = false;
                    foreach (DictionaryEntry de in mergePairs)
                    {
                        if ((string)de.Value == tokens[i])
                        {
                            string originalPair = (string)de.Key;
                            tokens[i] = originalPair[0].ToString();
                            tokens.Insert(i + 1, originalPair.Substring(1));
                            i += 2;
                            foundPair = true;
                            break;
                        }
                    }

                    if (!foundPair)
                    {
                        i++;
                    }
                }
            }

            return tokens;
        }
    }
}
