using System;
using System.Collections.Generic;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.TransactionBuilding;

namespace CardanoSharp.Wallet.Advanced.AdvancedCoinSelection.Utilities;

public static class CollateralSelectionUtility
{
    public static TransactionBodyBuilder UseCollateralSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        List<Utxo> utxos,
        string changeAddress,
        ulong collateralAmount = 4000000, // Smart contract fee is max ~1.5 ada so this is more then 150% of the max fee
        ulong feeBuffer = 0,
        long maxTxSize = 12000
    )
    {
        TransactionBodyBuilder collateralTBB = (TransactionBodyBuilder)TransactionBodyBuilder.Create;
        collateralTBB.AddOutput(
            TransactionOutputBuilder.Create.SetTransactionOutputValue(new TransactionOutputValue { Coin = collateralAmount }).Build()
        );

        int maxCollateralInputs = 3;
        int maxCollateralOutputs = 1;
        CoinSelection? coinSelection = null;
        try
        {
            coinSelection = CoinSelectionUtility.CoinSelection(
                collateralTBB,
                utxos,
                changeAddress,
                limit: maxCollateralInputs,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize
            );
        }
        catch { }

        if (coinSelection == null || coinSelection.ChangeOutputs.Count > maxCollateralOutputs || coinSelection.Inputs.Count > maxCollateralInputs)
        {
            if (coinSelection == null)
                throw new Exception("Unable to build collateral for update transaction. Please ada another UTXO with ~5 ada to your wallet.");
            else if (coinSelection.ChangeOutputs.Count > maxCollateralOutputs)
                throw new Exception(
                    $"Unable to build collateral for transaction. Cannot have more than {maxCollateralOutputs} Collateral Output. Please add more ADA to your wallet."
                );
            else if (coinSelection.Inputs.Count > maxCollateralInputs)
                throw new Exception(
                    $"Unable to build collateral for transaction. Cannot have more than {maxCollateralInputs} Collateral Inputs. Please add more ADA to your wallet."
                );
            else
            {
                throw new Exception($"An error has occured in collateral selection");
            }
        }

        ulong totalCollateral = GetTotalCollateral(coinSelection);
        foreach (TransactionOutput collateralOutput in coinSelection.ChangeOutputs)
            transactionBodyBuilder.SetCollateralOutput(collateralOutput);
        foreach (TransactionInput collateralInput in coinSelection.Inputs)
            transactionBodyBuilder.AddCollateralInput(collateralInput);
        transactionBodyBuilder.SetTotalCollateral(totalCollateral);

        return transactionBodyBuilder;
    }

    private static ulong GetTotalCollateral(CoinSelection coinSelection)
    {
        ulong totalCollateral = 0;
        foreach (Utxo utxo in coinSelection.SelectedUtxos)
            totalCollateral += utxo.Balance.Lovelaces;

        foreach (TransactionOutput output in coinSelection.ChangeOutputs)
            totalCollateral -= output.Value.Coin;

        return totalCollateral;
    }
}
