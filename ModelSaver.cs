using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class ModelSaver
    {
        private readonly BPE _bpe;

        public ModelSaver(BPE bpe)
        {
            _bpe = bpe;
        }

        public void SaveModel(string filePath)
        {
            int lastValue = _bpe.token2id.Max(pair => pair.Value);
            int spaceValue = lastValue + 1;

            _bpe.token2id.Add(new KeyValuePair<string, int>(" ", spaceValue));

            using (StreamWriter writer = new (filePath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.WriteLine("Vocabulary:");
                foreach (DictionaryEntry entry in _bpe.vocab)
                {
                    writer.WriteLine($"{entry.Key}¦{entry.Value}");
                }

                writer.WriteLine("Merge Pairs:");
                foreach (DictionaryEntry entry in _bpe.mergePairs)
                {
                    writer.WriteLine($"{entry.Key}¦{entry.Value}");
                }

                writer.WriteLine("Token to ID Mappings:");
                foreach (var pair in _bpe.token2id)
                {
                    writer.WriteLine($"{pair.Key}¦{pair.Value}");
                }
            }
        }
    }
}
