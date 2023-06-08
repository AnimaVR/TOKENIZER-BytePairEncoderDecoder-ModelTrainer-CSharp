using System.Linq;
using System.IO;
using System.Windows;
using System;

namespace BytePairEncoding
{
    public partial class MainWindow : Window
    {
        private int[] encodedIds;
        BPE bpe;
        public MainWindow()
        {
            InitializeComponent();
            bpe = new BPE();
        }
        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            await bpe.TrainAsync("input.txt", 2, 1);
            vocabSizeTextBlock.Text = "Vocabulary size: " + bpe.GetVocabSize().ToString();
           
        }
        private void encodeButton_Click(object sender, RoutedEventArgs e)
        {
            string inputText = inputTextBox.Text;
            encodedIds = bpe.Encode(inputText);
            string encodedText = string.Join(" ", encodedIds);
            encodedTextBlock.Text = encodedText;
        }

        private void decodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (encodedIds != null)
            {
                string decodedText = bpe.Decode(encodedIds);
                decodedTextBlock.Text = decodedText;
            }
            else
            {
                // Handle the case when no encoded IDs are available
                decodedTextBlock.Text = "No encoded text to decode.";
            }
        }


        private void LoadModelButton_Click(object sender, RoutedEventArgs e)
        {
            string modelPath = "model.json";
            if (File.Exists(modelPath))
            {
                bpe.LoadModel(modelPath);
                MessageBox.Show("Model loaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No model found. You need to train one first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TokenizeData_Click(object sender, RoutedEventArgs e)
        {
            string text = File.ReadAllText("input.txt");
            int[] trainIds = bpe.TokeniseAndCreateBins(text, 0.9);
            string trainBinContent = string.Join(" ", trainIds);
            trainBinTextBlock.Text = trainBinContent;
        }
        private void sampleButton_Click(object sender, RoutedEventArgs e)
        {
            // Read the train.bin file
            byte[] trainBytes = File.ReadAllBytes("train.bin");

            // Convert the byte array back to an array of integers
            int[] trainIds = new int[trainBytes.Length / sizeof(int)];
            Buffer.BlockCopy(trainBytes, 0, trainIds, 0, trainBytes.Length);

            // Define the block size
            int blockSize = 2048;

            // Sample a random block start index
            Random random = new Random();
            int randomIndex = random.Next(0, trainIds.Length / blockSize) * blockSize;

            // Get the block of IDs at the random index
            int[] blockOfIds = new int[blockSize];
            Array.Copy(trainIds, randomIndex, blockOfIds, 0, blockSize);

            // Decode the block of IDs
            string decodedText = bpe.Decode(blockOfIds);
            decodedText = decodedText.Replace("<UNK>", "");
            // Display the decoded text
            decodedTextBlock.Text = decodedText;
        }



    }
}
