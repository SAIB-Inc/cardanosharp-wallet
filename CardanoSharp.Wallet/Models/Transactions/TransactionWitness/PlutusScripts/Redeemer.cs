using System;
using CardanoSharp.Wallet.Enums;

namespace CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

public class Redeemer
{
    public RedeemerTag Tag { get; set; }
    public uint Index { get; set; }
    public IPlutusData PlutusData { get; set; } = default!;
    public ExUnits ExUnits { get; set; } = new ExUnits();

    // Building Data
    public Utxo? Utxo { get; set; } = null; // If this Utxo is set, calculate the index in the "Complete" function
    public int? ParameterIndex { get; set; } = null; // If this ParameterIndex is set, also apply the Index to the PlutusData array at the ParameterIndex

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Redeemer other = (Redeemer)obj;

        // If Utxo is not null, base equality on it
        if (Utxo != null)
            return Utxo.Equals(other.Utxo);

        // If Utxo is null, use reference equality
        return Object.ReferenceEquals(this, other);
    }

    public override int GetHashCode()
    {
        // If Utxo is not null, base hash code on it
        if (Utxo != null)
            return Utxo.GetHashCode();

        // If Utxo is null, use the default hash code (i.e., based on object's reference)
        return base.GetHashCode();
    }
}
