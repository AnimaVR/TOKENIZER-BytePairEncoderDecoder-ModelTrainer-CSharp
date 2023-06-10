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
        public List<KeyValuePair<string, int>> token2id = new List<KeyValuePair<string, int>>();
        public OrderedDictionary vocab = new OrderedDictionary();
        public OrderedDictionary mergePairs = new OrderedDictionary();
        public int tokenCount = 0;
    
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

       

        public int GetVocabSize()
        {
            return vocab.Count;
        }

    }
}
