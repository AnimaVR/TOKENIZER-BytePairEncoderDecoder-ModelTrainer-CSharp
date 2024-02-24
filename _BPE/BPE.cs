using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class BPE
    {
        public Encoder encoder;
        public Decoder decoder;

        public TokenJuggler juggler;

        public TokenizeandBin tokenizeandbin;
        public Train train;

        public ModelLoader loader;
        public ModelSaver saver;

        public List<KeyValuePair<string, int>> token2id = new ();
        public OrderedDictionary vocab = new ();
        public OrderedDictionary mergePairs = new ();
        public int tokenCount = 0;

        public BPE()
        {
            loader = new ModelLoader(this);
            saver = new ModelSaver(this);

            train = new Train(this);

            encoder = new Encoder(this);
            decoder = new Decoder(this);

            juggler = new TokenJuggler(this);

            tokenizeandbin = new TokenizeandBin(this);

            loader.LoadModelStart();

        }

        public int[] Encode(string text)
        {
            return encoder.Encode(text);
        }

        public string Decode(int[] ids)
        {
            return decoder.Decode(ids);
        }

        public async Task Train(string fileName, int numMerges, int minFrequency, IProgress<int>? progress = null)
        {
            await train.TrainingStart(fileName, numMerges, minFrequency, progress);
        }

        public void SaveModel(string filePath)
        {
            saver.SaveModel(filePath);
        }

        public void LoadModel(string filePath)
        {
            loader.LoadModel(filePath);
        }

        public async Task<int[]> TokenizeAndBinTextFile(string fileName, double trainRatio = 0.9, IProgress<int>? progress = null)
        {
            return await tokenizeandbin.TokenizeNBinTxTFile(fileName, trainRatio, progress);
        }

        public int GetVocabSize()
        {
            return vocab.Count;
        }

    }
}
