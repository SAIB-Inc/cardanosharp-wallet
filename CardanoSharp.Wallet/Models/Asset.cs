namespace CardanoSharp.Wallet.Models;

public class Asset
{
    public string PolicyId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long Quantity { get; set; }
}
