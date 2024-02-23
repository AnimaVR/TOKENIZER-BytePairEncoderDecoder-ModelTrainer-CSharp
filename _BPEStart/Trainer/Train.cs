using System;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class Train
    {
        public BPE _bpe;
        private readonly ModelManipulation modelmanip;

        public Train(BPE bpe)
        {
            _bpe = bpe;
            modelmanip = new ModelManipulation(_bpe);
        }

        public async Task TrainingStart(string fileName, int numMerges, int minFrequency, IProgress<int>? progress)
        {
            _bpe.loader.ResetModelInMemoryToZeroNInitialiseSpecialTokens();

            var sanitizedText = await TextManipulation.ReadAndSanitizeTextAsync(fileName);

            var words = TextManipulation.SplitTextIntoWords(sanitizedText);

            var tempVocab = TextManipulation.AccumulateWordFrequencies(words);

            modelmanip.UpdateMainVocabulary(tempVocab);

            modelmanip.RemoveTokensBelowFrequency(minFrequency);

            await modelmanip.PerformMerges(words, numMerges, progress);

            modelmanip.FinalizeToken2Id();

            _bpe.saver.SaveModel("model.txt");
        }
    }
}
