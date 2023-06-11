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

        private string ReadTextFromFile(string fileName)
        {
            return File.ReadAllText(fileName);
        }
      
        public int[] ProcessFile(string fileName, double trainRatio = 0.9)
        {
            string text = ReadTextFromFile(fileName);
            int[] encodedWords = EncodeWords(text);
            SplitWordsIntoTrainAndVal(encodedWords, trainRatio, out var trainWords, out var valWords);
            AdjustWordsToChunkSizeAndWriteToFile("train.bin", trainWords);
            int[] valIds = AdjustWordsToChunkSizeAndWriteToFile("val.bin", valWords);
          
            return valIds;
        }

        private int[] AdjustWordsToChunkSizeAndWriteToFile(string fileName, int[] words)
        {
            int chunkSize = 2048;
            int numTokens = (int)Math.Ceiling(words.Length / (double)chunkSize) * chunkSize;
            int[] adjustedWords = AdjustTokensToChunkSize(words, chunkSize, numTokens);
            WriteIdsToBinFile(fileName, adjustedWords);

            return adjustedWords; 
        }

        private int[] EncodeWords(string text)
        {
            return encodeDecode.Encode(text);
        }

        private void SplitWordsIntoTrainAndVal(int[] encodedWords, double trainRatio, out int[] trainWords, out int[] valWords)
        {
            int splitIndex = (int)(encodedWords.Length * trainRatio);
            trainWords = encodedWords.Take(splitIndex).ToArray();
            valWords = encodedWords.Skip(splitIndex).ToArray();
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
