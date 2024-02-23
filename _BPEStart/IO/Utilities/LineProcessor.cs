using System.Collections.Generic;
using System.Linq;

namespace BytePairEncoding
{
    public class LineProcessor
    {
        BPE _bpe;
        public LineProcessor(BPE bpe)
        {
            _bpe = bpe;
        }

        public void ProcessLine(string section, string line)
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

        public void ProcessVocabularyLine(string line)
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

        public void ProcessMergePairsLine(string line)
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

        public void ProcessTokenToIdMappingsLine(string line)
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
