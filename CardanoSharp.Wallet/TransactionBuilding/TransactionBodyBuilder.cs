using System;
using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.Utilities;

namespace CardanoSharp.Wallet.TransactionBuilding;

public interface ITransactionBodyBuilder : IABuilder<TransactionBody>
{
    ITransactionBodyBuilder AddInput(TransactionInput transactionInput);
    ITransactionBodyBuilder AddInput(Utxo utxo);
    ITransactionBodyBuilder AddInput(byte[] transactionId, uint transactionIndex, TransactionOutput? resolvedOutput = null);
    ITransactionBodyBuilder AddInput(string transactionId, uint transactionIndex, TransactionOutput? resolvedOutput = null);
    ITransactionBodyBuilder AddOutput(
        byte[] address,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    );
    ITransactionBodyBuilder AddOutput(
        Address address,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    );
    ITransactionBodyBuilder AddOutput(TransactionOutput transactionOutput);
    ITransactionBodyBuilder AddOutput(
        byte[] address,
        Utxo utxo,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    );
    ITransactionBodyBuilder AddBaseOutput(
        byte[] address,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    );

    ITransactionBodyBuilder AddBaseOutput(TransactionOutput transactionOutput);
    ITransactionBodyBuilder SetFee(ulong fee);
    ITransactionBodyBuilder SetValidBefore(uint validBeforeSlot);
    ITransactionBodyBuilder SetValidAfter(uint validAfterSlot);
    ITransactionBodyBuilder SetValidBefore(long validBeforeMilliseconds, NetworkType networkType);
    ITransactionBodyBuilder SetValidAfter(long validAfterMilliseconds, NetworkType networkType);
    ITransactionBodyBuilder AddCertificate(ICertificateBuilder certificateBuilder);
    ITransactionBodyBuilder SetCertificates(List<Certificate> certificates);
    ITransactionBodyBuilder SetWithdrawals(Dictionary<byte[], uint> withdrawals);
    ITransactionBodyBuilder SetMetadataHash(IAuxiliaryDataBuilder auxiliaryDataBuilder);
    ITransactionBodyBuilder SetMint(ITokenBundleBuilder token);
    ITransactionBodyBuilder AddMint(ITokenBundleBuilder token);
    ITransactionBodyBuilder SetScriptDataHash(byte[] scriptDataHash);
    ITransactionBodyBuilder SetScriptDataHash(List<Redeemer> redeemers, List<IPlutusData> datums);
    ITransactionBodyBuilder SetScriptDataHash(List<Redeemer> redeemers, List<IPlutusData> datums, byte[] languageViews);
    ITransactionBodyBuilder AddCollateralInput(TransactionInput transactionInput);
    ITransactionBodyBuilder AddCollateralInput(byte[] transactionId, uint transactionIndex);
    ITransactionBodyBuilder AddCollateralInput(string transactionIdStr, uint transactionIndex);
    ITransactionBodyBuilder AddRequiredSigner(byte[] requiredSigner);
    ITransactionBodyBuilder SetNetworkId(uint networkId);
    ITransactionBodyBuilder SetCollateralOutput(TransactionOutput transactionOutput);
    ITransactionBodyBuilder SetCollateralOutput(Address address, ulong coin);
    ITransactionBodyBuilder SetCollateralOutput(byte[] address, ulong coin);
    ITransactionBodyBuilder SetTotalCollateral(ulong TotalCollateral);
    ITransactionBodyBuilder AddReferenceInput(TransactionInput transactionInput);
    ITransactionBodyBuilder AddReferenceInput(Utxo utxo);
    ITransactionBodyBuilder AddReferenceInput(
        byte[] transactionId,
        uint transactionIndex,
        Address? address = null,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null
    );
    ITransactionBodyBuilder AddReferenceInput(
        string transactionIdStr,
        uint transactionIndex,
        Address? address = null,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null
    );

    // Get Functions
    IList<TransactionOutput> GetTransactionOutputs();
    ICollection<ICertificateBuilder> GetCertificates();
    ITokenBundleBuilder GetMint();

    // Helper Functions
    ITransactionBodyBuilder RemoveFeeFromChange(ulong? fee = null);
}

