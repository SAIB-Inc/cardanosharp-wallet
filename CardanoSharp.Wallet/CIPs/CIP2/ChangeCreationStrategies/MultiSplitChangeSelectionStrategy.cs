using System;
using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.CIPs.CIP2.Extensions;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Utilities;

namespace CardanoSharp.Wallet.CIPs.CIP2.ChangeCreationStrategies;

public class MultiSplitChangeSelectionStrategy : IChangeCreationStrategy
{
    public void CalculateChange(CoinSelection coinSelection, Balance outputBalance, string changeAddress, ulong feeBuffer = 0)
    {
        // Clear our change output list
        coinSelection.ChangeOutputs.Clear();
        var inputBalance = coinSelection.SelectedUtxos.AggregateAssets();

        // Calculate ideal change output conditions
        int idealChangeOutputs = CalculateIdealChangeOutputCount(inputBalance);
        int assetsPerOutput = (int)Math.Ceiling((double)inputBalance.Assets.Count / idealChangeOutputs);

        // Calculate change for token bundle
        var groupedAssets = inputBalance.Assets.GroupBy(asset => asset.PolicyId).SelectMany(group => group).ToList(); // We group the assets so that we reduce the size of multiple change outputs having the same policy dictionary
        foreach (var asset in groupedAssets)
        {
            CalculateTokenBundleUtxo(coinSelection, asset, outputBalance, changeAddress, assetsPerOutput, idealChangeOutputs);
        }

        // Determine/calculate the min lovelaces required for the token bundles
        ulong minLovelaces = 0;
        foreach (var changeOutput in coinSelection.ChangeOutputs)
        {
            ulong changeLovelaces = changeOutput.CalculateMinUtxoLovelace();
            minLovelaces += changeLovelaces;
            changeOutput.Value.Coin = changeLovelaces;
        }

        // Add remaining ada to the last ouput
        CalculateAdaUtxo(coinSelection, inputBalance.Lovelaces, minLovelaces, outputBalance, changeAddress, feeBuffer, idealChangeOutputs);
    }

    public static int CalculateIdealChangeOutputCount(Balance balance, int maxChangeOutputs = 4, int idealMaxAssetsPerOutput = 30)
    {
        // Determine how many change outputs we should have
        int adaChangeOutputCount = 1;
        int assetChangeOutputCount = 0;

        ulong lovelaces = balance.Lovelaces;
        if (lovelaces > 10000)
            adaChangeOutputCount = 2;

        var assets = balance.Assets;
        int assetCount = assets.Count;
        if (assetCount > 0)
            assetChangeOutputCount = (int)Math.Ceiling((double)assetCount / idealMaxAssetsPerOutput);

        int changeOutputCount = Math.Max(adaChangeOutputCount, assetChangeOutputCount);
        changeOutputCount = Math.Min(changeOutputCount, maxChangeOutputs);
        return changeOutputCount;
    }

