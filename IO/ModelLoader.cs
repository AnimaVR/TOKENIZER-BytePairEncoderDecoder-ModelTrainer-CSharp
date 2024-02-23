using BytePairEncoding.Utilities;
using System.Collections.Generic;
using System.IO;


namespace BytePairEncoding
{
    public class ModelLoader
    {
        public LineProcessor _processline;
        private readonly BPE _bpe;
        public ModelLoader(BPE bpe)
        {
            _bpe = bpe;
            _processline = new LineProcessor(_bpe);
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
                      _processline.ProcessLine(section, line);
                    }
                }
            }
        }

        public void LoadModelStart()
        {
            string modelPath = "model.txt";

             LoadModel(modelPath);
        }

    }
}
