using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CardanoSharp.Wallet.Advanced.AdvancedCoinSelection.Enums;
using CardanoSharp.Wallet.Advanced.AdvancedCoinSelection.Utilities;
using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Extensions.Models.Transactions.TransactionWitnesses;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.Providers;
using CardanoSharp.Wallet.Utilities;
using CsBindgen;

namespace CardanoSharp.Wallet.TransactionBuilding;

public static class TransactionBuilderExtensions
{
    //---------------------------------------------------------------------------------------------------//
    // Transaction Creation Functions
    //---------------------------------------------------------------------------------------------------//

    // Complete function with simplified interface, the goal of this function is to eventually have no parameters
    public static async Task<(Transaction, TransactionEvaluation)> Complete(
        this ITransactionBuilder transactionBuilder,
        AProviderService providerService,
        Address address,
        List<Utxo>? candidateUtxos = null,
        List<Utxo>? requiredUtxos = null,
        List<Utxo>? spentUtxos = null,
        TxChainingType txChainingType = TxChainingType.None
    )
    {
        TokenBundleBuilder tokenBundleBuilder = (TokenBundleBuilder)transactionBuilder.transactionBodyBuilder.GetMint();
        List<Redeemer> redeemers = transactionBuilder.transactionWitnessesBuilder.GetRedeemers();

        return await FullComplete(
            transactionBuilder,
            providerService,
            address,
            tokenBundleBuilder,
            candidateUtxos,
            requiredUtxos,
            spentUtxos,
            txChainingType: txChainingType,
            isSmartContract: redeemers.Count > 0
        );
    }

    // Complete function with all parameters
    public static async Task<(Transaction, TransactionEvaluation)> FullComplete(
        this ITransactionBuilder transactionBuilder,
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
        bool isSmartContract = false,
        int signerCount = 2
    )
    {
        TransactionBodyBuilder transactionBodyBuilder = (TransactionBodyBuilder)transactionBuilder.transactionBodyBuilder;
        await transactionBodyBuilder.UseCoinSelection(
            providerService: providerService,
            address: address,
            mint: mint,
            candidateUtxos: candidateUtxos,
            requiredUtxos: requiredUtxos,
            spentUtxos: spentUtxos,
            limit: limit,
            feeBuffer: feeBuffer,
            maxTxSize: maxTxSize,
            txChainingType: txChainingType,
            isSmartContract: isSmartContract
        );

        Transaction transaction = transactionBuilder.Build();
        if (transaction.TransactionBody.ValidBefore <= 0)
            transactionBodyBuilder.SetValidBefore((uint)(providerService.ProviderData.Tip + 1 * 60 * 60));
        if (transaction.TransactionBody.ValidAfter <= 0)
            transactionBodyBuilder.SetValidAfter((uint)providerService.ProviderData.Tip);
        transactionBuilder.SetBodyBuilder(transactionBodyBuilder);

        var fullTransaction = transactionBuilder.SimpleComplete(
            providerService.ProviderData.ProtocolParameters!,
            providerService.ProviderData.NetworkType,
            signerCount
        );
        return fullTransaction;
    }

    // Complete Function for when coin selection has already occured
    public static (Transaction, TransactionEvaluation) SimpleComplete(
        this ITransactionBuilder transactionBuilder,
        ProtocolParameters protocolParameters,
        NetworkType networkType,
        int signerCount = 2
    )
    {
        // Calculate transaction variables
        Transaction transaction = transactionBuilder.Build();
        List<TransactionInput> transactionInputs = (List<TransactionInput>)transaction.TransactionBody.TransactionInputs;
        List<TransactionInput>? collateralInputs = (List<TransactionInput>?)transaction.TransactionBody.Collateral;

        // Calculate the number of payment keys used
        HashSet<string> uniqueAddresses = new();
        foreach (TransactionInput transactionInput in transactionInputs)
            if (transactionInput?.Output?.Address != null)
                uniqueAddresses.Add(transactionInput.Output.Address.ToString()!);

        if (collateralInputs != null)
            foreach (TransactionInput collateralInput in collateralInputs)
                if (collateralInput.Output?.Address != null)
                    uniqueAddresses.Add(collateralInput.Output.Address.ToString()!);
        signerCount += uniqueAddresses.Count;

        bool isSmartContractTx = transaction.TransactionWitnessSet.Redeemers.Count > 0;
        if (isSmartContractTx)
            return CompleteSmartContractTransaction(transactionBuilder, protocolParameters, networkType, signerCount);

        return CompleteSimpleTransaction(transactionBuilder, protocolParameters, signerCount);
    }