    public static void CalculateTokenBundleUtxo(
        CoinSelection coinSelection,
        Asset asset,
        Balance outputBalance,
        string changeAddress,
        int assetsPerOutput,
        int idealChangeOutputs = 1
    )
    {
        // Get quantity of utxo for current asset
        long currentQuantity = coinSelection.SelectedUtxos
            .Where(x => x.Balance.Assets is not null)
            .SelectMany(
                x =>
                    x.Balance.Assets
                        .Where(al => al.PolicyId.SequenceEqual(asset.PolicyId) && al.Name.Equals(asset.Name))
                        .Select(x => (long)x.Quantity)
            )
            .Sum();

        var outputQuantity = outputBalance.Assets
            .Where(x => x.PolicyId.SequenceEqual(asset.PolicyId) && x.Name.Equals(asset.Name))
            .Select(x => x.Quantity)
            .Sum();

        // Determine change value for current asset based on requested and how much is selected
        var changeValue = currentQuantity - outputQuantity;
        if (changeValue <= 0)
            return;

        var changeUtxo = coinSelection.ChangeOutputs.LastOrDefault(x => x.Value.MultiAsset is not null);
        if (changeUtxo is null)
        {
            changeUtxo = new TransactionOutput()
            {
                Address = new Address(changeAddress).GetBytes(),
                Value = new TransactionOutputValue() { MultiAsset = new Dictionary<byte[], NativeAsset>() },
                OutputPurpose = OutputPurpose.Change
            };
            coinSelection.ChangeOutputs.Add(changeUtxo);
        }

        // Determine if we need to create a new change output
        bool createNewChangeOutput = false;

        // Determine if we already have an asset added with the same policy id
        var multiAsset = changeUtxo.Value.MultiAsset.Where(x => x.Key.SequenceEqual(asset.PolicyId.HexToByteArray()));
        if (!multiAsset.Any())
        {
            // Add policy and asset to token bundle
            changeUtxo.Value.MultiAsset.Add(
                asset.PolicyId.HexToByteArray(),
                new NativeAsset() { Token = new Dictionary<byte[], long>() { { asset.Name.HexToByteArray(), changeValue } } }
            );

            // We ideally only want to create a new change output for assets of different policies
            // We still perform a utxo is valid check
            int changeOutputsCount = coinSelection.ChangeOutputs.Count;
            int changeOutputAssetCount = changeUtxo.Value.MultiAsset.Sum(nativeAsset => nativeAsset.Value.Token.Count);
            createNewChangeOutput = changeOutputsCount < idealChangeOutputs && changeOutputAssetCount >= assetsPerOutput;
        }
        else
        {
            // Policy already exists in token bundle, just add the asset
            var policyAsset = multiAsset.FirstOrDefault();
            policyAsset.Value.Token.Add(asset.Name.HexToByteArray(), changeValue);
        }

        // If the changeUTXO is no longer valid, remove the asset that was just added, and create a new output
        // Set maxOutputBytesSize to 2000 to create more change Utxos for smoother future transactions
        if (createNewChangeOutput || !changeUtxo.IsValid(maxOutputBytesSize: 2000))
        {
            var previousMultiAsset = changeUtxo.Value.MultiAsset.Where(x => x.Key.SequenceEqual(asset.PolicyId.HexToByteArray())).FirstOrDefault();
            var previousToken = previousMultiAsset.Value.Token.Where(x => x.Key.SequenceEqual(asset.Name.HexToByteArray())).FirstOrDefault();

            long newTokenValue = previousToken.Value - changeValue;
            previousMultiAsset.Value.Token[previousToken.Key] = newTokenValue;
            if (newTokenValue <= 0)
            {
                previousMultiAsset.Value.Token.Remove(previousToken.Key);
            }

            if (previousMultiAsset.Value.Token.Count <= 0)
            {
                changeUtxo.Value.MultiAsset.Remove(previousMultiAsset.Key);
            }

            // Create a new Output and add it to the change outputs
            var newOutput = new TransactionOutput()
            {
                Address = new Address(changeAddress).GetBytes(),
                Value = new TransactionOutputValue() { MultiAsset = new Dictionary<byte[], NativeAsset>() },
                OutputPurpose = OutputPurpose.Change
            };

            newOutput.Value.MultiAsset.Add(
                asset.PolicyId.HexToByteArray(),
                new NativeAsset() { Token = new Dictionary<byte[], long>() { { asset.Name.HexToByteArray(), changeValue } } }
            );
            coinSelection.ChangeOutputs.Add(newOutput);
        }
    }

    public static void CalculateAdaUtxo(
        CoinSelection coinSelection,
        ulong ada,
        ulong tokenBundleMin,
        Balance outputBalance,
        string changeAddress,
        ulong feeBuffer = 0,
        int idealChangeOutputs = 1
    )
    {
        // Determine change value for current asset based on requested and how much is selected
        var changeValue = Math.Abs((long)(ada - tokenBundleMin - outputBalance.Lovelaces)) + (long)feeBuffer; // Add feebuffer to account for it being subtracted in the outputBalance.Lovelaces
        if (changeValue <= 0)
            return;

        // Determine how many change outputs we should have.
        int changeOutputsCount = coinSelection.ChangeOutputs.Count;
        int newChangeOutputs = idealChangeOutputs - changeOutputsCount;

        if (newChangeOutputs > 0)
        {
            // Ensure we have enough minUtxo in the changeValue to create new change outputs
            int maxNewChangeOutputs = (int)Math.Floor((double)(changeValue / CardanoUtility.adaOnlyMinUtxo));
            newChangeOutputs = Math.Min(newChangeOutputs, maxNewChangeOutputs);
            for (int i = 0; i < newChangeOutputs; i++)
            {
                var newOutput = new TransactionOutput()
                {
                    Address = new Address(changeAddress).GetBytes(),
                    Value = new TransactionOutputValue() { Coin = CardanoUtility.adaOnlyMinUtxo, MultiAsset = new Dictionary<byte[], NativeAsset>() },
                    OutputPurpose = OutputPurpose.Change
                };
                changeValue -= CardanoUtility.adaOnlyMinUtxo;
                coinSelection.ChangeOutputs.Add(newOutput);
            }
        }

        long changeValuePerOutput = changeValue / coinSelection.ChangeOutputs.Count;
        long changeValueRemainder = changeValue % coinSelection.ChangeOutputs.Count;
        long[] changeValues = new long[coinSelection.ChangeOutputs.Count];
        for (int i = 0; i < coinSelection.ChangeOutputs.Count; i++)
        {
            changeValues[i] = changeValuePerOutput;
            if (i == coinSelection.ChangeOutputs.Count - 1)
                changeValues[i] += changeValueRemainder;
        }

        for (int i = 0; i < coinSelection.ChangeOutputs.Count; i++)
            coinSelection.ChangeOutputs[i].Value.Coin += (ulong)changeValues[i];
    }
}
