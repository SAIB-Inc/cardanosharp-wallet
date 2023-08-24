using CardanoSharp.Wallet.Enums;

namespace CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts
{
    public class Redeemer
    {
        public RedeemerTag Tag { get; set; }
        public uint Index { get; set; }
        public IPlutusData PlutusData { get; set; } = default!;
        public ExUnits ExUnits { get; set; } = new ExUnits();

        // Building Data

        public Utxo? Utxo { get; set; } = null; // If this Utxo is set, calculate the index in the "Complete" function
    }
}
