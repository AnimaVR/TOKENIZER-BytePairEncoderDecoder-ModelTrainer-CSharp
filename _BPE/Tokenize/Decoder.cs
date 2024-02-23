using System.Collections.Generic;
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
            var tokens = ConvertIdsToTokens(ids);

            tokens = _expander.ExpandTokens(tokens);

            tokens.RemoveAll(token => token == "<PAD>");

            return string.Join("", tokens);
        }

        private List<string> ConvertIdsToTokens(int[] ids)
        {
            return ids.Select(id => _bpe.token2id.FirstOrDefault(kv => kv.Value == id).Key ?? "<UNK>").ToList();
        }

    }
}
