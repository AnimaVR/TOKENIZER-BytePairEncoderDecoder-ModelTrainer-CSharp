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
            WriteIdsToBinFileAsync("train.bin", trainWords),
            WriteIdsToBinFileAsync("val.bin", valWords)
                };
                await Task.WhenAll(writeTasks);

                progress?.Report(100);

                return valWords;
            }
        }

        private static async Task WriteIdsToBinFileAsync(string fileName, int[] ids)
        {
            using (FileStream fs = new (fileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            using (BinaryWriter writer = new (fs))
            {
                foreach (int id in ids)
                {
                    writer.Write(id);
                }
                await fs.FlushAsync();
            }
        }

        public static async Task WriteIdsToBinFileWithPaddingAsync(string fileName, int[] ids, int chunkSizeInBytes = 4096, int paddingToken = -1)
        {
            int tokenSizeInBytes = 4; // Size of int in bytes
            int tokensPerChunk = chunkSizeInBytes / tokenSizeInBytes;
            int totalTokens = ids.Length;
            int totalFullChunks = totalTokens / tokensPerChunk;
            int remainingTokens = totalTokens % tokensPerChunk;

            FileStream stream = new (fileName, FileMode.Create, FileAccess.Write);
            using (BinaryWriter writer = new (stream))
            {
                // Write full chunks
                for (int i = 0; i < totalFullChunks; i++)
                {
                    for (int j = 0; j < tokensPerChunk; j++)
                    {
                        int tokenIndex = i * tokensPerChunk + j;
                        writer.Write(ids[tokenIndex]);
                    }
                }

                // Write remaining tokens, if any
                if (remainingTokens > 0)
                {
                    for (int i = 0; i < remainingTokens; i++)
                    {
                        int tokenIndex = totalFullChunks * tokensPerChunk + i;
                        writer.Write(ids[tokenIndex]);
                    }

                    // Pad the last chunk
                    int paddingTokensNeeded = tokensPerChunk - remainingTokens;
                    for (int i = 0; i < paddingTokensNeeded; i++)
                    {
                        writer.Write(paddingToken);
                    }
                }
            }

            await stream.FlushAsync();
            stream.Close();
        }


    }
}
