using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace BytePairEncoding
{
    public class ModelLoader
    {
        private readonly BPE _bpe;
        public ModelLoader(BPE bpe)
        {
            _bpe = bpe;
        }

        public void ResetModelInMemoryToZeroNInitialiseSpecialTokens()
        {
            _bpe.vocab.Clear();
            _bpe.mergePairs.Clear();
            _bpe.token2id.Clear();
            _bpe.tokenCount = 0;

            _bpe.vocab["<UNK>"] = 0;
            _bpe.token2id.Add(new KeyValuePair<string, int>("<UNK>", 0));
            _bpe.vocab["<PAD>"] = 1;
            _bpe.token2id.Add(new KeyValuePair<string, int>("<PAD>", 1));
            _bpe.vocab["<NEWLINE>"] = 2;
            _bpe.token2id.Add(new KeyValuePair<string, int>("<NEWLINE>", 2));
            _bpe.tokenCount = 3;
        }

        public void LoadModel(string filePath)
        {
            ResetModelInMemoryToZeroNInitialiseSpecialTokens();

            using (StreamReader reader = new(filePath))
            {
                string section = "";
                string? line;
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
                        ProcessLine(section, line);
                    }
                }
            }
        }

        private void ProcessLine(string section, string line)
        {
            switch (section)
            {
                case "Vocabulary":
                    ProcessVocabularyLine(line);
                    break;
                case "MergePairs":
                    ProcessMergePairsLine(line);
                    break;
                case "TokenToIdMappings":
                    ProcessTokenToIdMappingsLine(line);
                    break;
            }
        }

        private void ProcessVocabularyLine(string line)
        {
            string[] parts = line.Split('¦');
            if (parts.Length == 2 && int.TryParse(parts[1], out int count))
            {
                if (!_bpe.vocab.Contains(parts[0]))
                {
                    _bpe.vocab.Add(parts[0], count);
                }
            }
        }

        private void ProcessMergePairsLine(string line)
        {
            string[] parts = line.Split('¦');
            if (parts.Length == 2)
            {
                if (!_bpe.mergePairs.Contains(parts[0]))
                {
                    _bpe.mergePairs.Add(parts[0], parts[1]);
                }
            }
        }

        private void ProcessTokenToIdMappingsLine(string line)
        {
            string[] parts = line.Split('¦');
            if (parts.Length == 2 && int.TryParse(parts[1], out int id))
            {
                if (!_bpe.token2id.Any(kv => kv.Key == parts[0]))
                {
                    _bpe.token2id.Add(new KeyValuePair<string, int>(parts[0], id));
                    if (id > _bpe.tokenCount)
                    {
                        _bpe.tokenCount = id + 1;
                    }
                }
            }
        }
    }
}
