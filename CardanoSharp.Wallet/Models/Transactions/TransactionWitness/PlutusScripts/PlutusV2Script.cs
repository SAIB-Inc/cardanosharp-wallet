namespace CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts
{
    // plutus_v2_script = bytes
    public class PlutusV2Script
    {
        public byte[] script { get; set; } = default!;

        public PlutusV2Script() { }
    }
}
