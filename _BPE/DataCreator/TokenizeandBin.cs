using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class TokenizeandBin
    {
        public BPE _bpe;

        public TokenizeandBin(BPE bpe)
        {
            _bpe = bpe;
        }

        public async Task<int[]> TokenizeNBinTxTFile(string fileName, double trainRatio = 0.9, IProgress<int>? progress = null)
        {
            using (var reader = new StreamReader(fileName, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false)))
            {
                string text = await reader.ReadToEndAsync();

                int[] encodedWords = _bpe.encoder.Encode(text);

                TrainNValSplitter.SplitWordsIntoTrainAndVal(encodedWords, trainRatio, out var trainWords, out var valWords);

                var writeTasks = new Task[]
                {
            TrainNValSplitter.WriteIdsToBinFileAsync("train.bin", trainWords),
            TrainNValSplitter.WriteIdsToBinFileAsync("val.bin", valWords)
                };
                await Task.WhenAll(writeTasks);

                progress?.Report(100);

                return valWords;
            }
        }
    }
}
