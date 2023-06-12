using System.IO;
using System.Windows;
using System;
using System.Linq;

namespace BytePairEncoding
{
    public partial class MainWindow : Window
    {
        private int[] encodedIds;
        Encoder encodedecode;
        Decoder decoder;
        BPE bpe;
        TokenizeandBin tokenizeandbin;
        Train train;
        public MainWindow()
        {
            InitializeComponent();
            bpe = new BPE();
            encodedecode = new Encoder(bpe);
            decoder = new Decoder(bpe);
            tokenizeandbin = new TokenizeandBin(bpe);
            train = new Train(bpe);
        }


        private void LoadModelButton_Click(object sender, RoutedEventArgs e)
        {
            string modelPath = "model.txt";
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

        private async void startTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            vocabSizeTextBlock.Text = "Training the model, please wait";
            await train.TrainAsync("input.txt", 2, 0);
            vocabSizeTextBlock.Text = "Training complete, vocabulary size of model = " + bpe.GetVocabSize().ToString() + "+1 for the end of line spaces";
           
        }

        private void encodeButton_Click(object sender, RoutedEventArgs e)
        {
            string inputText = inputTextBox.Text;
            encodedIds = encodedecode.Encode(inputText);
            string encodedText = string.Join(" ", encodedIds);
            encodedTextBlock.Text = encodedText;
        }

        private void decodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (encodedIds != null)
            {
                string decodedText = decoder.Decode(encodedIds);
                decodedTextBlock.Text = decodedText;
            }
            else
            {
                decodedTextBlock.Text = "No encoded text to decode.";
            }
        }

        private void TokenizeData_Click(object sender, RoutedEventArgs e)
        {
            vocabSizeTextBlock.Text = "Tokenising and saving training and validation data to bins";
            string fileName = "input.txt";
            int[] valIds = tokenizeandbin.ProcessFile(fileName, 0.9);
            string valBinContent = string.Join(" ", valIds);
            valBinTextBlock.Text = valBinContent;
            vocabSizeTextBlock.Text = "Tokenisation and bin saving complete";
        }

        private void sampleButton_Click(object sender, RoutedEventArgs e)
        {
            // Read the train.bin file
            byte[] trainBytes = File.ReadAllBytes("val.bin");

            // Convert the byte array back to an array of integers
            int[] trainIds = new int[trainBytes.Length / sizeof(int)];
            Buffer.BlockCopy(trainBytes, 0, trainIds, 0, trainBytes.Length);

            // Define the block size
            int blockSize = 4096;
            
            int[] blockOfIds = trainIds.Take(blockSize).ToArray();

            string decodedText = decoder.Decode(blockOfIds);

            valBinTextBlock.Text = decodedText;
           
        }

    }
}
