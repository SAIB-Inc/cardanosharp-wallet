using System;
using System.Collections.Generic;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using PeterO.Cbor2;

namespace CardanoSharp.Wallet.Extensions.Models;

public static class RedeemerExtensions
{
    public static CBORObject GetCBOR(this IEnumerable<Redeemer> redeemers)
    {
        CBORObject cborRedeemers = CBORObject.NewMap();

        foreach (Redeemer redeemer in redeemers)
        {
            CBORObject cborRedeemerKey = CBORObject.NewArray();
            cborRedeemerKey.Add((uint)redeemer.Tag);
            cborRedeemerKey.Add(redeemer.Index);

            CBORObject cborRedeemerValue = CBORObject.NewArray();
            cborRedeemerValue.Add(redeemer.PlutusData.GetCBOR());
            cborRedeemerValue.Add(redeemer.ExUnits.GetCBOR());

            cborRedeemers.Add(cborRedeemerKey, cborRedeemerValue);
        }

        return cborRedeemers;
    }

    public static IEnumerable<Redeemer> GetRedeemers(this CBORObject redeemersCbor)
    {
        if (redeemersCbor == null)
        {
            throw new ArgumentNullException(nameof(redeemersCbor));
        }

        if (redeemersCbor.Type != CBORType.Map)
        {
            throw new ArgumentException("redeemersCbor is not expected type CBORType.Map");
        }

        List<Redeemer> redeemers = new();

        foreach (CBORObject key in redeemersCbor.Keys)
        {
            if (key.Type != CBORType.Array)
            {
                throw new ArgumentException("redeemerCbor key is not expected type CBORType.Array");
            }

            if (key.Count != 2)
            {
                throw new ArgumentException("redeemerCbor key has unexpected number of elements (expected 2)");
            }

            CBORObject value = redeemersCbor[key];

            if (value.Type != CBORType.Array)
            {
                throw new ArgumentException("redeemerCbor value is not expected type CBORType.Array");
            }

            if (value.Count != 2)
            {
                throw new ArgumentException("redeemerCbor value has unexpected number of elements (expected 2)");
            }

            Redeemer redeemer =
                new()
                {
                    Tag = (RedeemerTag)key[0].DecodeValueToInt32(),
                    Index = (uint)key[1].DecodeValueToUInt32(),
                    PlutusData = value[0].GetPlutusData(),
                    ExUnits = value[1].GetExUnits()
                };

            redeemers.Add(redeemer);
        }

        return redeemers;
    }

    public static byte[] Serialize(this IEnumerable<Redeemer> redeemers)
    {
        return redeemers.GetCBOR().EncodeToBytes();
    }

    public static IEnumerable<Redeemer> Deserialize(this byte[] bytes)
    {
        return CBORObject.DecodeFromBytes(bytes).GetRedeemers();
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

        // If we are implementing a fast own input function in our smart contract,
        // we need to set the correct sorted index in the PlutusData array so we can find it fast in the smart contract
        if (redeemer.ParameterIndex != null)
        {
            PlutusDataConstr plutusDataConstr = redeemer.PlutusData.GetCBOR().GetPlutusDataConstr();
            IPlutusData[] updatedPlutusDatas = plutusDataConstr.Value.Value;
            updatedPlutusDatas[(int)redeemer.ParameterIndex] = new PlutusDataUInt(index);

            // Set the redeemer values back
            plutusDataConstr.Value.Value = updatedPlutusDatas;
            redeemer.PlutusData = plutusDataConstr;
        }

        return redeemer;
    }
}
