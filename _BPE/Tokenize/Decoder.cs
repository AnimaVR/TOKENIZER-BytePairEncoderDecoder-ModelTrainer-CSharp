using System.Linq;

namespace BytePairEncoding
{
    public class Decoder
    {
        private readonly BPE _bpe;
        private readonly DecodedTokenExpander _expander;

        public Decoder(BPE bpe)
        {
            _bpe = bpe;
            _expander = new DecodedTokenExpander(_bpe);
        }

        public string Decode(int[] ids)
        {
            var tokens = ids.Select(id => _bpe.token2id.FirstOrDefault(kv => kv.Value == id).Key ?? "<UNK>").ToList();

            tokens = _expander.ExpandTokens(tokens);

            tokens.RemoveAll(token => token == "<PAD>");

            return string.Join("", tokens);
        }

    }
}
