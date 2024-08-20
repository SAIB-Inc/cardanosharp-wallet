using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardanoSharp.Wallet.Advanced.AdvancedCoinSelection.Enums;
using CardanoSharp.Wallet.CIPs.CIP2;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Providers;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoSharp.Wallet.Utilities;
using CoinSelection = CardanoSharp.Wallet.CIPs.CIP2.Models.CoinSelection;
using Transaction = CardanoSharp.Wallet.Models.Transactions.Transaction;

namespace CardanoSharp.Wallet.Advanced.AdvancedCoinSelection.Utilities;

public static class CoinSelectionUtility
{
    public static async Task<TransactionBodyBuilder> UseCoinSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        AProviderService providerService,
        Address address,
        TokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        List<Utxo>? candidateUtxos = null,
        List<Utxo>? requiredUtxos = null,
        List<Utxo>? spentUtxos = null,
        HashSet<Utxo>? inputMempoolUtxos = null,
        HashSet<Utxo>? outputMempoolUtxos = null,
        int limit = 120,
        ulong feeBuffer = 1000000,
        long maxTxSize = 12000,
        TxChainingType txChainingType = TxChainingType.None,
        CoinSelectionType coinSelectionType = CoinSelectionType.OptimizedRandomImprove,
        ChangeSelectionType changeSelectionType = ChangeSelectionType.MultiSplit,
        DateTime? filterAfterTime = null,
        bool isSmartContract = false
    )
    {
        if (isSmartContract)
            await transactionBodyBuilder.UseCoinAndCollateralSelection(
                providerService,
                address,
                mint,
                certificates,
                candidateUtxos: candidateUtxos,
                requiredUtxos: requiredUtxos,
                spentUtxos: spentUtxos,
                inputMempoolUtxos: inputMempoolUtxos,
                outputMempoolUtxos: outputMempoolUtxos,
                limit,
                feeBuffer,
                maxTxSize,
                txChainingType,
                coinSelectionType,
                changeSelectionType,
                filterAfterTime
            );
        else
            await transactionBodyBuilder.UseStandardCoinSelection(
                providerService,
                address,
                mint,
                certificates,
                candidateUtxos: candidateUtxos,
                requiredUtxos: requiredUtxos,
                spentUtxos: spentUtxos,
                inputMempoolUtxos: inputMempoolUtxos,
                outputMempoolUtxos: outputMempoolUtxos,
                limit,
                feeBuffer,
                maxTxSize,
                txChainingType,
                coinSelectionType,
                changeSelectionType,
                filterAfterTime
            );
        return transactionBodyBuilder;
    }

    //---------------------------------------------------------------------------------------------------//
    // Coin Selection Functions
    //---------------------------------------------------------------------------------------------------//
    public static async Task<TransactionBodyBuilder> UseStandardCoinSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        AProviderService providerService,
        Address address,
        TokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        List<Utxo>? candidateUtxos = null,
        List<Utxo>? requiredUtxos = null,
        List<Utxo>? spentUtxos = null,
        HashSet<Utxo>? inputMempoolUtxos = null,
        HashSet<Utxo>? outputMempoolUtxos = null,
        int limit = 120,
        ulong feeBuffer = 0,
        long maxTxSize = 12000,
        TxChainingType txChainingType = TxChainingType.None,
        CoinSelectionType coinSelectionType = CoinSelectionType.OptimizedRandomImprove,
        ChangeSelectionType changeSelectionType = ChangeSelectionType.MultiSplit,
        DateTime? filterAfterTime = null
    )
    {
        string paymentAddress = address.ToString();
        HashSet<Utxo> inputUtxos = inputMempoolUtxos ?? new();
        HashSet<Utxo> outputUtxos = outputMempoolUtxos ?? new();
        if (inputMempoolUtxos == null || outputMempoolUtxos == null)
        {
            (inputUtxos, outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(
                providerService,
                paymentAddress,
                inputUtxos,
                outputUtxos,
                filterAfterTime: filterAfterTime
            );
        }

        HashSet<Utxo> spentUtxoSet = new();
        if (spentUtxos != null)
            spentUtxoSet = new HashSet<Utxo>(spentUtxos);

        // First Address
        List<Utxo> utxos;
        if (candidateUtxos != null && candidateUtxos.Count > 0)
            utxos = TransactionChainingUtility.TxChainingUtxos(paymentAddress, candidateUtxos, inputUtxos, outputUtxos, spentUtxoSet, txChainingType);
        else
            utxos = TransactionChainingUtility.TxChainingUtxos(
                paymentAddress,
                await providerService.GetSingleAddressUtxos(paymentAddress),
                inputUtxos,
                outputUtxos,
                spentUtxoSet,
                txChainingType
            );
        try
        {
            transactionBodyBuilder.UseStandardCoinSelection(
                utxos,
                paymentAddress,
                mint: mint,
                certificates: certificates,
                requiredUtxos: requiredUtxos,
                limit: limit,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize,
                coinSelectionType: coinSelectionType,
                changeSelectionType: changeSelectionType
            );
            return transactionBodyBuilder;
        }
        catch { }

        // First and Last Address Utxos
        string? lastAddress = await providerService.GetMainAddress(paymentAddress, "desc");
        if (lastAddress != null && lastAddress != paymentAddress && !AddressUtility.IsSmartContractAddress(lastAddress))
        {
            (inputUtxos, outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(
                providerService,
                lastAddress,
                inputUtxos,
                outputUtxos,
                filterAfterTime: filterAfterTime
            );
            List<Utxo> singleFirstAndLastAddressUtxos = TransactionChainingUtility.TxChainingUtxos(
                lastAddress,
                await providerService.GetSingleAddressUtxos(lastAddress),
                inputUtxos,
                outputUtxos,
                spentUtxoSet,
                txChainingType
            );
            utxos.AddRange(singleFirstAndLastAddressUtxos);
        }
        try
        {
            transactionBodyBuilder.UseStandardCoinSelection(
                utxos,
                paymentAddress,
                mint: mint,
                certificates: certificates,
                requiredUtxos: requiredUtxos,
                limit: limit,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize,
                coinSelectionType: coinSelectionType,
                changeSelectionType: changeSelectionType
            );
            return transactionBodyBuilder;
        }
        catch { }

        // All Address Utxos
        // If there is not enough ada in the first and last addresses, get all UTXOs from the wallet.
        utxos = TransactionChainingUtility.TxChainingUtxos(
            paymentAddress,
            await providerService.GetUtxos(paymentAddress, true),
            inputUtxos,
            outputUtxos,
            spentUtxoSet,
            txChainingType
        );
        transactionBodyBuilder.UseStandardCoinSelection(
            utxos,
            paymentAddress,
            mint: mint,
            certificates: certificates,
            requiredUtxos: requiredUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize,
            coinSelectionType: coinSelectionType,
            changeSelectionType: changeSelectionType
        );
        return transactionBodyBuilder;
    }

    public static async Task<TransactionBodyBuilder> UseCoinAndCollateralSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        AProviderService providerService,
        Address address,
        TokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        List<Utxo>? candidateUtxos = null,
        List<Utxo>? requiredUtxos = null,
        List<Utxo>? spentUtxos = null,
        HashSet<Utxo>? inputMempoolUtxos = null,
        HashSet<Utxo>? outputMempoolUtxos = null,
        int limit = 20,
        ulong feeBuffer = 0,
        long maxTxSize = 12000,
        TxChainingType txChainingType = TxChainingType.None,
        CoinSelectionType coinSelectionType = CoinSelectionType.OptimizedRandomImprove,
        ChangeSelectionType changeSelectionType = ChangeSelectionType.MultiSplit,
        DateTime? filterAfterTime = null
    )
    {
        string paymentAddress = address.ToString();
        HashSet<Utxo> inputUtxos = inputMempoolUtxos ?? new();
        HashSet<Utxo> outputUtxos = outputMempoolUtxos ?? new();
        if (inputMempoolUtxos == null || outputMempoolUtxos == null)
        {
            (inputUtxos, outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(
                providerService,
                paymentAddress,
                inputUtxos,
                outputUtxos,
                filterAfterTime: filterAfterTime
            );
        }

        HashSet<Utxo> spentUtxoSet = new();
        if (spentUtxos != null)
            spentUtxoSet = new HashSet<Utxo>(spentUtxos);

        // First Address
        List<Utxo> utxos;
        if (candidateUtxos != null && candidateUtxos.Count > 0)
            utxos = TransactionChainingUtility.TxChainingUtxos(paymentAddress, candidateUtxos, inputUtxos, outputUtxos, spentUtxoSet, txChainingType);
        else
            utxos = TransactionChainingUtility.TxChainingUtxos(
                paymentAddress,
                await providerService.GetSingleAddressUtxos(paymentAddress),
                inputUtxos,
                outputUtxos,
                spentUtxoSet,
                txChainingType
            );
        try
        {
            transactionBodyBuilder.UseCoinAndCollateralSelection(
                utxos,
                paymentAddress,
                mint: mint,
                certificates: certificates,
                requiredUtxos: requiredUtxos,
                limit: limit,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize,
                coinSelectionType: coinSelectionType,
                changeSelectionType: changeSelectionType
            );
            return transactionBodyBuilder;
        }
        catch { }

        // First and last Address Utxos
        string? lastAddress = await providerService.GetMainAddress(paymentAddress, "desc");
        if (lastAddress != null && lastAddress != paymentAddress && !AddressUtility.IsSmartContractAddress(lastAddress))
        {
            (inputUtxos, outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(
                providerService,
                lastAddress,
                inputUtxos,
                outputUtxos,
                filterAfterTime: filterAfterTime
            );
            List<Utxo> singleFirstAndLastAddressUtxos = TransactionChainingUtility.TxChainingUtxos(
                lastAddress,
                await providerService.GetSingleAddressUtxos(lastAddress),
                inputUtxos,
                outputUtxos,
                spentUtxoSet,
                txChainingType
            );
            utxos.AddRange(singleFirstAndLastAddressUtxos);
        }
        try
        {
            transactionBodyBuilder.UseCoinAndCollateralSelection(
                utxos,
                paymentAddress,
                mint: mint,
                certificates: certificates,
                requiredUtxos: requiredUtxos,
                limit: limit,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize,
                coinSelectionType: coinSelectionType,
                changeSelectionType: changeSelectionType
            );
            return transactionBodyBuilder;
        }
        catch { }

        // All Address Utxos
        // If there is not enough ada in the first and last addresses, get all UTXOs from the wallet.
        // This functions gets Utxos from an address that has the same stake key hash as the payment address.
        utxos = TransactionChainingUtility.TxChainingUtxos(
            paymentAddress,
            await providerService.GetUtxos(paymentAddress, true),
            inputUtxos,
            outputUtxos,
            spentUtxoSet,
            txChainingType
        );
        transactionBodyBuilder.UseCoinAndCollateralSelection(
            utxos,
            paymentAddress,
            mint: mint,
            certificates: certificates,
            requiredUtxos: requiredUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize,
            coinSelectionType: coinSelectionType,
            changeSelectionType: changeSelectionType
        );
        return transactionBodyBuilder;
    }

    //---------------------------------------------------------------------------------------------------//
    // Coin Selection Helper Functions
    //---------------------------------------------------------------------------------------------------//
    private static TransactionBodyBuilder UseCoinAndCollateralSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        List<Utxo> utxos,
        string paymentAddress,
        TokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 20,
        ulong feeBuffer = 0,
        long maxTxSize = 12000,
        CoinSelectionType coinSelectionType = CoinSelectionType.OptimizedRandomImprove,
        ChangeSelectionType changeSelectionType = ChangeSelectionType.MultiSplit
    )
    {
        CoinSelection? coinSelection = CoinSelection(
            transactionBodyBuilder,
            utxos,
            paymentAddress,
            mint: mint,
            certificates: certificates,
            requiredUtxos: requiredUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize,
            coinSelectionType: coinSelectionType,
            changeSelectionType: changeSelectionType
        );
        if (coinSelection == null)
            throw new InsufficientFundsException(
                "Not enough ada or tokens in wallet to build transaction. Please add more ada or wait for your pending transactions to resolve on chain"
            );

        // Set Inputs and outputs
        foreach (TransactionOutput changeOutput in coinSelection.ChangeOutputs)
            transactionBodyBuilder.AddOutput(changeOutput);

        foreach (TransactionInput input in coinSelection.Inputs)
            transactionBodyBuilder.AddInput(input);

        // Calculate Dummy Fee to estimate collateral
        TransactionBuilder dummyFeeTransactionBuilder = (TransactionBuilder)TransactionBuilder.Create;
        TransactionWitnessSetBuilder transactionWitnessSetBuilder = (TransactionWitnessSetBuilder)TransactionWitnessSetBuilder.Create;
        transactionWitnessSetBuilder.MockVKeyWitness(2);
        dummyFeeTransactionBuilder.SetBodyBuilder(transactionBodyBuilder).SetWitnessesBuilder(transactionWitnessSetBuilder);
        long dummyFee = dummyFeeTransactionBuilder.Build().CalculateFee();

        // Estimate Smart Contracts. 2 Steps
        int smartContractEstimate = 0;

        // 1) Estimate Mints, we assume 1 smart contract execution per mint
        Dictionary<byte[], NativeAsset>? nativeAssets = mint?.Build();
        if (nativeAssets != null)
        {
            foreach (var nativeAssetPair in nativeAssets)
            {
                var tokens = nativeAssetPair.Value.Token;
                foreach (var tokenPair in tokens)
                {
                    // A Smart Contract is executed for both mint and burn so ignore the quantity
                    smartContractEstimate += 1;
                }
            }
        }

        // 2) Each required utxo is a smart contract execution
        smartContractEstimate += requiredUtxos?.Count ?? 0;

        // Estimate Collateral
        long contractCollateral = (long)(0.25 * CardanoUtility.adaToLovelace * smartContractEstimate); // Smart Contracts are usually required Utxos, so we should add extra collateral for those
        ulong estimateCollateral = (ulong)(4 * dummyFee + contractCollateral); // Normally collateral is 150% of the fee, but for our estimate we will use 400%

        // Set Collateral
        transactionBodyBuilder.UseCollateralSelection(
            utxos,
            paymentAddress,
            collateralAmount: estimateCollateral,
            maxTxSize: maxTxSize - GetCoinSelectionSize(coinSelection)
        );

        return transactionBodyBuilder;
    }

    private static TransactionBodyBuilder UseStandardCoinSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        List<Utxo> utxos,
        string paymentAddress,
        TokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 120,
        ulong feeBuffer = 0,
        long maxTxSize = 12000,
        CoinSelectionType coinSelectionType = CoinSelectionType.OptimizedRandomImprove,
        ChangeSelectionType changeSelectionType = ChangeSelectionType.MultiSplit
    )
    {
        CoinSelection? coinSelection = CoinSelection(
            transactionBodyBuilder,
            utxos,
            paymentAddress,
            mint: mint,
            certificates: certificates,
            requiredUtxos: requiredUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize,
            coinSelectionType: coinSelectionType,
            changeSelectionType: changeSelectionType
        );
        if (coinSelection == null)
            throw new InsufficientFundsException(
                "Not enough ada or tokens in wallet to build transaction. Please add more ada or wait for your pending transactions to resolve on chain"
            );

        // Set Inputs and outputs
        foreach (TransactionOutput changeOutput in coinSelection.ChangeOutputs)
            transactionBodyBuilder.AddOutput(changeOutput);

        foreach (TransactionInput input in coinSelection.Inputs)
            transactionBodyBuilder.AddInput(input);

        return transactionBodyBuilder;
    }

    public static TransactionBodyBuilder UseAllCoinSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        List<Utxo> utxos,
        string changeAddress,
        TokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        int limit = 120,
        ulong feeBuffer = 0
    )
    {
        CoinSelection coinSelection = transactionBodyBuilder.UseAll(
            utxos,
            changeAddress,
            mint: mint,
            certificates: certificates,
            limit: limit,
            feeBuffer: feeBuffer
        );

        // Set Inputs and outputs
        foreach (TransactionOutput changeOutput in coinSelection.ChangeOutputs)
            transactionBodyBuilder.AddBaseOutput(changeOutput);

        foreach (TransactionInput input in coinSelection.Inputs)
            transactionBodyBuilder.AddInput(input);

        return transactionBodyBuilder;
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Standard Coin Selection Functions
    //---------------------------------------------------------------------------------------------------//
    public static CoinSelection? CoinSelection(
        TransactionBodyBuilder transactionBodyBuilder,
        List<Utxo> utxos,
        string changeAddress,
        TokenBundleBuilder? mint = null,
        List<ICertificateBuilder>? certificates = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 120,
        ulong feeBuffer = 0,
        long maxTxSize = 12000,
        CoinSelectionType coinSelectionType = CoinSelectionType.OptimizedRandomImprove,
        ChangeSelectionType changeSelectionType = ChangeSelectionType.MultiSplit
    )
    {
        // Filter out all required utxos from utxos
        if (requiredUtxos != null && requiredUtxos.Count > 0)
        {
            var requiredUtxosSet = new HashSet<Utxo>(requiredUtxos);
            utxos.RemoveAll(requiredUtxosSet.Contains);
        }

        // If random improve fails, fallback to largest first
        CoinSelection? coinSelection = null;
        try
        {
            if (coinSelectionType == CoinSelectionType.OptimizedRandomImprove)
                coinSelection = transactionBodyBuilder.UseOptimizedRandomImprove(
                    utxos,
                    changeAddress,
                    mint!,
                    certificates,
                    requiredUtxos,
                    limit,
                    feeBuffer,
                    changeSelectionType
                );
            else if (coinSelectionType == CoinSelectionType.LargestFirst)
                coinSelection = transactionBodyBuilder.UseLargestFirst(
                    utxos,
                    changeAddress,
                    mint!,
                    certificates,
                    requiredUtxos,
                    limit,
                    feeBuffer,
                    changeSelectionType
                );
            else if (coinSelectionType == CoinSelectionType.RandomImprove)
                coinSelection = transactionBodyBuilder.UseRandomImprove(
                    utxos,
                    changeAddress,
                    mint!,
                    certificates,
                    requiredUtxos,
                    limit,
                    feeBuffer,
                    changeSelectionType
                );
            else if (coinSelectionType == CoinSelectionType.All)
                coinSelection = transactionBodyBuilder.UseAll(utxos, changeAddress, mint!, certificates, limit, feeBuffer);
            else
                coinSelection = transactionBodyBuilder.UseOptimizedRandomImprove(
                    utxos,
                    changeAddress,
                    mint!,
                    certificates,
                    requiredUtxos,
                    limit,
                    feeBuffer,
                    changeSelectionType
                );

            long totalSize = GetCoinSelectionSize(coinSelection);
            if (totalSize > maxTxSize)
                coinSelection = transactionBodyBuilder.UseLargestFirst(utxos, changeAddress, mint!, certificates, requiredUtxos, limit, feeBuffer);

            return coinSelection;
        }
        catch { }

        try
        {
            coinSelection = transactionBodyBuilder.UseLargestFirst(utxos, changeAddress, mint!, certificates, requiredUtxos, limit, feeBuffer);
        }
        catch { }

        return coinSelection;
    }

    private static long GetCoinSelectionSize(CoinSelection coinSelection)
    {
        long totalSize = 0;
        foreach (TransactionInput transactionInput in coinSelection.Inputs)
            totalSize += transactionInput.Serialize().Length;

        foreach (TransactionOutput transactionOutput in coinSelection.ChangeOutputs)
            totalSize += transactionOutput.Serialize().Length;

        return totalSize;
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Helper Functions
    //---------------------------------------------------------------------------------------------------//
    public async static Task<List<Utxo>> CalculateInitialCandidates(
        AProviderService providerService,
        string paymentAddress,
        List<Utxo>? spentUtxos = null,
        DateTime? filterAfterTime = null
    )
    {
        (HashSet<Utxo> inputUtxos, HashSet<Utxo> outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(
            providerService,
            paymentAddress,
            filterAfterTime: filterAfterTime
        );
        HashSet<Utxo> spentUtxoSet = new();
        if (spentUtxos != null)
            spentUtxoSet = new HashSet<Utxo>(spentUtxos);

        List<Utxo> singleAddressUtxos = await providerService.GetSingleAddressUtxos(paymentAddress);
        List<Utxo> initialCandidateUtxos = TransactionChainingUtility.TxChainingUtxos(
            paymentAddress,
            singleAddressUtxos,
            inputUtxos,
            outputUtxos,
            spentUtxoSet,
            TxChainingType.Chain
        );
        return initialCandidateUtxos;
    }

    public static List<Utxo> CalculateNewCandidates(string paymentAddress, List<Utxo> candidateUtxos, List<Transaction> transactions)
    {
        List<Utxo> newCandidates = new();
        List<Utxo> tempCandidates = new();
        tempCandidates.AddRange(candidateUtxos);

        // Get Spent inputs and new Outputs into a Hashset
        HashSet<Utxo> inputs = new();
        HashSet<Utxo> outputs = new();
        foreach (Transaction transaction in transactions)
        {
            if (transaction == null)
                continue;

            foreach (TransactionInput input in transaction.TransactionBody!.TransactionInputs)
            {
                Utxo utxo = new() { TxHash = input.TransactionId.ToStringHex(), TxIndex = input.TransactionIndex };
                inputs.Add(utxo);
            }

            string transactionId = HashUtility.Blake2b256(transaction.TransactionBody.Serialize(transaction.AuxiliaryData!)).ToStringHex();
            uint outputIndex = 0;
            foreach (TransactionOutput output in transaction.TransactionBody.TransactionOutputs)
            {
                Utxo utxo =
                    new()
                    {
                        TxHash = transactionId,
                        TxIndex = outputIndex,
                        Balance = output.Value.GetBalance(),
                        OutputAddress = new Address(output.Address).ToString()!,
                        OutputDatumOption = output.DatumOption
                    };
                outputs.Add(utxo);

                outputIndex += 1;
            }
        }

        foreach (Utxo utxo in outputs)
        {
            string address = new Address(utxo.OutputAddress).ToString()!;
            if (address != paymentAddress)
                continue;

            tempCandidates.Add(utxo);
        }

        foreach (Utxo tempCandidate in tempCandidates)
        {
            if (inputs.Contains(tempCandidate))
                continue;

            newCandidates.Add(tempCandidate);
        }

        return newCandidates;
    }

    //---------------------------------------------------------------------------------------------------//
}
