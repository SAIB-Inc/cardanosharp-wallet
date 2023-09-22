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
using CardanoSharp.Wallet.Providers.Blockfrost;
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
        List<Utxo>? candidateUtxos = null,
        List<Utxo>? requiredUtxos = null,
        List<Utxo>? spentUtxos = null,
        int limit = 120,
        ulong feeBuffer = 1000000,
        long maxTxSize = 12000,
        TxChainingType txChainingType = TxChainingType.None,
        bool isSmartContract = false
    )
    {
        if (isSmartContract)
            await transactionBodyBuilder.UseCoinAndCollateralSelection(
                providerService,
                address,
                mint,
                candidateUtxos: candidateUtxos,
                requiredUtxos: requiredUtxos,
                spentUtxos: spentUtxos,
                limit,
                feeBuffer,
                maxTxSize,
                txChainingType
            );
        else
            await transactionBodyBuilder.UseStandardCoinSelection(
                providerService,
                address,
                mint,
                candidateUtxos: candidateUtxos,
                requiredUtxos: requiredUtxos,
                spentUtxos: spentUtxos,
                limit,
                feeBuffer,
                maxTxSize,
                txChainingType
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
        List<Utxo>? candidateUtxos = null,
        List<Utxo>? requiredUtxos = null,
        List<Utxo>? spentUtxos = null,
        int limit = 120,
        ulong feeBuffer = 0,
        long maxTxSize = 12000,
        TxChainingType txChainingType = TxChainingType.None
    )
    {
        string paymentAddress = address.ToString();
        (HashSet<Utxo> inputUtxos, HashSet<Utxo> outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(providerService, paymentAddress);
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
                requiredUtxos: requiredUtxos,
                limit: limit,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize
            );
            return transactionBodyBuilder;
        }
        catch { }

        // First and Last Address Utxos
        string? lastAddress = await providerService.GetMainAddress(paymentAddress, "desc");
        if (lastAddress != null && !AddressUtility.IsSmartContractAddress(lastAddress))
        {
            (inputUtxos, outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(providerService, paymentAddress, inputUtxos, outputUtxos);
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
                requiredUtxos: requiredUtxos,
                limit: limit,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize
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
            requiredUtxos: requiredUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize
        );
        return transactionBodyBuilder;
    }

    public static async Task<TransactionBodyBuilder> UseCoinAndCollateralSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        AProviderService providerService,
        Address address,
        TokenBundleBuilder? mint = null,
        List<Utxo>? candidateUtxos = null,
        List<Utxo>? requiredUtxos = null,
        List<Utxo>? spentUtxos = null,
        int limit = 20,
        ulong feeBuffer = 0,
        long maxTxSize = 12000,
        TxChainingType txChainingType = TxChainingType.None
    )
    {
        string paymentAddress = address.ToString();
        (HashSet<Utxo> inputUtxos, HashSet<Utxo> outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(providerService, paymentAddress);
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
                requiredUtxos: requiredUtxos,
                limit: limit,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize
            );
            return transactionBodyBuilder;
        }
        catch { }

        // First and last Address Utxos
        string? lastAddress = await providerService.GetMainAddress(paymentAddress, "desc");
        if (lastAddress != null)
        {
            (inputUtxos, outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(providerService, paymentAddress, inputUtxos, outputUtxos);
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
                requiredUtxos: requiredUtxos,
                limit: limit,
                feeBuffer: feeBuffer,
                maxTxSize: maxTxSize
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
            requiredUtxos: requiredUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize
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
        List<Utxo>? requiredUtxos = null,
        int limit = 20,
        ulong feeBuffer = 0,
        long maxTxSize = 12000
    )
    {
        CoinSelection? coinSelection = CoinSelection(
            transactionBodyBuilder,
            utxos,
            paymentAddress,
            mint: mint,
            requiredUtxos: requiredUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize
        );
        if (coinSelection == null)
            throw new InsufficientFundsException(
                "Not enough ada or NFTs in wallet to build transaction. Please add more ada or wait for your pending transactions to resolve on chain"
            );

        transactionBodyBuilder.UseCollateralSelection(
            utxos,
            paymentAddress,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize - GetCoinSelectionSize(coinSelection)
        );

        // Set Inputs and outputs
        foreach (TransactionOutput changeOutput in coinSelection.ChangeOutputs)
            transactionBodyBuilder.AddOutput(changeOutput);

        foreach (TransactionInput input in coinSelection.Inputs)
            transactionBodyBuilder.AddInput(input);

        return transactionBodyBuilder;
    }

    private static TransactionBodyBuilder UseStandardCoinSelection(
        this TransactionBodyBuilder transactionBodyBuilder,
        List<Utxo> utxos,
        string paymentAddress,
        TokenBundleBuilder? mint = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 120,
        ulong feeBuffer = 0,
        long maxTxSize = 12000
    )
    {
        CoinSelection? coinSelection = CoinSelection(
            transactionBodyBuilder,
            utxos,
            paymentAddress,
            mint: mint,
            requiredUtxos: requiredUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize
        );
        if (coinSelection == null)
            throw new InsufficientFundsException(
                "Not enough ada or NFTs in wallet to build transaction. Please add more ada or wait for your pending transactions to resolve on chain"
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
        int limit = 120,
        ulong feeBuffer = 0
    )
    {
        CoinSelection coinSelection = transactionBodyBuilder.UseAll(utxos, changeAddress, mint: mint, limit: limit, feeBuffer: feeBuffer);

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
        List<Utxo>? requiredUtxos = null,
        int limit = 120,
        ulong feeBuffer = 0,
        long maxTxSize = 12000
    )
    {
        // Filter out all required utxos from utxos
        if (requiredUtxos != null && requiredUtxos.Count > 0)
        {
            var requiredUtxosSet = new HashSet<Utxo>(requiredUtxos);
            utxos.RemoveAll(u => requiredUtxosSet.Contains(u));
        }

        // If random improve fails, fallback to largest first
        CoinSelection? coinSelection = null;
        try
        {
            coinSelection = transactionBodyBuilder.UseRandomImprove(utxos, changeAddress, mint!, requiredUtxos, limit, feeBuffer);
            long totalSize = GetCoinSelectionSize(coinSelection);
            if (totalSize > maxTxSize)
                coinSelection = transactionBodyBuilder.UseLargestFirst(utxos, changeAddress, mint!, requiredUtxos, limit, feeBuffer);

            return coinSelection;
        }
        catch { }

        try
        {
            coinSelection = transactionBodyBuilder.UseLargestFirst(utxos, changeAddress, mint!, requiredUtxos, limit, feeBuffer);
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
        List<Utxo>? spentUtxos = null
    )
    {
        (HashSet<Utxo> inputUtxos, HashSet<Utxo> outputUtxos) = await TransactionChainingUtility.GetMempoolUtxos(providerService, paymentAddress);
        HashSet<Utxo> spentUtxoSet = new();
        if (spentUtxos != null)
            spentUtxoSet = new HashSet<Utxo>(spentUtxos);

        List<Utxo> blockfrostUtxos = await providerService.GetSingleAddressUtxos(paymentAddress);
        List<Utxo> initialCandidateUtxos = TransactionChainingUtility.TxChainingUtxos(
            paymentAddress,
            blockfrostUtxos,
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
