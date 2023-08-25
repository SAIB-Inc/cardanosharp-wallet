namespace CardanoSharp.Wallet.Models.Transactions;

public partial class PoolMetadata
{
    public string Url { get; set; } = default!;
    public byte[] MetadataHash { get; set; } = default!;
}