public class TransactionBodyBuilder : ABuilder<TransactionBody>, ITransactionBodyBuilder
{
    private TransactionBodyBuilder()
    {
        _model = new TransactionBody();
    }

    private TransactionBodyBuilder(TransactionBody model)
    {
        _model = model;
    }

    public static ITransactionBodyBuilder GetBuilder(TransactionBody model)
    {
        if (model == null)
        {
            return new TransactionBodyBuilder();
        }
        return new TransactionBodyBuilder(model);
    }

    public static ITransactionBodyBuilder Create
    {
        get => new TransactionBodyBuilder();
    }

    public ITransactionBodyBuilder AddInput(TransactionInput transactionInput)
    {
        _model.TransactionInputs.Add(transactionInput);
        return this;
    }

    public ITransactionBodyBuilder AddInput(Utxo utxo)
    {
        TransactionInput transactionInput = TransactionInputBuilder.Create
            .SetTransactionId(utxo.TxHash.HexToByteArray())
            .SetTransactionIndex(utxo.TxIndex)
            .SetOutput(
                TransactionOutputBuilder.Create
                    .SetOutputFromUtxo(new Address(utxo.OutputAddress).GetBytes(), utxo, utxo.OutputDatumOption, utxo.OutputScriptReference)
                    .Build()
            )
            .Build();
        return AddInput(transactionInput);
    }

    public ITransactionBodyBuilder AddInput(string transactionIdStr, uint transactionIndex, TransactionOutput? resolvedOutput = null)
    {
        return AddInput(transactionIdStr.HexToByteArray(), transactionIndex);
    }

    public ITransactionBodyBuilder AddInput(byte[] transactionId, uint transactionIndex, TransactionOutput? resolvedOutput = null)
    {
        _model.TransactionInputs.Add(
            new TransactionInput()
            {
                TransactionId = transactionId,
                TransactionIndex = transactionIndex,
                Output = resolvedOutput
            }
        );
        return this;
    }

    // Add output functions will automatically calculate the minUtxo
    public ITransactionBodyBuilder AddOutput(
        byte[] address,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    )
    {
        TransactionOutputBuilder transactionOutputBuilder = (TransactionOutputBuilder)
            TransactionOutputBuilder.Create.SetOutput(address, coin, tokenBundleBuilder, datumOption, scriptReference, outputPurpose);

        _model.TransactionOutputs.Add(transactionOutputBuilder.Build());
        return this;
    }

    public ITransactionBodyBuilder AddOutput(
        Address address,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    )
    {
        return AddOutput(address.GetBytes(), coin, tokenBundleBuilder, datumOption, scriptReference, outputPurpose);
    }

    public ITransactionBodyBuilder AddOutput(TransactionOutput transactionOutput)
    {
        TransactionOutputBuilder transactionOutputBuilder = (TransactionOutputBuilder)TransactionOutputBuilder.GetBuilder(transactionOutput);
        TokenBundleBuilder tokenBundleBuilder = (TokenBundleBuilder)TokenBundleBuilder.GetBuilder(transactionOutput.Value.MultiAsset);
        TransactionOutput minUtxoTransactionOutput = transactionOutputBuilder
            .SetOutput(
                transactionOutput.Address,
                transactionOutput.Value.Coin,
                tokenBundleBuilder,
                transactionOutput.DatumOption,
                transactionOutput.ScriptReference,
                transactionOutput.OutputPurpose
            )
            .Build();

        _model.TransactionOutputs.Add(minUtxoTransactionOutput);
        return this;
    }

    public ITransactionBodyBuilder AddOutput(
        byte[] address,
        Utxo utxo,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    )
    {
        TransactionOutputBuilder transactionOutputBuilder = (TransactionOutputBuilder)
            TransactionOutputBuilder.Create.SetOutputFromUtxo(address, utxo, datumOption, scriptReference, outputPurpose);

        _model.TransactionOutputs.Add(transactionOutputBuilder.Build());
        return this;
    }

