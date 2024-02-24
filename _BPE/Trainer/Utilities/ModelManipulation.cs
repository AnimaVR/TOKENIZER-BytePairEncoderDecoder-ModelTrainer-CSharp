using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class ModelManipulation
    {
        private readonly BPE _bpe;

        public ModelManipulation(BPE bpe)
        {
            _bpe = bpe;
        }

        public void UpdateMainVocabulary(Dictionary<string, int> tempVocab)
        {
            foreach (var pair in tempVocab)
            {
                if (_bpe.vocab.Contains(pair.Key))
                {
                    if (_bpe.vocab[pair.Key] is int existingValue)
                    {
                        _bpe.vocab[pair.Key] = existingValue + pair.Value;
                    }
                    else
                    {
                        _bpe.vocab[pair.Key] = pair.Value;
                    }
                }
                else
                {
                    _bpe.vocab[pair.Key] = pair.Value;
                }
            }
        }

        public void RemoveTokensBelowFrequency(int minFrequency)
        {
            List<string> subwordUnitsToRemove = new();
            foreach (DictionaryEntry pair in _bpe.vocab)
            {
                if (pair.Value is int value && value < minFrequency)
                {
                    subwordUnitsToRemove.Add((string)pair.Key);
                }
            }

            foreach (var subwordUnit in subwordUnitsToRemove)
            {
                _bpe.vocab.Remove(subwordUnit);
            }
        }


        public async Task PerformMerges(List<List<string>> words, int numMerges, IProgress<int>? progress)
        {
            for (int i = 0; i < numMerges; i++)
            {
                var mostFreqPair = await CountPairAsync(words);

                if (!mostFreqPair.HasValue) break;

                MergeMostFrequentPair(mostFreqPair, words);
                progress?.Report((i + 1) * 100 / numMerges);
            }
        }



        public static async Task<KeyValuePair<string, int>?> CountPairAsync(List<List<string>> words)
        {
            var globalPairCounts = new ConcurrentDictionary<string, int>();

            await Task.Run(() => Parallel.ForEach(words, wordList =>
            {
                for (int i = 0; i < wordList.Count - 1; i++)
                {
                    string pair = wordList[i] + wordList[i + 1];
                    globalPairCounts.AddOrUpdate(pair, 1, (key, value) => value + 1);
                }
            }));

            if (globalPairCounts.IsEmpty)
            {
                return null;
            }

            var mostFreqPair = globalPairCounts.OrderByDescending(pair => pair.Value).First();
            return new KeyValuePair<string, int>(mostFreqPair.Key, mostFreqPair.Value);
        }


        public void MergeMostFrequentPair(KeyValuePair<string, int>? mostFreqPair, List<List<string>> words)
        {
            if (mostFreqPair == null)
            {
                return;
            }

            string mostFreqPairKey = mostFreqPair.Value.Key;
            string newToken = mostFreqPairKey;

            _bpe.vocab[newToken] = mostFreqPair.Value.Value;
            _bpe.mergePairs[mostFreqPairKey] = newToken;

            foreach (var word in words)
            {
                for (int j = 0; j < word.Count - 1;)
                {
                    if (word[j] + word[j + 1] == mostFreqPairKey)
                    {
                        word[j] = newToken;
                        word.RemoveAt(j + 1);
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }

        public void FinalizeToken2Id()
        {
            foreach (string token in _bpe.vocab.Keys)
            {
                if (!_bpe.token2id.Any(kv => kv.Key == token))
                {
                    _bpe.token2id.Add(new KeyValuePair<string, int>(token, _bpe.tokenCount++));
                }
            }
        }
    }
}
