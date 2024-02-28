namespace CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

// plutus_v1_script = bytes
public class PlutusV1Script
{
    public byte[] script { get; set; } = default!;

    public PlutusV1Script() { }
}