    // Base Output does not calculate minUtxo
    public ITransactionBodyBuilder AddBaseOutput(
        byte[] address,
        ulong coin,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null,
        OutputPurpose outputPurpose = OutputPurpose.Spend
    )
    {
        var outputValue = new TransactionOutputValue() { Coin = coin };

        if (tokenBundleBuilder != null)
        {
            outputValue.MultiAsset = tokenBundleBuilder.Build();
        }

        var output = new TransactionOutput()
        {
            Address = address,
            Value = outputValue,
            OutputPurpose = outputPurpose
        };

        if (datumOption is not null)
            output.DatumOption = datumOption;

        if (scriptReference is not null)
            output.ScriptReference = scriptReference;

        _model.TransactionOutputs.Add(output);
        return this;
    }

    public ITransactionBodyBuilder AddBaseOutput(TransactionOutput transactionOutput)
    {
        _model.TransactionOutputs.Add(transactionOutput);
        return this;
    }

    public ITransactionBodyBuilder SetFee(ulong fee)
    {
        _model.Fee = fee;
        return this;
    }

    public ITransactionBodyBuilder SetValidBefore(uint validBeforeSlot)
    {
        _model.ValidBefore = validBeforeSlot;
        return this;
    }

    public ITransactionBodyBuilder SetValidAfter(uint validAfterSlot)
    {
        // This is the current slot number, and is the lower bound where as ttl (valid before) is the upper bound
        _model.ValidAfter = validAfterSlot;
        return this;
    }

    public ITransactionBodyBuilder SetValidBefore(long validBeforeMilliseconds, NetworkType networkType)
    {
        _model.ValidBefore = (uint?)SlotUtility.GetSlotFromUnixTime(SlotUtility.GetSlotNetworkConfig(networkType), validBeforeMilliseconds / 1000);
        return this;
    }

    public ITransactionBodyBuilder SetValidAfter(long validAfterMilliseconds, NetworkType networkType)
    {
        _model.ValidAfter = (uint?)SlotUtility.GetSlotFromUnixTime(SlotUtility.GetSlotNetworkConfig(networkType), validAfterMilliseconds / 1000);
        return this;
    }

    public ITransactionBodyBuilder SetCertificates(List<Certificate> certificates)
    {
        _model.Certificates = certificates;
        return this;
    }

    public ITransactionBodyBuilder AddCertificate(ICertificateBuilder certificateBuilder)
    {
        if (_model.Certificates is null)
            _model.Certificates = new List<Certificate>();

        _model.Certificates.Add(certificateBuilder.Build());
        return this;
    }

    public ITransactionBodyBuilder SetWithdrawals(Dictionary<byte[], uint> withdrawals)
    {
        _model.Withdrawls = withdrawals;
        return this;
    }

    public ITransactionBodyBuilder SetMetadataHash(IAuxiliaryDataBuilder auxiliaryDataBuilder)
    {
        _model.MetadataHash = HashUtility.Blake2b256(auxiliaryDataBuilder.Build().GetCBOR().EncodeToBytes()).ToStringHex();
        return this;
    }

    public ITransactionBodyBuilder SetMint(ITokenBundleBuilder tokenBuilder)
    {
        _model.Mint = tokenBuilder.Build();
        return this;
    }

    public ITransactionBodyBuilder AddMint(ITokenBundleBuilder tokenBuilder)
    {
        if (_model.Mint is null)
            return SetMint(tokenBuilder);

        var mintBuild = _model.Mint;
        Dictionary<byte[], NativeAsset> tokenBuild = tokenBuilder.Build();

        // Send them both through the byte[] to hex dict converter
        Dictionary<string, Dictionary<string, long>> mintBuildStringDict = TokenUtility.ConvertKeysToHexStrings(mintBuild);
        Dictionary<string, Dictionary<string, long>> tokenBuildStringDict = TokenUtility.ConvertKeysToHexStrings(tokenBuild);

        // Send them both to the merging function
        var mergedStringDict = TokenUtility.MergeStringDictionaries(mintBuildStringDict!, tokenBuildStringDict!);

        // Next reconvert the merged dict back to byte[] keys
        var mergedByteDict = TokenUtility.ConvertStringKeysToByteArrays(mergedStringDict);

        // Finally, add the merged byte dict to the model
        _model.Mint = mergedByteDict;

        return this;
    }

    public ITransactionBodyBuilder SetScriptDataHash(byte[] scriptDataHash)
    {
        _model.ScriptDataHash = scriptDataHash;
        return this;
    }

