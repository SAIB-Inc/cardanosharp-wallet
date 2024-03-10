using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Utilities;

namespace CardanoSharp.Wallet.CIPs.CIP2;

public interface IOptimizedRandomImproveStrategy : ICoinSelectionStrategy { }

public class OptimizedRandomImproveStrategy : BaseSelectionStrategy, IRandomImproveStrategy
{
    public void SelectInputs(
        CoinSelection coinSelection,
        List<Utxo> availableUtxos,
        long amount,
        Asset? asset = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 20
    )
    {
        // 1. Randomly select utxos
        var rand = new Random();

        // Determine the current balance and filter and reorder the utxo list
        long currentAmount = GetCurrentBalance(coinSelection, asset);

        // If we already have enough of this asset, return
        if (currentAmount >= amount)
            return;

        // Filter and order the available utxos
        List<Utxo> filteredAvailableUtxos = FilterUtxosByAsset(availableUtxos, asset);
        List<Utxo> descendingAvailableUtxos = OrderUtxosByDescending(filteredAvailableUtxos, asset);

        // Set a minimum limit for the number of utxos to select. This helps clean up dust and uses small utxos for collateral selection
        int baseMinimumLimit = asset != null ? 1 : 3; // If we are selecting a specific asset, we only need 1 utxo minimum
        int minimumLimit = Math.Min(limit, baseMinimumLimit);

        // Create a temporary selected utxo list
        var currentSelectedUtxos = new List<Utxo>();
        while ((currentAmount < amount || currentSelectedUtxos.Count < minimumLimit) && descendingAvailableUtxos.Any())
        {
            // Make sure we havent added too many utxos
            if (currentSelectedUtxos.Count + coinSelection.SelectedUtxos.Count >= limit)
            {
                // If it is not enough amount, clear the current and existing selections
                if (currentAmount < amount)
                {
                    currentSelectedUtxos.Clear();
                    coinSelection.Clear();
                    SelectRequiredInputs(coinSelection, requiredUtxos);
                    currentAmount = GetCurrentBalance(coinSelection, asset);
                }
                else
                    break;
            }

            var availableLength = descendingAvailableUtxos.Count;
            var randomIndex = rand.Next(availableLength - 1);
            var randomUtxo = descendingAvailableUtxos[randomIndex];

            currentSelectedUtxos.Add(randomUtxo);
            descendingAvailableUtxos.RemoveAt(randomIndex);

            // Get the quantity of the asset in the utxo
            var quantity =
                (asset is null)
                    ? (long)randomUtxo.Balance.Lovelaces
                    : randomUtxo.Balance.Assets.FirstOrDefault(x => x.PolicyId.SequenceEqual(asset.PolicyId) && x.Name.Equals(asset.Name))!.Quantity;

            // Increment current amount by the Utxo quantity
            currentAmount += quantity;
        }

        // 2. Improve by expanding selection
        // This is the previous algorithm: https://cips.cardano.org/cips/cip2/#random-improve
        // However, this algorithm assumes that if the numbers of inputs is less then the number of outputs, the algorithm will fail.
        // This is not acceptable for enterprise applications.

        // For this new algorithm, we will attempt to reduce the size of the transaction by swapping utxos if it would create a smaller acceptable transaction

        // Step 1:
        // We will create 2 lists of OptimizeContainers, one for the selected utxos and one for the remaining utxos
        // These OptimizeContainers store the amount of the asset we are selecting as well as the number of other assets in the utxo
        // We will loop through sort the selectedUtxos by the number of assets descending and the reamining utxos by the number of assets ascending

        // Step 2:
        // We will then loop through the remaining utxos and swap them with the selected utxos if the remaining utxo has less assets then the selected utxo and is still above the requiredAmount
        // This will allow us to minimize the number of other assets while maximizing the quantity of the asset up to the amount

        // Step 3:
        // Finally we filter the Utxo list to remove any Utxos that are not required to meet the amount

        // Step 1
        List<OptimizeContainer> selectedUtxosContainers = new();
        List<OptimizeContainer> remainingUtxosContainers = new();
        foreach (Utxo selectedUtxo in currentSelectedUtxos)
            selectedUtxosContainers.Add(new OptimizeContainer(selectedUtxo, asset));
        foreach (Utxo remainingUtxo in descendingAvailableUtxos)
            remainingUtxosContainers.Add(new OptimizeContainer(remainingUtxo, asset));

        // Step 2: Double pass optimize utxo selection
        selectedUtxosContainers = selectedUtxosContainers.OrderByDescending(x => x.numAssetCount).ToList();
        remainingUtxosContainers = remainingUtxosContainers.OrderBy(x => x.numAssetCount).ToList();
        currentAmount = OptimizeSelectedUtxos(selectedUtxosContainers, remainingUtxosContainers, amount, currentAmount);

        selectedUtxosContainers = selectedUtxosContainers.OrderByDescending(x => x.numAssetCount).ToList();
        remainingUtxosContainers = remainingUtxosContainers.OrderBy(x => x.numAssetCount).ToList();
        currentAmount = OptimizeSelectedUtxos(selectedUtxosContainers, remainingUtxosContainers, amount, currentAmount);

        // Step 3: Filter utxo optimization
        selectedUtxosContainers = FilterSelectedUtxos(selectedUtxosContainers, amount, currentAmount, asset);

        // Add the final selected Utxos to the coin selection and remove them from the available utxos
        List<Utxo> finalSelectedUtxos = selectedUtxosContainers.Select(x => x.utxo).ToList();
        coinSelection.SelectedUtxos.AddRange(finalSelectedUtxos);
        finalSelectedUtxos.ForEach(x => availableUtxos.Remove(x));
    }

