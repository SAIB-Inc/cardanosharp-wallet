namespace CardanoSharp.Wallet.Common;

public static class FeeStructure
{
    public const uint Coefficient = 44;
    public const uint Constant = 155381;
    public const ulong MaxTxExMem = 14000000;
    public const ulong MaxTxExSteps = 10000000000;
    public const double PriceMem = 0.0577;
    public const double PriceStep = 0.0000721;
    public const uint RefScriptBase = 15;
    public const uint RefScriptRange = 25600;
    public const double RefScriptMultiplier = 1.2;
}
