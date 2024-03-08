namespace CardanoSharp.Wallet.CIPs.CIP2;

public enum CoinSelectionType
{
    None = 0,
    All = 1,
    LargestFirst = 2,
    RandomImprove = 3,
    OptimizedRandomImprove = 4,
}
