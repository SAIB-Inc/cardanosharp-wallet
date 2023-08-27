using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Advanced.AdvancedCoinSelection.Enums;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.Providers;
using CardanoSharp.Wallet.Utilities;

namespace CardanoSharp.Wallet.Advanced.AdvancedCoinSelection.Utilities;

public static class TransactionChainingUtility
{
    //---------------------------------------------------------------------------------------------------//
    // Chaining Functions
    //---------------------------------------------------------------------------------------------------//
    public static List<Utxo> TxChainingUtxos(
        string address,
        List<Utxo> candidateUtxos,
        HashSet<Utxo> inputUtxos,
        HashSet<Utxo> outputUtxos,
        HashSet<Utxo> spentUtxos,
        TxChainingType txChainingType = TxChainingType.Filter
    )
    {
        // Add all Utxos that are not inputs or outputs of the previous tx. We are adding outputs back later to ensure no duplicate utxos are added
        List<Utxo> utxos = new();
        if (txChainingType == TxChainingType.Filter || txChainingType == TxChainingType.Chain)
        {
            foreach (Utxo utxo in candidateUtxos)
            {
                if (inputUtxos.Contains(utxo) || outputUtxos.Contains(utxo) || spentUtxos.Contains(utxo))
                    continue;

                utxos.Add(utxo);
            }
        }

        // Add all Utxos that are outputs of the previous tx to this address
        if (txChainingType == TxChainingType.Chain)
        {
            foreach (Utxo utxo in outputUtxos)
            {
                if (utxo.OutputAddress != address || spentUtxos.Contains(utxo))
                    continue;

                utxos.Add(utxo);
            }
        }

        return utxos;
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Mempool Functions
    //---------------------------------------------------------------------------------------------------//
    public static async Task<(HashSet<Utxo>, HashSet<Utxo>)> GetMempoolUtxos(
        AProviderService providerService,
        string address,
        HashSet<Utxo>? currentInputUtxos = null,
        HashSet<Utxo>? currentOutputUtxos = null
    )
    {
        // Determine if the wallet has any pending transactions
        MempoolTransactionHash[]? mempoolTxHashes = (await providerService.MempoolClient.GetMempoolAddressTransactionsAsync(address))?.Content;
        List<string> pendingTxHashes = mempoolTxHashes?.Select(mempoolTxHash => mempoolTxHash.TxHash).ToList()!;

        HashSet<Utxo> inputUtxos = new();
        HashSet<Utxo> outputUtxos = new();
        if (pendingTxHashes != null && pendingTxHashes.Count > 0)
        {
            var mempoolTransactions = await providerService.GetMempoolTransactions(pendingTxHashes);
            (inputUtxos, outputUtxos) = GetUtxosFromMempoolTransactions(mempoolTransactions);
        }

        if (currentInputUtxos != null)
            inputUtxos.UnionWith(currentInputUtxos);

        if (currentOutputUtxos != null)
            outputUtxos.UnionWith(currentOutputUtxos);

        return (inputUtxos, outputUtxos);
    }

    private static (HashSet<Utxo> inputUtxos, HashSet<Utxo> outputUtxos) GetUtxosFromMempoolTransactions(MempoolTransaction[] mempoolTransactions)
    {
        HashSet<Utxo> inputUtxos = new();
        HashSet<Utxo> outputUtxos = new();
        foreach (MempoolTransaction mempoolTransaction in mempoolTransactions)
        {
            if (mempoolTransaction.Inputs != null)
            {
                foreach (var input in mempoolTransaction.Inputs)
                {
                    if (input.Collateral || input.Reference)
                        continue;

                    Utxo inputUtxo = new() { TxHash = input.TxHash!, TxIndex = (uint)input.OutputIndex };
                    inputUtxos.Add(inputUtxo);
                }
            }

            if (mempoolTransaction.Outputs != null)
            {
                foreach (var output in mempoolTransaction.Outputs)
                {
                    if (output.Collateral)
                        continue;

                    Utxo outputUtxo = GetUtxoFromMempoolOutput(mempoolTransaction.Tx.Hash!, output);
                    outputUtxos.Add(outputUtxo);
                }
            }
        }
        return (inputUtxos, outputUtxos);
    }

    private static Utxo GetUtxoFromMempoolOutput(string txHash, MempoolTransaction.Output output)
    {
        ulong lovelaces = 0;
        List<Asset> assets = new();
        if (output.Amount != null)
        {
            foreach (var amount in output.Amount)
            {
                if (amount.Unit == "lovelace")
                    lovelaces += ulong.Parse(amount.Quantity);
                else
                    assets.Add(
                        new Asset
                        {
                            PolicyId = AssetUtility.GetHexPolicyId(amount.Unit),
                            Name = AssetUtility.GetHexAssetName(amount.Unit),
                            Quantity = long.Parse(amount.Quantity)
                        }
                    );
            }
        }

        DatumOption? datumOption =
            output.InlineDatum != null ? DatumOptionExtension.DeserializeFromInlineDatum(output.InlineDatum!.HexToByteArray()) : null;
        Utxo utxo =
            new()
            {
                TxHash = txHash,
                TxIndex = output.OutputIndex,
                Balance = { Lovelaces = lovelaces, Assets = assets },
                OutputAddress = output.Address!,
                OutputDatumOption = datumOption
            };
        return utxo;
    }
    //---------------------------------------------------------------------------------------------------//
}
