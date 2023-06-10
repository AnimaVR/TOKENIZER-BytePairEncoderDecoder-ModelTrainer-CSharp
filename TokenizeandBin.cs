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
        public EncodeDecode encodeDecode;
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
            this.encodeDecode = new EncodeDecode(bpe);
        }

        public int[] TokeniseAndCreateBins(string fileName, double trainRatio = 0.9)
        {
            string text = File.ReadAllText(fileName);

            int[] encodedWords = encodeDecode.Encode(text);

            int trainChunkSize = 2048;
            int valChunkSize = 2048;

            int splitIndex = (int)(encodedWords.Length * trainRatio);

            int[] trainWords = encodedWords.Take(splitIndex).ToArray();
            int[] valWords = encodedWords.Skip(splitIndex).ToArray();

            int trainNumTokens = (int)Math.Ceiling(trainWords.Length / (double)trainChunkSize) * trainChunkSize;
            int valNumTokens = (int)Math.Ceiling(valWords.Length / (double)valChunkSize) * valChunkSize;

            int[] trainIds = AdjustTokensToChunkSize(trainWords, trainChunkSize, trainNumTokens);
            int[] valIds = AdjustTokensToChunkSize(valWords, valChunkSize, valNumTokens);

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite("train.bin")))
            {
                foreach (int id in trainIds)
                {
                    writer.Write(id);
                }
            }

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite("val.bin")))
            {
                foreach (int id in valIds)
                {
                    writer.Write(id);
                }
            }

            return valIds;
        }

        private int[] AdjustTokensToChunkSize(int[] tokens, int chunkSize, int numTokens)
        {
            if (tokens.Length < numTokens)
            {
                int[] adjustedTokens = new int[numTokens];
                Array.Copy(tokens, adjustedTokens, tokens.Length);

                int paddingToken = token2id.Single(kv => kv.Key == "<PAD>").Value;
                for (int i = tokens.Length; i < numTokens; i++)
                {
                    adjustedTokens[i] = paddingToken;
                }

                return adjustedTokens;
            }
            else if (tokens.Length > numTokens)
            {
                int[] adjustedTokens = new int[numTokens];
                Array.Copy(tokens, adjustedTokens, numTokens);
                return adjustedTokens;
            }
            else
            {
                return tokens;
            }
        }
    }
}
