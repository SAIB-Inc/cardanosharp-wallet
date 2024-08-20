namespace CardanoSharp.Wallet.Models.Transactions;

public partial class StakeDeregistration
{
    public int StakeCredentialType { get; set; } // 0, addr_keyhash - 1, scripthash
    public byte[] StakeCredential { get; set; } = default!;
}
