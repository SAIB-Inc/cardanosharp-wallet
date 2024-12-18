﻿using System;
using System.Collections.Generic;
using System.Linq;
using CardanoSharp.Wallet.Extensions.Models.Certificates;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Utilities;
using PeterO.Cbor2;

namespace CardanoSharp.Wallet.Extensions.Models.Transactions;

public static class TransactionBodyExtensions
{
    public static CBORObject GetCBOR(this TransactionBody transactionBody, AuxiliaryData? auxiliaryData)
    {
        CBORObject cborBody = CBORObject.NewMap();

        CBORObject cborInputs = null!;
        CBORObject cborOutputs = null!;
        CBORObject cborCollateralInputs = null!;
        CBORObject cborRequiredSigners = null!;
        CBORObject cborReferenceInputs = null!;

        // 0) Transaction Inputs
        if (transactionBody.TransactionInputs.Any())
        {
            cborInputs = CBORObject.NewArray().WithTag(258);
            foreach (var txInput in transactionBody.TransactionInputs)
            {
                cborInputs.Add(txInput.GetCBOR());
            }
        }

        if (cborInputs != null)
            cborBody.Add(0, cborInputs);

        // 1) Transaction Outputs
        if (transactionBody.TransactionOutputs.Any())
        {
            cborOutputs = CBORObject.NewArray();
            foreach (var txOutput in transactionBody.TransactionOutputs)
            {
                cborOutputs.Add(txOutput.GetCBOR());
            }
        }

        if (cborOutputs != null)
            cborBody.Add(1, cborOutputs);

        // 2) Fee
        cborBody.Add(2, transactionBody.Fee);

        // 3) TTL
        if (transactionBody.ValidBefore.HasValue)
            cborBody.Add(3, transactionBody.ValidBefore.Value);

        // 4) Certificates
        if (transactionBody.Certificates != null && transactionBody.Certificates.Any())
        {
            CBORObject cborTransactionCertificates = CBORObject.NewArray().WithTag(258);
            foreach (var certificate in transactionBody.Certificates)
                cborTransactionCertificates.Add(certificate.GetCBOR());
            cborBody.Add(4, cborTransactionCertificates);
        }

        // 5) Withdrawals
        if (transactionBody.Withdrawls != null && transactionBody.Withdrawls.Any())
        {
            CBORObject cborTransactionWithdrawals = CBORObject.NewMap();
            foreach (var withdrawal in transactionBody.Withdrawls)
                cborTransactionWithdrawals.Add(withdrawal.Key, withdrawal.Value);
            cborBody.Add(5, cborTransactionWithdrawals);
        }

        // 6) Update

        // 7) Metadata
        if (auxiliaryData != null || transactionBody.MetadataHash != default)
        {
            if (auxiliaryData != null)
            {
                cborBody.Add(7, HashUtility.Blake2b256(auxiliaryData.Serialize()));
            }
            else if (transactionBody.MetadataHash != default)
            {
                cborBody.Add(7, transactionBody.MetadataHash.HexToByteArray());
            }
        }

        // 8) validity interval start
        if (transactionBody.ValidAfter.HasValue)
            cborBody.Add(8, transactionBody.ValidAfter.Value);

        // 9) add tokens for minting
        if (transactionBody.Mint.Any())
        {
            CBORObject cborTransactionMint = CBORObject.NewMap();
            foreach (var nativeAsset in transactionBody.Mint)
            {
                var assetCbor = CBORObject.NewMap();
                foreach (var asset in nativeAsset.Value.Token)
                {
                    assetCbor.Add(asset.Key, asset.Value);
                }
                cborTransactionMint.Add(nativeAsset.Key, assetCbor);
            }
            cborBody.Add(9, cborTransactionMint);
        }

        // 11) script_data_hash
        if (transactionBody.ScriptDataHash != null)
        {
            cborBody.Add(11, transactionBody.ScriptDataHash);
        }

        // 13) collateral_inputs
        if (transactionBody.Collateral != null && transactionBody.Collateral.Any())
        {
            cborCollateralInputs = CBORObject.NewArray().WithTag(258);
            foreach (var txInput in transactionBody.Collateral)
            {
                cborCollateralInputs.Add(txInput.GetCBOR());
            }
        }
        if (cborCollateralInputs != null)
            cborBody.Add(13, cborCollateralInputs);

        // 14) required_signers
        if (transactionBody.RequiredSigners != null && transactionBody.RequiredSigners.Any())
        {
            cborRequiredSigners = CBORObject.NewArray().WithTag(258);
            foreach (var requireSigners in transactionBody.RequiredSigners)
            {
                cborRequiredSigners.Add(requireSigners);
            }
        }
        if (cborRequiredSigners != null)
            cborBody.Add(14, cborRequiredSigners);

        // 15) network_id
        if (transactionBody.NetworkId != null && transactionBody.NetworkId.HasValue)
        {
            cborBody.Add(15, transactionBody.NetworkId);
        }

        // 16) collateral return
        if (transactionBody.CollateralReturn != null)
        {
            cborBody.Add(16, transactionBody.CollateralReturn.GetCBOR());
        }

        // 17) total collateral
        if (transactionBody.TotalCollateral != null && transactionBody.TotalCollateral.HasValue)
        {
            cborBody.Add(17, transactionBody.TotalCollateral);
        }

        // 18) reference inputs
        if (transactionBody.ReferenceInputs != null && transactionBody.ReferenceInputs.Any())
        {
            cborReferenceInputs = CBORObject.NewArray().WithTag(258);
            foreach (var referenceInput in transactionBody.ReferenceInputs)
            {
                cborReferenceInputs.Add(referenceInput.GetCBOR());
            }
        }

        if (cborReferenceInputs != null)
        {
            cborBody.Add(18, cborReferenceInputs);
        }

        return cborBody;
    }

