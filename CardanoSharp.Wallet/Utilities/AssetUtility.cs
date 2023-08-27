namespace CardanoSharp.Wallet.Utilities;

public static class AssetUtility
{
    public static string GetHexPolicyId(string hexAsset)
    {
        string assetName = hexAsset[..56];
        return assetName;
    }

    public static string GetHexAssetName(string hexAsset)
    {
        string assetName = hexAsset[56..];
        return assetName;
    }

    public static string GetAssetFullName(string policyId, string assetName)
    {
        return $"{policyId}{assetName}";
    }

    public static string? GetCIP68AssetFullName(string cip25AssetFullName)
    {
        string policyId = cip25AssetFullName[..56];
        string assetName = cip25AssetFullName[56..];
        if (!assetName.StartsWith(AssetLabelUtility.GetAssetLabelHex(100)))
            return null;

        assetName = AssetLabelUtility.GetAssetLabelHex(222) + assetName.Substring(8);
        return $"{policyId}{assetName}";
    }
}
