using CardanoSharp.Wallet.Models.Keys;

namespace CardanoSharp.Wallet.Models.Transactions;

//vkeywitness = [ $vkey, $signature]
public partial class VKeyWitness
{
    public PublicKey VKey { get; set; } = default!;
    public PrivateKey SKey { get; set; } = default!;
    public byte[] Signature { get; set; } = default!;
    public bool IsMock { get; set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        VKeyWitness other = (VKeyWitness)obj;
        return VKey.Equals(other.VKey) && SKey.Equals(other.SKey);
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(VKey, SKey);
    }
}
