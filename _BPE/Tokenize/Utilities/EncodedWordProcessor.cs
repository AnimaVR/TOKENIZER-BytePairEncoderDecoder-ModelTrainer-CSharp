using System.Collections;
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
        public List<int> ProcessSequence(string sequence)
        {
            sequence = sequence.Replace("\r\n", "\n");

            List<int> encodedTokens = new();
            int i = 0;

            while (i < sequence.Length)
            {
                var (bestToken, bestLength) = FindBestToken(sequence, i);

                // Handle special tokens before checking token2id
                HandleSpecialTokens(ref bestToken);

                // Search for the bestToken in the token2id list
                var tokenEntry = _bpe.token2id.FirstOrDefault(kv => kv.Key == bestToken);

                if (!string.IsNullOrEmpty(tokenEntry.Key))
                {
                    // Token found, use its ID
                    encodedTokens.Add(tokenEntry.Value);
                }
                else
                {
                    // If not found, consider it as an unknown token
                    var unkTokenId = _bpe.token2id.FirstOrDefault(kv => kv.Key == "<UNK>").Value;
                    encodedTokens.Add(unkTokenId);
                }

                i += bestLength;
            }

            return encodedTokens;
        }



        public List<int> ProcessWord(string word)
        {
            List<string> wordList = word.Select(ch => ch.ToString()).ToList();
            List<int> encodedTokens = new();

            string wordJoined = string.Join("", wordList);

            for (int i = 0; i < wordList.Count; i++)
            {
                var (bestToken, bestLength) = FindBestToken(wordJoined, i);

                HandleSpecialTokens(ref bestToken);
                ReplaceUnknownToken(bestToken);

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


        private (string, int) FindBestToken(string wordJoined, int startIdx)
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


        private void ReplaceUnknownToken(string token)
        {
            if (!_bpe.token2id.Any(kv => kv.Key == token))
            {
                var unkTokenValue = _bpe.token2id.FirstOrDefault(kv => kv.Key == "<UNK>").Value;

                _bpe.token2id.Add(new KeyValuePair<string, int>(token, unkTokenValue));
            }
        }
    }
}

