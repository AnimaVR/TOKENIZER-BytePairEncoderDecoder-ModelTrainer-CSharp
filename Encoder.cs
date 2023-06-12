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
    public class Encoder
    {
        public BPE bpe;
        public List<KeyValuePair<string, int>> token2id;
        public OrderedDictionary vocab;
        public OrderedDictionary mergePairs;
        public int tokenCount;

        public Encoder(BPE bpe)
        {
            this.bpe = bpe;
            this.token2id = bpe.token2id;
            this.vocab = bpe.vocab;
            this.mergePairs = bpe.mergePairs;
            this.tokenCount = bpe.tokenCount;
        }

        public async Task<int[]> EncodeAsync(string text)
        {
            string[] words = text.Split(' ');

            List<int> encodedTokens = new List<int>();

            foreach (var word in words)
            {
                List<int> wordTokens = await Task.Run(() => ProcessWord(word));
                encodedTokens.AddRange(wordTokens);
                AddSpaceToken(encodedTokens);
            }

            RemoveTrailingSpace(encodedTokens);

            return encodedTokens.ToArray();
        }


        private List<int> ProcessWord(string word)
        {
            List<string> wordList = word.Select(ch => ch.ToString()).ToList();
            List<int> encodedTokens = new List<int>();

            string wordJoined = string.Join("", wordList);

            for (int i = 0; i < wordList.Count; i++)
            {
                var (bestToken, bestLength) = FindBestToken(wordJoined, i);
                HandleSpecialTokens(ref bestToken);
                UpdateTokenToIdIfNeeded(bestToken);
                encodedTokens.Add(token2id.Single(kv => kv.Key == bestToken).Value);
                i += bestLength - 1;
            }

            return encodedTokens;
        }

        private (string, int) FindBestToken(string wordJoined, int startIdx)
        {
            string bestToken = wordJoined[startIdx].ToString();
            int bestLength = 1;

            foreach (DictionaryEntry pair in mergePairs)
            {
                string key = pair.Key.ToString();
                if (key.Length > bestLength && startIdx + key.Length <= wordJoined.Length)
                {
                    string substring = wordJoined.Substring(startIdx, key.Length);
                    if (substring == key)
                    {
                        bestLength = key.Length;
                        bestToken = pair.Value.ToString();
                    }
                }
            }

            return (bestToken, bestLength);
        }

        private void HandleSpecialTokens(ref string token)
        {
            if (token == "\n")
            {
                token = "<NEWLINE>";
            }
        }

        private void UpdateTokenToIdIfNeeded(string token)
        {
            if (!token2id.Any(kv => kv.Key == token))
            {
                token2id.Add(new KeyValuePair<string, int>(token, tokenCount));
                tokenCount++;
            }
        }

        private void AddSpaceToken(List<int> encodedTokens)
        {
            if (token2id.Any(kv => kv.Key == "<SPACE>"))
            {
                encodedTokens.Add(token2id.Single(kv => kv.Key == "<SPACE>").Value);
            }
        }

        private void RemoveTrailingSpace(List<int> encodedTokens)
        {
            if (encodedTokens.Count > 0 && token2id.Any(kv => kv.Key == "<SPACE>") && encodedTokens[^1] == token2id.Single(kv => kv.Key == "<SPACE>").Value)
            {
                encodedTokens.RemoveAt(encodedTokens.Count - 1);
            }
        }
    }
}