    public ITransactionBodyBuilder SetScriptDataHash(List<Redeemer> redeemers, List<IPlutusData> datums)
    {
        return SetScriptDataHash(redeemers, datums, CostModelUtility.PlutusV2CostModel.Serialize());
    }

    public ITransactionBodyBuilder SetScriptDataHash(List<Redeemer> redeemers, List<IPlutusData> datums, byte[] languageViews)
    {
        _model.ScriptDataHash = ScriptUtility.GenerateScriptDataHash(redeemers, datums, languageViews);
        return this;
    }

    public ITransactionBodyBuilder AddCollateralInput(TransactionInput transactionInput)
    {
        if (_model.Collateral is null)
        {
            _model.Collateral = new List<TransactionInput>();
        }

        _model.Collateral.Add(transactionInput);
        return this;
    }

    public ITransactionBodyBuilder AddCollateralInput(byte[] transactionId, uint transactionIndex)
    {
        if (_model.Collateral is null)
        {
            _model.Collateral = new List<TransactionInput>();
        }

        _model.Collateral.Add(new TransactionInput() { TransactionId = transactionId, TransactionIndex = transactionIndex });
        return this;
    }

    public ITransactionBodyBuilder AddCollateralInput(string transactionIdStr, uint transactionIndex)
    {
        if (_model.Collateral is null)
        {
            _model.Collateral = new List<TransactionInput>();
        }

        byte[] transactionId = transactionIdStr.HexToByteArray();
        _model.Collateral.Add(new TransactionInput() { TransactionId = transactionId, TransactionIndex = transactionIndex });
        return this;
    }

    public ITransactionBodyBuilder AddRequiredSigner(byte[] requiredSigner)
    {
        if (_model.RequiredSigners is null)
            _model.RequiredSigners = new List<byte[]>();

        if (!_model.RequiredSigners.Any(existingSigner => existingSigner.SequenceEqual(requiredSigner)))
            _model.RequiredSigners.Add(requiredSigner);

        return this;
    }

    public ITransactionBodyBuilder SetNetworkId(uint networkId)
    {
        _model.NetworkId = networkId;
        return this;
    }

    public ITransactionBodyBuilder SetCollateralOutput(TransactionOutput transactionOutput)
    {
        _model.CollateralReturn = transactionOutput;
        return this;
    }

    public ITransactionBodyBuilder SetCollateralOutput(Address address, ulong coin)
    {
        return SetCollateralOutput(address.GetBytes(), coin);
    }

    public ITransactionBodyBuilder SetCollateralOutput(byte[] address, ulong coin)
    {
        var outputValue = new TransactionOutputValue() { Coin = coin };

        var output = new TransactionOutput()
        {
            Address = address,
            Value = outputValue,
            OutputPurpose = OutputPurpose.Collateral
        };

        _model.CollateralReturn = output;
        return this;
    }

    public ITransactionBodyBuilder SetTotalCollateral(ulong totalCollateral)
    {
        _model.TotalCollateral = totalCollateral;
        return this;
    }

    public ITransactionBodyBuilder AddReferenceInput(TransactionInput transactionInput)
    {
        if (_model.ReferenceInputs is null)
            _model.ReferenceInputs = new List<TransactionInput>();

        if (!_model.ReferenceInputs.Contains(transactionInput, new TransactionEqualityInputComparer()))
            _model.ReferenceInputs.Add(transactionInput);

        return this;
    }

    public ITransactionBodyBuilder AddReferenceInput(Utxo utxo)
    {
        TransactionInput transactionInput = TransactionInputBuilder.Create
            .SetTransactionId(utxo.TxHash.HexToByteArray())
            .SetTransactionIndex(utxo.TxIndex)
            .SetOutput(
                TransactionOutputBuilder.Create
                    .SetOutputFromUtxo(
                        new Address(utxo.OutputAddress).GetBytes(),
                        utxo,
                        datumOption: utxo.OutputDatumOption,
                        scriptReference: utxo.OutputScriptReference
                    )
                    .Build()
            )
            .Build();
        return AddReferenceInput(transactionInput);
    }