    public static TransactionBody GetTransactionBody(this CBORObject transactionBodyCbor)
    {
        //validation
        if (transactionBodyCbor == null)
        {
            throw new ArgumentNullException(nameof(transactionBodyCbor));
        }
        if (transactionBodyCbor.Type != CBORType.Map)
        {
            throw new ArgumentException("transactionBodyCbor is not expected type CBORType.Map");
        }
        if (!transactionBodyCbor.ContainsKey(0))
        {
            throw new ArgumentException("transactionBodyCbor key 0 (Inputs) not present");
        }
        if (!transactionBodyCbor.ContainsKey(1))
        {
            throw new ArgumentException("transactionBodyCbor key 1 (Outputs) not present");
        }
        if (!transactionBodyCbor.ContainsKey(2))
        {
            throw new ArgumentException("transactionBodyCbor key 2 (Fee/Coin) not present");
        }
        else if (transactionBodyCbor[2].Type != CBORType.Integer)
        {
            throw new ArgumentException("transactionBodyCbor element 2 (Fee/Coin) unexpected type (expected Integer)");
        }

        //get data
        var transactionBody = new TransactionBody();
        //0 : set<transaction_input>    ; inputs
        var inputsCbor = transactionBodyCbor[0];
        foreach (var input in inputsCbor.Values)
        {
            transactionBody.TransactionInputs.Add(input.GetTransactionInput());
        }

        //1 : [* transaction_output]
        var outputsCbor = transactionBodyCbor[1];
        foreach (var output in outputsCbor.Values)
        {
            transactionBody.TransactionOutputs.Add(output.GetTransactionOutput());
        }

        //2 : coin                      ; fee
        transactionBody.Fee = transactionBodyCbor[2].DecodeValueToUInt64();

        //? 3 : uint                    ; time to live
        if (transactionBodyCbor.ContainsKey(3))
        {
            transactionBody.ValidBefore = transactionBodyCbor[3].DecodeValueToUInt32();
        }

        //? 4 : [* certificate]
        if (transactionBodyCbor.ContainsKey(4))
        {
            transactionBody.Certificates = new List<Certificate>();

            var certificatesCbor = transactionBodyCbor[4];
            foreach (var certificate in certificatesCbor.Values)
                transactionBody.Certificates.Add(certificate.GetCertificate());
        }

        //? 5 : withdrawals
        if (transactionBodyCbor.ContainsKey(5))
        {
            transactionBody.Withdrawls = new Dictionary<byte[], uint>();

            var withdrawalsCbor = transactionBodyCbor[5];
            foreach (var withdrawal in withdrawalsCbor.Keys)
            {
                var byteWithdrawal = ((string)withdrawal.DecodeValueByCborType()).HexToByteArray();
                var value = withdrawalsCbor[withdrawal].DecodeValueToUInt32();
                transactionBody.Withdrawls.Add(byteWithdrawal, value);
            }
        }

        //? 6 : update
        //? 7 : auxiliary_data_hash
        if (transactionBodyCbor.ContainsKey(7))
        {
            transactionBody.MetadataHash = (string)transactionBodyCbor[7].DecodeValueByCborType();
        }

        //? 8 : uint                    ; validity interval start
        if (transactionBodyCbor.ContainsKey(8))
        {
            transactionBody.ValidAfter = transactionBodyCbor[8].DecodeValueToUInt32();
        }

        //? 9 : mint
        if (transactionBodyCbor.ContainsKey(9))
        {
            var mintCbor = transactionBodyCbor[9];
            foreach (var key in mintCbor.Keys)
            {
                var byteMintKey = ((string)key.DecodeValueByCborType()).HexToByteArray();
                var assetCbor = mintCbor[key];
                var nativeAsset = new NativeAsset();
                foreach (var assetKey in assetCbor.Keys)
                {
                    var byteAssetKey = ((string)assetKey.DecodeValueByCborType()).HexToByteArray();
                    var token = assetCbor[assetKey].DecodeValueToInt64();
                    nativeAsset.Token.Add(byteAssetKey, token);
                }

                transactionBody.Mint.Add(byteMintKey, nativeAsset);
            }
        }

        //? 11 : script_data_hash;
        if (transactionBodyCbor.ContainsKey(11))
        {
            var scriptDataHashCBOR = transactionBodyCbor[11];
            var scriptDataHash = ((string)scriptDataHashCBOR.DecodeValueByCborType()).HexToByteArray();
            transactionBody.ScriptDataHash = scriptDataHash;
        }

        //? 13 : set<transaction_input> ; collateral inputs
        if (transactionBodyCbor.ContainsKey(13))
        {
            transactionBody.Collateral = new List<TransactionInput>();

            var collateralInputsCbor = transactionBodyCbor[13];
            foreach (var input in collateralInputsCbor.Values)
            {
                transactionBody.Collateral.Add(input.GetTransactionInput());
            }
        }

        //? 14 : required_signers;
        if (transactionBodyCbor.ContainsKey(14))
        {
            transactionBody.RequiredSigners = new List<byte[]>();

            var requiredSignersCbor = transactionBodyCbor[14];
            foreach (var requiredSignerCbor in requiredSignersCbor.Values)
            {
                var requiredSigner = ((string)requiredSignerCbor.DecodeValueByCborType()).HexToByteArray();
                transactionBody.RequiredSigners.Add(requiredSigner);
            }
        }

        //? 15 : network_id;
        if (transactionBodyCbor.ContainsKey(15))
        {
            transactionBody.NetworkId = transactionBodyCbor[15].DecodeValueToUInt32();
        }

        //? 16 : transaction_output     ; collateral return; New
        if (transactionBodyCbor.ContainsKey(16))
        {
            transactionBody.CollateralReturn = transactionBodyCbor[16].GetTransactionOutput();
        }

        //? 17 : coin                   ; total collateral; New
        if (transactionBodyCbor.ContainsKey(17))
        {
            transactionBody.TotalCollateral = transactionBodyCbor[17].DecodeValueToUInt64();
        }

        //? 18 : set<transaction_input> ; reference inputs; New
        if (transactionBodyCbor.ContainsKey(18))
        {
            transactionBody.ReferenceInputs = new List<TransactionInput>();

            var referenceInputsCbor = transactionBodyCbor[18];
            foreach (var referenceInput in referenceInputsCbor.Values)
            {
                transactionBody.ReferenceInputs.Add(referenceInput.GetTransactionInput());
            }
        }

        //return
        return transactionBody;
    }

    public static byte[] Serialize(this TransactionBody transactionBody, AuxiliaryData auxiliaryData)
    {
        return transactionBody.GetCBOR(auxiliaryData).EncodeToBytes();
    }

    public static TransactionBody DeserializeTransactionBody(this byte[] bytes)
    {
        return CBORObject.DecodeFromBytes(bytes).GetTransactionBody();
    }
}
