namespace CardanoSharp.Wallet.Models.Transactions;

public partial class StakeDelegation
{
    public byte[] StakeCredential { get; set; } = default!;
    public byte[] PoolHash { get; set; } = default!;
}