    public ITransactionBodyBuilder AddReferenceInput(
        string transactionIdStr,
        uint transactionIndex,
        Address? address = null,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null
    )
    {
        return AddReferenceInput(
            transactionIdStr.HexToByteArray(),
            transactionIndex,
            address,
            coin,
            tokenBundleBuilder,
            datumOption,
            scriptReference
        );
    }

    public ITransactionBodyBuilder AddReferenceInput(
        byte[] transactionId,
        uint transactionIndex,
        Address? address = null,
        ulong coin = 0,
        ITokenBundleBuilder? tokenBundleBuilder = null,
        DatumOption? datumOption = null,
        ScriptReference? scriptReference = null
    )
    {
        if (_model.ReferenceInputs is null)
            _model.ReferenceInputs = new List<TransactionInput>();

        TransactionOutputBuilder transactionOutputBuilder = (TransactionOutputBuilder)TransactionOutputBuilder.Create;
        if (address is not null)
            transactionOutputBuilder.SetAddress(address.GetBytes());

        if (tokenBundleBuilder is not null)
            transactionOutputBuilder.SetTransactionOutputValue(new TransactionOutputValue { Coin = coin, MultiAsset = tokenBundleBuilder.Build() });
        else
            transactionOutputBuilder.SetTransactionOutputValue(new TransactionOutputValue { Coin = coin });

        if (datumOption is not null)
            transactionOutputBuilder.SetDatumOption(datumOption);
        if (scriptReference is not null)
            transactionOutputBuilder.SetScriptReference(scriptReference);

        TransactionInput transactionInput = TransactionInputBuilder.Create
            .SetTransactionId(transactionId)
            .SetTransactionIndex(transactionIndex)
            .SetOutput(transactionOutputBuilder.Build())
            .Build();

        if (!_model.ReferenceInputs.Contains(transactionInput, new TransactionEqualityInputComparer()))
            _model.ReferenceInputs.Add(transactionInput);

        return this;
    }

    // Get Functions
    public IList<TransactionOutput> GetTransactionOutputs()
    {
        return _model.TransactionOutputs;
    }

    public ICollection<ICertificateBuilder> GetCertificates()
    {
        List<ICertificateBuilder> certificateBuilders = new();
        if (_model.Certificates is null)
            return certificateBuilders;

        foreach (var c in _model.Certificates)
            certificateBuilders.Add(CertificateBuilder.GetBuilder(c));
        return certificateBuilders;
    }

    public ITokenBundleBuilder GetMint()
    {
        return TokenBundleBuilder.GetBuilder(_model.Mint);
    }

    // Helper Functions
    public ITransactionBodyBuilder RemoveFeeFromChange(ulong? fee = null)
    {
        if (fee is null)
            fee = _model.Fee;

        //get count of change outputs to deduct fee from evenly
        //note we are selecting only ones that dont have assets
        //  this is to respect minimum ada required for token bundles
        IEnumerable<TransactionOutput> changeOutputs;
        if (
            _model.TransactionOutputs.Any(
                x =>
                    x.OutputPurpose == OutputPurpose.Change
                    && (x.Value.MultiAsset is null || (x.Value.MultiAsset is not null && !x.Value.MultiAsset.Any()))
            )
        )
        {
            changeOutputs = _model.TransactionOutputs.Where(
                x =>
                    x.OutputPurpose == OutputPurpose.Change
                    && (x.Value.MultiAsset is null || (x.Value.MultiAsset is not null && !x.Value.MultiAsset.Any()))
            );
        }
        else
        {
            changeOutputs = _model.TransactionOutputs.Where(x => x.OutputPurpose == OutputPurpose.Change);
        }

        ulong feePerChangeOutput = fee.Value / (ulong)changeOutputs.Count();
        ulong feeRemaining = fee.Value % (ulong)changeOutputs.Count();
        bool needToApplyRemaining = true;
        foreach (var o in changeOutputs)
        {
            if (needToApplyRemaining)
            {
                o.Value.Coin = o.Value.Coin - feePerChangeOutput - feeRemaining;
                needToApplyRemaining = false;
            }
            else
                o.Value.Coin = o.Value.Coin - feePerChangeOutput;
        }

        return this;
    }
}