    private static (Transaction, TransactionEvaluation) CompleteSmartContractTransaction(
        this ITransactionBuilder transactionBuilder,
        ProtocolParameters protocolParameters,
        NetworkType networkType,
        int signerCount = 1
    )
    {
        TransactionBodyBuilder transactionBodyBuilder = (TransactionBodyBuilder)transactionBuilder.transactionBodyBuilder;
        TransactionWitnessSetBuilder transactionWitnessSetBuilder = (TransactionWitnessSetBuilder)transactionBuilder.transactionWitnessesBuilder;
        AuxiliaryDataBuilder auxiliaryDataBuilder = (AuxiliaryDataBuilder)transactionBuilder.auxDataBuilder;

        // Mock Key Signers for Fee Calculation and ExUnit Calculation
        if (transactionWitnessSetBuilder == null)
            transactionWitnessSetBuilder = (TransactionWitnessSetBuilder)TransactionWitnessSetBuilder.Create;
        transactionWitnessSetBuilder.MockVKeyWitness(signerCount);

        // Set Dummy Value for script data hash for evaluation calculation
        List<IPlutusData> datums = new() { };
        List<Redeemer> redeemers = new() { };
        transactionBodyBuilder.SetScriptDataHash(redeemers, datums, CostModelUtility.PlutusV2CostModel.Serialize());

        // Build Transaction and set dummy fee for evaluate calculation
        Transaction transaction = transactionBuilder.Build();

        // Set Dummy fee for evaluation calculation
        var dummyFee = transaction.CalculateFee();
        transaction.TransactionBody.Fee = dummyFee;

        // Evaluation Transaction Ex Units
        transaction.SetRedeemerIndices(); // Set Redeemer Indices if they have Utxos
        TransactionEvaluation evaluation = UPLCMethods.GetExUnits(transaction, networkType);
        transaction.SetExUnits(evaluation);

        // Re-add the script data hash to account for updated redeemers
        // Ensure the Redeemers are sorted by index, this is not required by the Node but Lucid automatically sorts them when signing so we need to be compatible
        datums = transaction.TransactionWitnessSet.PlutusDatas.ToList();
        redeemers = transaction.TransactionWitnessSet.Redeemers.ToList();
        redeemers.Sort((x, y) => x.Index.CompareTo(y.Index));
        transaction.TransactionWitnessSet.Redeemers = redeemers;
        transactionBodyBuilder.SetScriptDataHash(redeemers, datums, CostModelUtility.PlutusV2CostModel.Serialize());

        // Calculate and Set Fee
        var fee = transaction.CalculateFee(
            protocolParameters.MinFeeA,
            protocolParameters.MinFeeB,
            protocolParameters.PriceMem,
            protocolParameters.PriceStep
        );
        transaction.TransactionBody.Fee = fee;
        ulong lastOutputChangeBalance = transaction.TransactionBody.TransactionOutputs.Last().Value.Coin;
        transaction.TransactionBody.TransactionOutputs.Last().Value.Coin = lastOutputChangeBalance - (ulong)fee;

        // Remove Transaction Mocks
        if (transaction.TransactionWitnessSet != null)
            transaction.TransactionWitnessSet.RemoveMocks();

        return (transaction, evaluation)!;
    }

    private static (Transaction, TransactionEvaluation) CompleteSimpleTransaction(
        this ITransactionBuilder transactionBuilder,
        ProtocolParameters protocolParameters,
        int signerCount = 1
    )
    {
        TransactionBodyBuilder transactionBodyBuilder = (TransactionBodyBuilder)transactionBuilder.transactionBodyBuilder;
        TransactionWitnessSetBuilder transactionWitnessSetBuilder = (TransactionWitnessSetBuilder)transactionBuilder.transactionWitnessesBuilder;
        AuxiliaryDataBuilder auxiliaryDataBuilder = (AuxiliaryDataBuilder)transactionBuilder.auxDataBuilder;

        // Mock Key Signers for fee calculation and Build Metadata for the tokens
        if (transactionWitnessSetBuilder == null)
            transactionWitnessSetBuilder = (TransactionWitnessSetBuilder)TransactionWitnessSetBuilder.Create;
        transactionWitnessSetBuilder.MockVKeyWitness(signerCount);

        // Build the transaction to finalize
        transactionBuilder.SetBody(transactionBodyBuilder).SetWitnesses(transactionWitnessSetBuilder);
        Transaction transaction = transactionBuilder.Build();

        // Calculate and Set Fee
        var fee = transaction.CalculateFee(protocolParameters.MinFeeA, protocolParameters.MinFeeB);

        transaction.TransactionBody.Fee = fee;
        ulong lastOutputChangeBalance = transaction.TransactionBody.TransactionOutputs.Last().Value.Coin;
        transaction.TransactionBody.TransactionOutputs.Last().Value.Coin = lastOutputChangeBalance - (ulong)fee;

        // Remove Transaction Mocks
        if (transaction.TransactionWitnessSet != null)
            transaction.TransactionWitnessSet.RemoveMocks();

        return (transaction, null)!;
    }

    //---------------------------------------------------------------------------------------------------//
}
