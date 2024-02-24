# Byte Pair Trainer/Encoder/Decoder/Tokenizer
c# xaml byte pair encoder/decoder/tokenizer with a model trainer!

**You need to either move input.txt into the built program folder or add your own data to be able to train your own model, this model is then used to encode into train.bin and val.bin binary files to use to train an LLM or similar""

1. Take some .txt
2. Train the model on it
3. Use model to encode decode text
4. Tokenise and .bin a txt file in UTF-8
5. Use bin to fine tune a model with custom encode and decode from the model you made!

Needs some work but is a working example with shoddy ui that trains on input.txt and saves model.txt that you can use to encode and decode tokens.

Hope it helps someone understand bpe a bit better.
