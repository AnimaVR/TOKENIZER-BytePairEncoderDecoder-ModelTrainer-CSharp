using System.Linq;
using System.IO;
using System.Windows;

namespace BytePairEncoding
{
    public partial class MainWindow : Window
    {
        BPE bpe;
        public MainWindow()
        {
            InitializeComponent();
            bpe = new BPE();
        }
        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            await bpe.TrainAsync("input.txt", 1, 1);
            vocabSizeTextBlock.Text = "Vocabulary size: " + bpe.GetVocabSize().ToString();
           
        }
        private void encodeButton_Click(object sender, RoutedEventArgs e)
        {
            string inputText = inputTextBox.Text;
            string encodedText = bpe.Encode(inputText);
            encodedTextBlock.Text = "Encoded Text: " + encodedText;
        }
        private void decodeButton_Click(object sender, RoutedEventArgs e)
        {
            string encodedText = encodedTextBlock.Text;
            string decodedText = bpe.Decode(encodedText);
            decodedTextBlock.Text = "Decoded Text: " + decodedText;
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
            trainBinTextBlock.Text = "Completed tokenising and saving of bins! Woohooohoho!" + trainBinContent;
        }
    }
}
