using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class TrainNValSplitter
    {
        public static void SplitWordsIntoTrainAndVal(int[] encodedWords, double trainRatio, out int[] trainWords, out int[] valWords)
        {
            int splitIndex = (int)(encodedWords.Length * trainRatio);

            trainWords = encodedWords.Take(splitIndex).ToArray();
            valWords = encodedWords.Skip(splitIndex).ToArray();
        }
        public static async Task WriteIdsToBinFileAsync(string fileName, int[] ids)
        {
            using (FileStream fs = new(fileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            using (BinaryWriter writer = new(fs))
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

            FileStream stream = new(fileName, FileMode.Create, FileAccess.Write);
            using (BinaryWriter writer = new(stream))
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
