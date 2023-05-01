namespace CardanoSharp.Wallet.Models
{
    public class Utxo
    {
        public string TxHash { get; set; } = null!;

        public uint TxIndex { get; set; }

        public string OutputAddress { get; set; } = null!;

        public Balance Balance { get; set; } = new Balance();
    }
}