    protected static long OptimizeSelectedUtxos(
        List<OptimizeContainer> selectedUtxos,
        List<OptimizeContainer> remainingUtxos,
        long requiredAmount,
        long currentAmount
    )
    {
        long newAmount = currentAmount;
        int selectedUtxoIndex = 0;
        for (int i = 0; i < remainingUtxos.Count; i++)
        {
            OptimizeContainer remainingUtxo = remainingUtxos[i];
            OptimizeContainer selectedUtxo = selectedUtxos[selectedUtxoIndex];

            // If the remaining utxo has less assets then the selected utxo and is still above the requiredAmount, then swap them
            bool hasLessAssets = remainingUtxo.numAssetCount < selectedUtxo.numAssetCount;
            bool isGreaterThenRequiredAmount = newAmount - selectedUtxo.assetAmount + remainingUtxo.assetAmount >= requiredAmount;
            if (hasLessAssets && isGreaterThenRequiredAmount)
            {
                selectedUtxos[selectedUtxoIndex] = remainingUtxo;
                remainingUtxos[i] = selectedUtxo;
                newAmount = newAmount - selectedUtxo.assetAmount + remainingUtxo.assetAmount;

                selectedUtxoIndex += 1;
                if (selectedUtxoIndex >= selectedUtxos.Count)
                    break;
            }
        }
        return newAmount;
    }

    public static List<OptimizeContainer> FilterSelectedUtxos(
        List<OptimizeContainer> selectedUtxos,
        long requiredAmount,
        long currentAmount,
        Asset? asset = null
    )
    {
        // Add a slightly adjusted required amount to account for change minUtxo
        long adjustedRequiredAmount = asset != null ? 0 : requiredAmount + CardanoUtility.adaOnlyMinUtxo;
        List<OptimizeContainer> removeUtxos = new();

        selectedUtxos = selectedUtxos.OrderByDescending(x => x.numAssetCount).ToList();
        long newAmount = currentAmount;
        for (int i = 0; i < selectedUtxos.Count; i++)
        {
            OptimizeContainer selectedUtxo = selectedUtxos[i];
            if (newAmount - selectedUtxo.assetAmount > adjustedRequiredAmount)
            {
                newAmount -= selectedUtxo.assetAmount;
                removeUtxos.Add(selectedUtxo);
            }
        }

        selectedUtxos = selectedUtxos.Except(removeUtxos).ToList();
        return selectedUtxos;
    }

    public class OptimizeContainer
    {
        public Utxo utxo { get; set; } = default!;
        public long assetAmount { get; set; }
        public long numAssetCount { get; set; }

        public OptimizeContainer(Utxo utxo, Asset? asset)
        {
            this.utxo = utxo;
            this.assetAmount =
                (asset is null)
                    ? (long)utxo.Balance.Lovelaces
                    : utxo.Balance.Assets.FirstOrDefault(x => x.PolicyId.SequenceEqual(asset.PolicyId) && x.Name.Equals(asset.Name))!.Quantity;
            this.numAssetCount = 1 + utxo.Balance.Assets.Count;
        }
    }

    public void SelectRequiredInputs(CoinSelection coinSelection, List<Utxo>? requiredUtxos)
    {
        SelectRequiredUtxos(coinSelection, requiredUtxos);
    }
}
