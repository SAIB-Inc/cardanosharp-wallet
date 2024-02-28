using CardanoSharp.Wallet.Models.Keys;

namespace CardanoSharp.Wallet.Models.Transactions;

//vkeywitness = [ $vkey, $signature]
public partial class VKeyWitness
{
    public PublicKey VKey { get; set; } = default!;
    public PrivateKey SKey { get; set; } = default!;
    public byte[] Signature { get; set; } = default!;
    public bool IsMock { get; set; } = default!;
}
