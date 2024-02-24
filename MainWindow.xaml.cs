using System.IO;
using System.Windows;
using System;
using System.Linq;

namespace BytePairEncoding
{
    public partial class MainWindow : Window
    {
        private string lastEncodedText = "";
        readonly BPE bpe;

        private int[] encodedIds = Array.Empty<int>();

        public MainWindow()
        {
            InitializeComponent();

            bpe = new BPE();

        }

        private async void startTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = false;

            vocabSizeTextBlock.Text = "Training the model, please wait";

            var progress = new Progress<int>(value =>
            {
               progressBar.Value = value;
            });

            await bpe.Train("formatted_conversations.txt", 10000, 0, progress);

            vocabSizeTextBlock.Text = "Training complete, vocabulary size of model = " + bpe.GetVocabSize().ToString() + "+1 for the end of line spaces";

            startButton.IsEnabled = true;
        }

        private void LoadModelButton_Click(object sender, RoutedEventArgs e)
        {
            bpe.LoadModel("model.txt");
        }

        private async void TokenizeData_Click(object sender, RoutedEventArgs e)
        {
            CreateTrainValBins.IsEnabled = false;

            vocabSizeTextBlock.Text = "Tokenising and saving training and validation data to bins";

            string fileName = "formatted_conversations.txt";

            Progress<int> progress = new (percentage =>
            {
                progressBar.Value = percentage;
            });

            int[] valIds = await bpe.TokenizeAndBinTextFile(fileName, 0.9, progress);

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

            string decodedText = bpe.Decode(blockOfIds);

            valBinTextBlock.Text = decodedText;
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
                decodedTextBlock.Text = "No encoded text to decode.";
            }
        }

        private void inputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string currentInputText = inputTextBox.Text.Trim();

            if (!currentInputText.Equals(lastEncodedText, StringComparison.Ordinal))
            {
                encodeButton_Click(sender, e);

                lastEncodedText = currentInputText;

                decodeButton_Click(sender, e);
            }
        }

    }
}
