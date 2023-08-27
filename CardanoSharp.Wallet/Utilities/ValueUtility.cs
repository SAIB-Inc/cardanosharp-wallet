using System.Collections.Generic;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Models;

namespace CardanoSharp.Wallet.Utilities;

public static class ValueUtility
{
    //---------------------------------------------------------------------------------------------------//
    // Helper Functions
    //---------------------------------------------------------------------------------------------------//
    public static Balance GetBalance(List<Amount> amounts)
    {
        ulong lovelaces = 0;
        List<Asset> assets = new();
        foreach (Amount amount in amounts)
        {
            if (amount.Unit == "lovelace")
            {
                lovelaces = ulong.Parse(amount.Quantity);
            }
            else
            {
                Asset asset =
                    new()
                    {
                        PolicyId = amount.Unit[..56],
                        Name = amount.Unit[56..],
                        Quantity = long.Parse(amount.Quantity)
                    };
                assets.Add(asset);
            }
        }

        return new Balance() { Lovelaces = lovelaces, Assets = assets };
    }
    //---------------------------------------------------------------------------------------------------//
}
