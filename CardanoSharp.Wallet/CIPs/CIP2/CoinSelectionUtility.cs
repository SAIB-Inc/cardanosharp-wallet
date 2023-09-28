using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.CIPs.CIP2.ChangeCreationStrategies;
using CardanoSharp.Wallet.CIPs.CIP2.Extensions;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.TransactionBuilding;

namespace CardanoSharp.Wallet.CIPs.CIP2;

public static class CoinSelectionUtility
{
    public static CoinSelection UseLargestFirst(
        this TransactionBodyBuilder tbb,
        List<Utxo> utxos,
        string changeAddress,
        ITokenBundleBuilder? mint = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 20,
        ulong feeBuffer = 0
    )
    {
        var cs = new CoinSelectionService(new LargestFirstStrategy(), new BasicChangeSelectionStrategy());
        var tb = tbb.Build();
        return cs.GetCoinSelection(tb.TransactionOutputs.ToList(), utxos, changeAddress, mint, requiredUtxos, limit, feeBuffer);
    }

    public static CoinSelection UseRandomImprove(
        this TransactionBodyBuilder tbb,
        List<Utxo> utxos,
        string changeAddress,
        ITokenBundleBuilder? mint = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 20,
        ulong feeBuffer = 0
    )
    {
        var cs = new CoinSelectionService(new RandomImproveStrategy(), new BasicChangeSelectionStrategy());
        var tb = tbb.Build();
        return cs.GetCoinSelection(tb.TransactionOutputs.ToList(), utxos, changeAddress, mint, requiredUtxos, limit, feeBuffer);
    }

    public static CoinSelection UseAll(
        this TransactionBodyBuilder tbb,
        List<Utxo> utxos,
        string changeAddress,
        TokenBundleBuilder? mint = null,
        int limit = 20,
        ulong feeBuffer = 0
    )
    {
        CoinSelection coinSelection = new();

        int utxoIndex = 0;
        foreach (Utxo utxo in utxos)
        {
            if (utxoIndex > limit)
                break;

            coinSelection.SelectedUtxos.Add(utxo);
            utxoIndex += 1;
        }

        var outputs = tbb.Build().TransactionOutputs.ToList();
        var balance = outputs.AggregateAssets(mint!);
        BasicChangeSelectionStrategy basicChangeSelectionStrategy = new();
        basicChangeSelectionStrategy.CalculateChange(coinSelection, balance, changeAddress);

        foreach (var su in coinSelection.SelectedUtxos)
            coinSelection.Inputs.Add(
                new TransactionInput()
                {
                    TransactionId = su.TxHash.HexToByteArray(),
                    TransactionIndex = su.TxIndex,
                    Output =
                        su.OutputAddress != null
                            ? TransactionOutputBuilder.Create
                                .SetOutputFromUtxo(new Address(su.OutputAddress).GetBytes(), su, su.OutputDatumOption, su.OutputScriptReference)
                                .Build()
                            : null
                }
            );

        return coinSelection;
    }
}
