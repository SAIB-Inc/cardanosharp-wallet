using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

namespace CardanoSharp.Wallet.Models;

public class Utxo
{
    public string TxHash { get; set; } = null!;
    public uint TxIndex { get; set; }
    public Balance Balance { get; set; } = new Balance();

    // Data from previous transaction output
    public string OutputAddress { get; set; } = null!;
    public DatumOption? OutputDatumOption { get; set; }
    public ScriptReference? OutputScriptReference { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Utxo other = (Utxo)obj;
        return TxHash == other.TxHash && TxIndex == other.TxIndex;
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(TxHash, TxIndex);
    }
}
