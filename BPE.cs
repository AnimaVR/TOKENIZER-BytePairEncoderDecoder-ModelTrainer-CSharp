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
        private int tokenCount = 0;


        public void SaveModel(string filePath)
        {
            int lastValue = token2id.Max(pair => pair.Value);
            int spaceValue = lastValue + 1;

            // Add a new entry with key as space and value as spaceValue
            token2id.Add(new KeyValuePair<string, int>(" ", spaceValue));

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Vocabulary:");
                foreach (DictionaryEntry entry in vocab)
                {
                    writer.WriteLine($"{entry.Key}¦{entry.Value}"); // Use '|' as the delimiter
                }

                writer.WriteLine("Merge Pairs:");
                foreach (DictionaryEntry entry in mergePairs)
                {
                    writer.WriteLine($"{entry.Key}¦{entry.Value}"); // Use '|' as the delimiter
                }

                writer.WriteLine("Token to ID Mappings:");
                foreach (var pair in token2id)
                {
                    writer.WriteLine($"{pair.Key}¦{pair.Value}"); // Use '|' as the delimiter
                }
            }
        }

        public void LoadModel(string filePath)
        {
            vocab.Clear();
            mergePairs.Clear();
            token2id.Clear();
            tokenCount = 0; // Reset the token count

            vocab["<UNK>"] = 0;
            token2id.Add(new KeyValuePair<string, int>("<UNK>", 0));
            vocab["<PAD>"] = 1;
            token2id.Add(new KeyValuePair<string, int>("<PAD>", 1));
            vocab["<NEWLINE>"] = 2;
            token2id.Add(new KeyValuePair<string, int>("<NEWLINE>", 2));
            tokenCount = 3;

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
                                    string[] parts = line.Split('¦'); // Use '|' as the delimiter
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
                                    string[] parts = line.Split('¦'); // Use '|' as the delimiter
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
                                    string[] parts = line.Split('¦'); // Use '|' as the delimiter
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




        public async Task TrainAsync(string fileName, int numMerges, int minFrequency)
        {
            vocab.Clear();
            mergePairs.Clear();
            token2id.Clear();
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            string text = await File.ReadAllTextAsync(filePath);

            // Handle spaces at the end of lines here
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var words = new List<List<string>>();
            foreach (var line in lines)
            {
                var lineWords = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(word => new List<string>(word.Select(ch => ch.ToString()))).ToList();
                lineWords.Add(new List<string> { "<SPACE>" });
                words.AddRange(lineWords);
            }

            await LoadVocabAsync(words, minFrequency);
            vocab["<UNK>"] = 0;
            token2id.Add(new KeyValuePair<string, int>("<UNK>", 0));
            vocab["<PAD>"] = 1;
            token2id.Add(new KeyValuePair<string, int>("<PAD>", 1));
            vocab["<NEWLINE>"] = 2;
            token2id.Add(new KeyValuePair<string, int>("<NEWLINE>", 2));
            tokenCount = 3;

            for (int i = 0; i < numMerges; i++)
            {
                var result = await CountPairAsync(words);
                var pairCounts = result.Item1;
                var mostFreqPair = result.Item2;

                if (pairCounts.Count == 0)
                {
                    break;
                }

                MergeMostFrequentPair(pairCounts, mostFreqPair, words);
            }

            foreach (string token in vocab.Keys)
            {
                if (!token2id.Any(kv => kv.Key == token))
                {
                    token2id.Add(new KeyValuePair<string, int>(token, tokenCount));
                    tokenCount++;
                }
            }


            SaveModel("model.txt");

            LoadModel("model.txt");  // this is a workaround to allow the model to be consistent between closing after training it into memory.
                                     // memory is saved slightly differently into file randomly so when we load it it's different and we need to tokenize again.... annoyingly!!!
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

            List<string> subwordUnitsToRemove = new List<string>();
            foreach (DictionaryEntry pair in vocab)
            {
                if ((int)pair.Value < minFrequency)
                {
                    subwordUnitsToRemove.Add(pair.Key.ToString());
                }
            }

            foreach (var subwordUnit in subwordUnitsToRemove)
            {
                vocab.Remove(subwordUnit);
            }

        }
        private async Task<(List<KeyValuePair<string, int>>, KeyValuePair<string, int>?)> CountPairAsync(List<List<string>> words)
        {
            var pairCounts = new List<KeyValuePair<string, int>>();
            KeyValuePair<string, int>? mostFreqPair = null;
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

                            // Check if this new pair is the most frequent
                            if (mostFreqPair == null || pair.Value > mostFreqPair.Value.Value)
                            {
                                mostFreqPair = pair;
                            }
                        }
                        else
                        {
                            var updatedPair = new KeyValuePair<string, int>(pair.Key, existingPair.Value.Value + pair.Value);
                            pairCounts[pairCounts.IndexOf(existingPair.Value)] = updatedPair;

                            // Check if the updated pair is now the most frequent
                            if (updatedPair.Value > mostFreqPair.Value.Value)
                            {
                                mostFreqPair = updatedPair;
                            }
                        }
                    }
                }
            }));

            return (pairCounts, mostFreqPair);
        }
        private void MergeMostFrequentPair(List<KeyValuePair<string, int>> pairCounts, KeyValuePair<string, int>? mostFreqPair, List<List<string>> words)
        {
            if (mostFreqPair == null)
            {
                return;
            }

            string mostFreqPairKey = mostFreqPair.Value.Key;
            int mostFreqPairValue = mostFreqPair.Value.Value;
            string newToken = mostFreqPairKey;

            lock (vocab)
            {
                vocab[newToken] = mostFreqPairValue;
            }

            lock (mergePairs)
            {
                mergePairs[mostFreqPairKey] = newToken;
            }

            foreach (var word in words)
            {
                for (int j = 0; j < word.Count - 1;)
                {
                    var builder = new StringBuilder(word[j]);
                    builder.Append(word[j + 1]);

                    if (builder.ToString() == mostFreqPairKey)
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




        public int GetVocabSize()
        {
            return vocab.Count;
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
