using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Extensions.Models.Transactions.TransactionWitnesses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.Utilities;
using CsBindgen;

namespace CardanoSharp.Wallet.TransactionBuilding
{
    public static class TransactionBuilderExtensions
    {
        //---------------------------------------------------------------------------------------------------//
        // Transaction Creation Functions
        //---------------------------------------------------------------------------------------------------//
        public static (Transaction, TransactionEvaluation) Complete(
            this ITransactionBuilder transactionBuilder,
            ProtocolParameters protocolParameters,
            NetworkType networkType,
            int signerCount = 1
        )
        {
            // Calculate transaction variables
            Transaction transaction = transactionBuilder.Build();
            List<TransactionInput> transactionInputs = (List<TransactionInput>)transaction.TransactionBody.TransactionInputs;
            List<TransactionInput>? collateralInputs = (List<TransactionInput>?)transaction.TransactionBody.Collateral;

            // Calculate the number of payment keys used
            HashSet<string> uniqueAddresses = new HashSet<string>();
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
            List<IPlutusData> datums = new List<IPlutusData>() { };
            List<Redeemer> redeemers = new List<Redeemer>() { };
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
}
