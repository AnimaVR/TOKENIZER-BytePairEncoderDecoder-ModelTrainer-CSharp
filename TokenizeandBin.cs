using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class TokenizeandBin
    {
        public BPE bpe;
        public Encoder encoder;
        public List<KeyValuePair<string, int>> token2id;
        public OrderedDictionary vocab;
        public OrderedDictionary mergePairs;
        public int tokenCount;

        public TokenizeandBin(BPE bpe)
        {
            this.bpe = bpe;
            this.token2id = bpe.token2id;
            this.vocab = bpe.vocab;
            this.mergePairs = bpe.mergePairs;
            this.tokenCount = bpe.tokenCount;
            this.encoder = new Encoder(bpe);
        }

        public async Task<int[]> ProcessFileAsync(string fileName, double trainRatio = 0.9, IProgress<int> progress = null)
        {
            string text = await File.ReadAllTextAsync(fileName);
            int[] encodedWords = await encoder.EncodeAsync(text);
            SplitWordsIntoTrainAndVal(encodedWords, trainRatio, out var trainWords, out var valWords);
            await AdjustWordsToChunkSizeAndWriteToFileAsync("train.bin", trainWords, progress);
            await AdjustWordsToChunkSizeAndWriteToFileAsync("val.bin", valWords, progress);

            return valWords;
        }

        private void SplitWordsIntoTrainAndVal(int[] encodedWords, double trainRatio, out int[] trainWords, out int[] valWords)
        {
            int splitIndex = (int)(encodedWords.Length * trainRatio);
            trainWords = encodedWords.Take(splitIndex).ToArray();
            valWords = encodedWords.Skip(splitIndex).ToArray();
        }

        private async Task AdjustWordsToChunkSizeAndWriteToFileAsync(string fileName, int[] words, IProgress<int> progress)
        {
            int chunkSize = 2048;
            int numTokens = (int)Math.Ceiling(words.Length / (double)chunkSize) * chunkSize;
            int[] adjustedWords = AdjustTokensToChunkSize(words, chunkSize, numTokens);

            int progressPercentage = 0;
            for (int i = 0; i < adjustedWords.Length; i += chunkSize)
            {
                int[] chunk = adjustedWords.Skip(i).Take(chunkSize).ToArray();
                WriteIdsToBinFile(fileName, chunk);

                progressPercentage = (int)((i + chunkSize) / (double)adjustedWords.Length * 100);
                progress?.Report(progressPercentage);

                // Introduce a delay to simulate gradual progress
                await Task.Delay(1); // Adjust the delay duration as needed
            }

            progress?.Report(100);
        }

        private int[] AdjustTokensToChunkSize(int[] tokens, int chunkSize, int numTokens)
        {
            if (tokens.Length >= numTokens)
            {
                return tokens;
            }

            int[] adjustedTokens = new int[numTokens];
            Array.Copy(tokens, adjustedTokens, tokens.Length);

            int paddingToken = token2id.Single(kv => kv.Key == "<PAD>").Value;

            for (int i = tokens.Length; i < numTokens; i++)
            {
                adjustedTokens[i] = paddingToken;
            }

            return adjustedTokens;
        }

        private void WriteIdsToBinFile(string fileName, int[] ids)
        {
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(fileName)))
            {
                foreach (int id in ids)
                {
                    writer.Write(id);
                }
            }
        }
    }
}
