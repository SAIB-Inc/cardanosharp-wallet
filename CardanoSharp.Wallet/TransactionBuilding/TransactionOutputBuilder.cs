using System;
using System.Collections.Generic;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.Utilities;

namespace CardanoSharp.Wallet.TransactionBuilding;

public interface ITransactionOutputBuilder : IABuilder<TransactionOutput>
{
    ITransactionOutputBuilder SetAddress(byte[] address);
    ITransactionOutputBuilder SetTransactionOutputValue(TransactionOutputValue value);
    ITransactionOutputBuilder SetDatumOption(DatumOption datumOption);
    ITransactionOutputBuilder SetScriptReference(ScriptReference scriptReference);
    ITransactionOutputBuilder SetOutputPurpose(OutputPurpose outputPurpose);

    // Advanced Helper Functions
    ITransactionOutputBuilder SetOutput(
        byte[] address,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    );

    ITransactionOutputBuilder SetOutputFromUtxo(
        byte[] address,
        Utxo utxo,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    );
}

public class TransactionOutputBuilder : ABuilder<TransactionOutput>, ITransactionOutputBuilder
{
    public TransactionOutputBuilder()
    {
        _model = new TransactionOutput();
    }

    private TransactionOutputBuilder(TransactionOutput model)
    {
        _model = model;
    }

    public static ITransactionOutputBuilder GetBuilder(TransactionOutput model)
    {
        if (model == null)
        {
            return new TransactionOutputBuilder();
        }
        return new TransactionOutputBuilder(model);
    }

    public static ITransactionOutputBuilder Create
    {
        get => new TransactionOutputBuilder();
    }

    public ITransactionOutputBuilder SetAddress(byte[] address)
    {
        _model.Address = address;
        return this;
    }

    public ITransactionOutputBuilder SetTransactionOutputValue(TransactionOutputValue value)
    {
        _model.Value = value;
        return this;
    }

    public ITransactionOutputBuilder SetDatumOption(DatumOption datumOption)
    {
        _model.DatumOption = datumOption;
        return this;
    }

    public ITransactionOutputBuilder SetScriptReference(ScriptReference scriptReference)
    {
        _model.ScriptReference = scriptReference;
        return this;
    }

    public ITransactionOutputBuilder SetOutputPurpose(OutputPurpose outputPurpose)
    {
        _model.OutputPurpose = outputPurpose;
        return this;
    }

    // Advanced Helper Functions
    public ITransactionOutputBuilder SetOutput(
        byte[] address,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    )
    {
        // First we create a transaction output builder with a dummy coin value
        ulong dummyCoin = (ulong)(CardanoUtility.adaOnlyMinUtxo); // We need a Dummy Coin for proper minUTXO calculation
        this.SetAddress(address).SetOutputPurpose(outputPurpose);

        if (tokenBundleBuilder is not null)
            this.SetTransactionOutputValue(new TransactionOutputValue { Coin = dummyCoin, MultiAsset = tokenBundleBuilder.Build() });
        else
            this.SetTransactionOutputValue(new TransactionOutputValue { Coin = dummyCoin });

        if (datumOption is not null)
            this.SetDatumOption(datumOption);
        if (scriptReference is not null)
            this.SetScriptReference(scriptReference);

        // Now we calculate the correct minUtxo coin value
        this.SetMinUtxo(coin);
        return this;
    }

    public ITransactionOutputBuilder SetOutputFromUtxo(
        byte[] address,
        Utxo utxo,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    )
    {
        Dictionary<string, Dictionary<string, long>> nativeAssetsNames = new Dictionary<string, Dictionary<string, long>>();
        foreach (var asset in utxo.Balance.Assets)
        {
            if (!nativeAssetsNames.ContainsKey(asset.PolicyId))
            {
                Dictionary<string, long> tokenName = new Dictionary<string, long>() { { asset.Name, asset.Quantity } };
                nativeAssetsNames.Add(asset.PolicyId, tokenName);
            }
            else
            {
                nativeAssetsNames[asset.PolicyId].Add(asset.Name, asset.Quantity);
            }
        }

        // Convert to Byte Array Dictionary
        Dictionary<byte[], NativeAsset> nativeAssets = new Dictionary<byte[], NativeAsset>();
        foreach (var nativeAssetPair in nativeAssetsNames)
        {
            byte[] policyId = nativeAssetPair.Key.HexToByteArray();
            Dictionary<byte[], long> tokenValue = new Dictionary<byte[], long>();
            foreach (var tokenPair in nativeAssetPair.Value)
            {
                byte[] tokenKey = tokenPair.Key.HexToByteArray();
                tokenValue.Add(tokenKey, tokenPair.Value);
            }
            NativeAsset nativeAsset = new NativeAsset { Token = tokenValue };
            nativeAssets.Add(policyId, nativeAsset);
        }

        this.SetAddress(address)
            .SetTransactionOutputValue(new TransactionOutputValue { Coin = utxo.Balance.Lovelaces, MultiAsset = nativeAssets })
            .SetOutputPurpose(outputPurpose);

        if (datumOption is not null)
            this.SetDatumOption(datumOption);

        if (scriptReference is not null)
            this.SetScriptReference(scriptReference);

        this.SetMinUtxo(utxo.Balance.Lovelaces);
        return this;
    }

    // MinUtxo Helper Function
    private ITransactionOutputBuilder SetMinUtxo(ulong coin)
    {
        // Now we calculate the correct minUtxo coin value
        var transactionOutput = this.Build();
        ulong finalCoin = Math.Max(transactionOutput.CalculateMinUtxoLovelace(), coin);

        // Set the correct minUtxo value
        if (transactionOutput.Value.MultiAsset is not null)
            this.SetTransactionOutputValue(new TransactionOutputValue { Coin = finalCoin, MultiAsset = transactionOutput.Value.MultiAsset });
        else
            this.SetTransactionOutputValue(new TransactionOutputValue { Coin = finalCoin });
        return this;
    }
}
