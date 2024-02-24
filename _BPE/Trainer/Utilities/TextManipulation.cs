using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytePairEncoding
{
    public class TextManipulation
    {
        public static async Task<string> ReadAndSanitizeTextAsync(string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            string rawText = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            return Encoding.UTF8.GetString(Encoding.Convert(Encoding.UTF8, Encoding.UTF8, Encoding.UTF8.GetBytes(rawText)));
        }

        public static List<List<string>> SplitTextIntoWords(string text)
        {
            return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                       .SelectMany(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(word => word.Normalize(NormalizationForm.FormD)
                                                       .Select(ch => ch.ToString())
                                                       .Append("<SPACE>")
                                                       .ToList()))
                       .ToList();
        }

        
        public static Dictionary<string, int> AccumulateWordFrequencies(List<List<string>> words)
        {
            var tempVocab = new Dictionary<string, int>();
            foreach (var wordList in words)
            {
                foreach (var word in wordList)
                {
                    if (!tempVocab.ContainsKey(word))
                    {
                        tempVocab[word] = 1;
                    }
                    else
                    {
                        tempVocab[word] += 1;
                    }
                }

                if (!tempVocab.ContainsKey("<SPACE>"))
                {
                    tempVocab["<SPACE>"] = 1;
                }
                else
                {
                    tempVocab["<SPACE>"] += 1;
                }
            }
            return tempVocab;
        }

    }
}
