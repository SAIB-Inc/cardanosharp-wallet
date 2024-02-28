using System;
using System.Collections.Generic;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using PeterO.Cbor2;

namespace CardanoSharp.Wallet.Extensions.Models;

public static class RedeemerExtensions
{
    public static CBORObject GetCBOR(this Redeemer redeemer)
    {
        // redeemer = [ tag: redeemer_tag, index: uint, data: plutus_data, ex_units: ex_units ]
        var cborRedeemer = CBORObject.NewArray();
        cborRedeemer.Add((uint)redeemer.Tag);
        cborRedeemer.Add(redeemer.Index);
        cborRedeemer.Add(redeemer.PlutusData.GetCBOR());
        cborRedeemer.Add(redeemer.ExUnits.GetCBOR());
        return cborRedeemer;
    }

    public static Redeemer GetRedeemer(this CBORObject redeemerCbor)
    {
        if (redeemerCbor == null)
        {
            throw new ArgumentNullException(nameof(redeemerCbor));
        }

        if (redeemerCbor.Type != CBORType.Array)
        {
            throw new ArgumentException("redeemerCbor is not expected type CBORType.Array");
        }

        if (redeemerCbor.Count != 4)
        {
            throw new ArgumentException("redeemerCbor has unexpected number of elements (expected 4)");
        }

        Redeemer redeemer =
            new()
            {
                Tag = (RedeemerTag)redeemerCbor[0].DecodeValueToInt32(),
                Index = (uint)redeemerCbor[1].DecodeValueToUInt32(),
                PlutusData = redeemerCbor[2].GetPlutusData(),
                ExUnits = (ExUnits)redeemerCbor[3].GetExUnits()
            };
        return redeemer;
    }

    public static byte[] Serialize(this Redeemer redeemer)
    {
        return redeemer.GetCBOR().EncodeToBytes();
    }

    public static Redeemer Deserialize(this byte[] bytes)
    {
        return CBORObject.DecodeFromBytes(bytes).GetRedeemer();
    }

    public static Redeemer SetIndexFromUtxo(this Redeemer redeemer, Transaction transaction)
    {
        if (redeemer.Utxo == null)
            return redeemer;

        List<TransactionInput> transactionInputs = new();
        transactionInputs.AddRange((List<TransactionInput>)transaction.TransactionBody.TransactionInputs);

        //https://github.com/bloxbean/cardano-client-lib/blob/7322b16030d8fa3ac5417d5dc58c92df401855ad/function/src/main/java/com/bloxbean/cardano/client/function/helper/RedeemerUtil.java
        //https://cardano.stackexchange.com/questions/7969/meaning-of-index-of-redeemer-in-serialization-lib-10-4
        // Sort transaction inputs to determine redeemer index
        transactionInputs.Sort(new TransactionInputComparer());

        uint index = (uint)
            transactionInputs.FindIndex(t => t.TransactionId.ToStringHex() == redeemer.Utxo.TxHash && t.TransactionIndex == redeemer.Utxo.TxIndex);
        redeemer.Index = index;
        return redeemer;
    }
}
