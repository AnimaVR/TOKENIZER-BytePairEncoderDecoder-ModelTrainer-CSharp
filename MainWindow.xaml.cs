using System.IO;
using System.Windows;
using System;
using System.Linq;

namespace BytePairEncoding
{
    public partial class MainWindow : Window
    {
        BPE bpe;
        Encoder encoder;
        Decoder decoder;
        TokenizeandBin tokenizeandbin;
        private int[] encodedIds;
        Train train;
        public MainWindow()
        {
            InitializeComponent();
            bpe = new BPE();
            encoder = new Encoder(bpe);
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
            startButton.IsEnabled = false;
            vocabSizeTextBlock.Text = "Training the model, please wait";
            var progress = new Progress<int>(value =>
            {
               progressBar.Value = value;
            });
            await train.TrainAsync("input.txt", 2, 0, progress);
            vocabSizeTextBlock.Text = "Training complete, vocabulary size of model = " + bpe.GetVocabSize().ToString() + "+1 for the end of line spaces";
            startButton.IsEnabled = true;
        }

        private async void encodeButton_Click(object sender, RoutedEventArgs e)
        {
            string inputText = inputTextBox.Text;
            encodedIds = await encoder.EncodeAsync(inputText);
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

        private async void TokenizeData_Click(object sender, RoutedEventArgs e)
        {
            CreateTrainValBins.IsEnabled = false;
            vocabSizeTextBlock.Text = "Tokenising and saving training and validation data to bins";
            string fileName = "input.txt";
            Progress<int> progress = new Progress<int>(percentage =>
            {
                progressBar.Value = percentage;
            });
            int[] valIds = await tokenizeandbin.ProcessFileAsync(fileName, 0.9, progress);
            string valBinContent = string.Join(" ", valIds);
            valBinTextBlock.Text = valBinContent;
            vocabSizeTextBlock.Text = "Tokenisation and bin saving complete";
            CreateTrainValBins.IsEnabled = true;
        }

        private void sampleButton_Click(object sender, RoutedEventArgs e)
        {
            byte[] trainBytes = File.ReadAllBytes("val.bin");
            int[] trainIds = new int[trainBytes.Length / sizeof(int)];
            Buffer.BlockCopy(trainBytes, 0, trainIds, 0, trainBytes.Length);
            int blockSize = 4096;
            int[] blockOfIds = trainIds.Take(blockSize).ToArray();
            string decodedText = decoder.Decode(blockOfIds);
            valBinTextBlock.Text = decodedText;
        }

    }
}
