using System;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class Train
    {
        public BPE _bpe;

        public Train(BPE bpe)
        {
            _bpe = bpe;  
        }

        public async Task TrainingStart(string fileName, int numMerges, int minFrequency, IProgress<int>? progress)
        {
            _bpe.loader.ResetModelInMemoryToZeroNInitialiseSpecialTokens();

            var sanitizedText = await TextManipulation.ReadAndSanitizeTextAsync(fileName);

            var words = TextManipulation.SplitTextIntoWords(sanitizedText);

            var tempVocab = TextManipulation.AccumulateWordFrequencies(words);

            _bpe.modelmanip.UpdateMainVocabulary(tempVocab);

            _bpe.modelmanip.RemoveTokensBelowFrequency(minFrequency);

            await _bpe.modelmanip.PerformMerges(words, numMerges, progress);

            _bpe.modelmanip.FinalizeToken2Id();

            _bpe.saver.SaveModel("model.txt");
        }
    }
}
