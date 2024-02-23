using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    }
}
