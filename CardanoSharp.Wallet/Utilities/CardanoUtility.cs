namespace CardanoSharp.Wallet.Utilities;

public static class CardanoUtility
{
    // Ada PolicyId and Asset Name
    public const string adaFullAssetName = "";
    public const string adaPolicyId = "";
    public const string adaAssetName = "";

    // Conversion Values
    public const long adaToLovelace = 1000000;
    public const double lovelaceToAda = 0.000001;
    public const long adaOnlyMinUtxo = 1000000;

    // Protocol Parameters
    public const double priceMem = 0.0577;
    public const double priceStep = 0.0000721;
    public const long maxTxExMem = 14000000;
    public const long maxTxExSteps = 10000000000;
}
