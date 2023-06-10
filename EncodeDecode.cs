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
    public class EncodeDecode
    {
        public BPE bpe;
        public List<KeyValuePair<string, int>> token2id;
        public OrderedDictionary vocab;
        public OrderedDictionary mergePairs;
        public int tokenCount;
        public EncodeDecode(BPE bpe)
        {
            this.bpe = bpe;
            this.token2id = bpe.token2id;
            this.vocab = bpe.vocab;
            this.mergePairs = bpe.mergePairs;
            this.tokenCount = bpe.tokenCount;
        }


        public int[] Encode(string text)
        {
            string[] words = text.Split(' ');

            List<int> encodedTokens = new List<int>();

            foreach (var word in words)
            {
                List<string> wordList = word.Select(ch => ch.ToString()).ToList();

                for (int i = 0; i < wordList.Count; i++)
                {
                    string bestToken = wordList[i];
                    int bestLength = 1;

                    foreach (DictionaryEntry pair in mergePairs)
                    {
                        string key = pair.Key.ToString();
                        if (key.Length > bestLength && i + key.Length <= wordList.Count)
                        {
                            string substring = string.Join("", wordList.GetRange(i, key.Length));
                            if (substring == key)
                            {
                                bestLength = key.Length;
                                bestToken = pair.Value.ToString();
                            }
                        }
                    }

                    if (bestToken == "\n")
                    {
                        bestToken = "<NEWLINE>";
                    }

                    if (!token2id.Any(kv => kv.Key == bestToken))
                    {
                        token2id.Add(new KeyValuePair<string, int>(bestToken, tokenCount));
                        tokenCount++;
                    }

                    encodedTokens.Add(token2id.Single(kv => kv.Key == bestToken).Value);
                    i += bestLength - 1;
                }

                if (token2id.Any(kv => kv.Key == "<SPACE>"))
                {
                    encodedTokens.Add(token2id.Single(kv => kv.Key == "<SPACE>").Value);
                }
            }

            if (encodedTokens.Count > 0 && token2id.Any(kv => kv.Key == "<SPACE>") && encodedTokens[^1] == token2id.Single(kv => kv.Key == "<SPACE>").Value)
            {
                encodedTokens.RemoveAt(encodedTokens.Count - 1);
            }

            return encodedTokens.ToArray();
        }
        public string Decode(int[] ids)
        {
            List<string> tokens = new List<string>();

            foreach (int id in ids)
            {
                bool foundToken = false;
                foreach (var kv in token2id)
                {
                    if (kv.Value == id)
                    {
                        tokens.Add(kv.Key);
                        foundToken = true;
                        break;
                    }
                }

                if (!foundToken)
                {
                    tokens.Add("<UNK>");
                }
            }

            int i = 0;
            while (i < tokens.Count)
            {
                if (tokens[i] == "<SPACE>")
                {
                    tokens[i] = " ";
                    i++;
                }
                else if (tokens[i] == "<UNK>")
                {
                    tokens[i] = "";  // Replace "<UNK>" with newline character
                    i++;
                }
                else if (tokens[i] == "<NEWLINE>")
                {
                    tokens[i] = "\n";  // Replace "<NEWLINE>" with newline character
                    i++;
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

            tokens.RemoveAll(token => token == "<PAD>");
            return string.Join("", tokens);
        }


        public int[] TokeniseAndCreateBins(string fileName, double trainRatio = 0.9)
        {
            string text = File.ReadAllText(fileName);

            int[] encodedWords = Encode(text);

            int trainChunkSize = 2048;
            int valChunkSize = 2048;

            int splitIndex = (int)(encodedWords.Length * trainRatio);

            int[] trainWords = encodedWords.Take(splitIndex).ToArray();
            int[] valWords = encodedWords.Skip(splitIndex).ToArray();

            int trainNumTokens = (int)Math.Ceiling(trainWords.Length / (double)trainChunkSize) * trainChunkSize;
            int valNumTokens = (int)Math.Ceiling(valWords.Length / (double)valChunkSize) * valChunkSize;

            int[] trainIds = AdjustTokensToChunkSize(trainWords, trainChunkSize, trainNumTokens);
            int[] valIds = AdjustTokensToChunkSize(valWords, valChunkSize, valNumTokens);

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite("train.bin")))
            {
                foreach (int id in trainIds)
                {
                    writer.Write(id);
                }
            }

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite("val.bin")))
            {
                foreach (int id in valIds)
                {
                    writer.Write(id);
                }
            }

            return valIds;
        }

        private int[] AdjustTokensToChunkSize(int[] tokens, int chunkSize, int numTokens)
        {
            if (tokens.Length < numTokens)
            {
                int[] adjustedTokens = new int[numTokens];
                Array.Copy(tokens, adjustedTokens, tokens.Length);

                int paddingToken = token2id.Single(kv => kv.Key == "<PAD>").Value;
                for (int i = tokens.Length; i < numTokens; i++)
                {
                    adjustedTokens[i] = paddingToken;
                }

                return adjustedTokens;
            }
            else if (tokens.Length > numTokens)
            {
                int[] adjustedTokens = new int[numTokens];
                Array.Copy(tokens, adjustedTokens, numTokens);
                return adjustedTokens;
            }
            else
            {
                return tokens;
            }
        }
    }
}
