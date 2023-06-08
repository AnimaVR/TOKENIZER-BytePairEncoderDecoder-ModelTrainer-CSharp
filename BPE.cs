using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class BPE
    {
        private List<KeyValuePair<string, int>> token2id = new List<KeyValuePair<string, int>>();
        private OrderedDictionary vocab = new OrderedDictionary();
        private OrderedDictionary mergePairs = new OrderedDictionary();
        private int tokenCount = 1;

        public async Task TrainAsync(string fileName, int numMerges, int minFrequency)
        {
            vocab.Clear();
            mergePairs.Clear();
            token2id.Clear();
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            string text = await File.ReadAllTextAsync(filePath);

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => new List<string>(word.Select(ch => ch.ToString()))).ToList();

            await LoadVocabAsync(words, minFrequency);
            vocab["<UNK>"] = 0;
            token2id.Add(new KeyValuePair<string, int>("<UNK>", 0));
            vocab["<PAD>"] = 0;
            token2id.Add(new KeyValuePair<string, int>("<PAD>", 0));
            tokenCount = 1;

            for (int i = 0; i < numMerges; i++)
            {
                var pairCounts = await CountPairAsync(words);
                if (pairCounts.Count == 0)
                {
                    break;
                }

                MergeMostFrequentPair(pairCounts, words);
            }

            foreach (string token in vocab.Keys)
            {
                if (!token2id.Any(kv => kv.Key == token))
                {
                    token2id.Add(new KeyValuePair<string, int>(token, tokenCount));
                    tokenCount++;
                }
            }
            SaveModel("model.json");
        }

        private async Task LoadVocabAsync(List<List<string>> words, int minFrequency)
        {
            object lockObject = new object();
            await Task.Run(() => Parallel.ForEach(words, word =>
            {
                var localVocab = new OrderedDictionary();

                foreach (var token in word)
                {
                    if (!localVocab.Contains(token))
                    {
                        localVocab[token] = 1;
                    }
                    else
                    {
                        localVocab[token] = (int)localVocab[token] + 1;
                    }
                }
                if (!localVocab.Contains("<SPACE>"))
                {
                    localVocab["<SPACE>"] = 1;
                }
                else
                {
                    localVocab["<SPACE>"] = (int)localVocab["<SPACE>"] + 1;
                }
                lock (lockObject)
                {
                    foreach (DictionaryEntry pair in localVocab)
                    {
                        if (!vocab.Contains(pair.Key))
                        {
                            vocab[pair.Key] = pair.Value;
                        }
                        else
                        {
                            vocab[pair.Key] = (int)vocab[pair.Key] + (int)pair.Value;
                        }
                    }
                }
            }));
            var subwordUnitsToRemove = vocab.Cast<DictionaryEntry>()
                                            .Where(pair => (int)pair.Value < minFrequency)
                                            .Select(pair => pair.Key.ToString())
                                            .ToList();
            foreach (var subwordUnit in subwordUnitsToRemove)
            {
                vocab.Remove(subwordUnit);
            }
        }
        private async Task<List<KeyValuePair<string, int>>> CountPairAsync(List<List<string>> words)
        {
            var pairCounts = new List<KeyValuePair<string, int>>();
            object lockObjectPairCounts = new object();

            await Task.Run(() => Parallel.ForEach(words, word =>
            {
                var localPairCounts = new List<KeyValuePair<string, int>>();

                for (int j = 0; j < word.Count - 1; j++)
                {
                    string pair = word[j] + word[j + 1];

                    KeyValuePair<string, int>? existingPair = null;
                    foreach (var kv in localPairCounts)  // Replace LINQ with loop
                    {
                        if (kv.Key == pair)
                        {
                            existingPair = kv;
                            break;
                        }
                    }

                    if (existingPair == null)
                    {
                        localPairCounts.Add(new KeyValuePair<string, int>(pair, 1));
                    }
                    else
                    {
                        localPairCounts[localPairCounts.IndexOf(existingPair.Value)] =
                            new KeyValuePair<string, int>(pair, existingPair.Value.Value + 1);
                    }
                }

                lock (lockObjectPairCounts)
                {
                    foreach (var pair in localPairCounts)
                    {
                        KeyValuePair<string, int>? existingPair = null;
                        foreach (var kv in pairCounts)  // Replace LINQ with loop
                        {
                            if (kv.Key == pair.Key)
                            {
                                existingPair = kv;
                                break;
                            }
                        }

                        if (existingPair == null)
                        {
                            pairCounts.Add(new KeyValuePair<string, int>(pair.Key, pair.Value));
                        }
                        else
                        {
                            pairCounts[pairCounts.IndexOf(existingPair.Value)] =
                                new KeyValuePair<string, int>(pair.Key, existingPair.Value.Value + pair.Value);
                        }
                    }
                }
            }));

            return pairCounts;
        }

        private void MergeMostFrequentPair(List<KeyValuePair<string, int>> pairCounts, List<List<string>> words)
        {
            var mostFreqPair = pairCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            string newToken = mostFreqPair;  // Note: No need to use string.Join("", mostFreqPair)

            lock (vocab)
            {
                vocab[newToken] = pairCounts.First(p => p.Key == mostFreqPair).Value;
            }

            lock (mergePairs)
            {
                mergePairs[mostFreqPair] = newToken;
            }

            foreach (var word in words)
            {
                for (int j = 0; j < word.Count - 1;)
                {
                    var builder = new StringBuilder(word[j]); // initialize StringBuilder with the first part of the pair
                    builder.Append(word[j + 1]); // append the second part of the pair

                    if (builder.ToString() == mostFreqPair)  // compare with StringBuilder result
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

            lock (vocab)
            {
                if (vocab.Contains(mostFreqPair[0].ToString()))
                {
                    vocab[mostFreqPair[0].ToString()] = (int)vocab[mostFreqPair[0].ToString()] - pairCounts.First(p => p.Key == mostFreqPair).Value;
                    if ((int)vocab[mostFreqPair[0].ToString()] <= 0)
                    {
                        vocab.Remove(mostFreqPair[0].ToString());
                    }
                }

                if (vocab.Contains(mostFreqPair[1].ToString()))
                {
                    vocab[mostFreqPair[1].ToString()] = (int)vocab[mostFreqPair[1].ToString()] - pairCounts.First(p => p.Key == mostFreqPair).Value;
                    if ((int)vocab[mostFreqPair[1].ToString()] <= 0)
                    {
                        vocab.Remove(mostFreqPair[1].ToString());
                    }
                }
            }
        }

        public int[] TokeniseAndCreateBins(string text, double trainRatio = 0.9)
        {
            string[] words = text.Split(' ');

            int trainChunkSize = 2048;
            int valChunkSize = 2048;

            int splitIndex = (int)(words.Length * trainRatio);

            string[] trainWords = words.Take(splitIndex).ToArray();
            string[] valWords = words.Skip(splitIndex).ToArray();

            string encodedTrain = Encode(string.Join(" ", trainWords));
            string encodedVal = Encode(string.Join(" ", valWords));

            int trainNumTokens = (int)Math.Ceiling(encodedTrain.Split(' ').Length / (double)trainChunkSize) * trainChunkSize;
            int valNumTokens = (int)Math.Ceiling(encodedVal.Split(' ').Length / (double)valChunkSize) * valChunkSize;

            int[] trainIds = encodedTrain.Split(' ').Select(token => token2id.Any(kv => kv.Key == token) ? token2id.First(kv => kv.Key == token).Value : token2id.First(kv => kv.Key == "<UNK>").Value).ToArray();
            int[] valIds = encodedVal.Split(' ').Select(token => token2id.Any(kv => kv.Key == token) ? token2id.First(kv => kv.Key == token).Value : token2id.First(kv => kv.Key == "<UNK>").Value).ToArray();

            trainIds = AdjustTokensToChunkSize(trainIds, trainChunkSize, trainNumTokens);
            valIds = AdjustTokensToChunkSize(valIds, valChunkSize, valNumTokens);

            File.WriteAllBytes("train.bin", trainIds.SelectMany(BitConverter.GetBytes).ToArray());
            File.WriteAllBytes("val.bin", valIds.SelectMany(BitConverter.GetBytes).ToArray());

            return trainIds;
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
       
        public void SaveModel(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Vocabulary:");
                foreach (DictionaryEntry entry in vocab)
                {
                    writer.WriteLine($"{entry.Key}\t{entry.Value}");
                }

                writer.WriteLine("Merge Pairs:");
                foreach (DictionaryEntry entry in mergePairs)
                {
                    writer.WriteLine($"{entry.Key}\t{entry.Value}");
                }

                writer.WriteLine("Token to ID Mappings:");
                foreach (var pair in token2id)
                {
                    writer.WriteLine($"{pair.Key}\t{pair.Value}");
                }
            }
        }
        public void LoadModel(string filePath)
        {
            vocab.Clear();
            mergePairs.Clear();
            token2id.Clear();
            tokenCount = 0; // Reset the token count

            using (StreamReader reader = new StreamReader(filePath))
            {
                string section = "";
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Vocabulary:"))
                    {
                        section = "Vocabulary";
                    }
                    else if (line.StartsWith("Merge Pairs:"))
                    {
                        section = "MergePairs";
                    }
                    else if (line.StartsWith("Token to ID Mappings:"))
                    {
                        section = "TokenToIdMappings";
                    }
                    else
                    {
                        switch (section)
                        {
                            case "Vocabulary":
                                {
                                    string[] parts = line.Split('\t');
                                    if (parts.Length == 2 && int.TryParse(parts[1], out int count))
                                    {
                                        if (!vocab.Contains(parts[0])) // Check if key already exists
                                        {
                                            vocab.Add(parts[0], count);
                                        }
                                    }
                                    break;
                                }
                            case "MergePairs":
                                {
                                    string[] parts = line.Split('\t');
                                    if (parts.Length == 2)
                                    {
                                        if (!mergePairs.Contains(parts[0])) // Check if key already exists
                                        {
                                            mergePairs.Add(parts[0], parts[1]);
                                        }
                                    }
                                    break;
                                }
                            case "TokenToIdMappings":
                                {
                                    string[] parts = line.Split('\t');
                                    if (parts.Length == 2 && int.TryParse(parts[1], out int id))
                                    {
                                        if (!token2id.Any(kv => kv.Key == parts[0])) // Check if key already exists
                                        {
                                            token2id.Add(new KeyValuePair<string, int>(parts[0], id));
                                            if (id > tokenCount)
                                            {
                                                tokenCount = id + 1; // Update the token count
                                            }
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
        }

        public string Encode(string text)
        {
            string[] words = text.Split(' ');
            StringBuilder encodedTokens = new StringBuilder();

            foreach (var word in words)
            {
                foreach (var ch in word)
                {
                    string token = ch.ToString();

                    if (!token2id.Any(kv => kv.Key == token))
                    {
                        token2id.Add(new KeyValuePair<string, int>(token, tokenCount));
                        tokenCount++;
                    }

                    encodedTokens.Append(token).Append(' ');
                }

                encodedTokens.Append("<SPACE>").Append(' ');

                int i = 0;
                while (i < encodedTokens.Length - 1)
                {
                    string pair = encodedTokens[i].ToString() + encodedTokens[i + 1].ToString();
                    if (mergePairs.Contains(pair))
                    {
                        string mergedToken = (string)mergePairs[pair]; // Cast to correct type
                        encodedTokens[i] = mergedToken[0];
                        encodedTokens.Remove(i + 1, 2);
                        if (!token2id.Any(kv => kv.Key == mergedToken))
                        {
                            token2id.Add(new KeyValuePair<string, int>(mergedToken, tokenCount));
                            tokenCount++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            if (encodedTokens.ToString().EndsWith("<SPACE> "))
            {
                encodedTokens.Remove(encodedTokens.Length - 8, 8);
            }
            int[] ids = encodedTokens.ToString().TrimEnd().Split(' ').Select(token => token2id.Any(kv => kv.Key == token) ? token2id.Single(kv => kv.Key == token).Value : token2id.Single(kv => kv.Key == "<UNK>").Value).ToArray();
            string idsString = string.Join(" ", ids);
            return idsString;
        }
        public string Decode(string idsString)
        {
            string[] idStrings = idsString.Split(' ');
            List<string> tokens = new List<string>();

            foreach (string idStr in idStrings)
            {
                if (int.TryParse(idStr, out int id))
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
            }

            int i = 0;
            while (i < tokens.Count)
            {
                if (tokens[i] == "<SPACE>")
                {
                    tokens[i] = " ";
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

        public int GetVocabSize()
        {
            return vocab.Count;
        }
    }
}
