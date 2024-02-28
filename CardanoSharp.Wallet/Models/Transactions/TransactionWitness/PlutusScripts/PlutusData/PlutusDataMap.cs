using System;
using System.Collections.Generic;
using PeterO.Cbor2;

namespace CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

// { * plutus_data => plutus_data }
public class PlutusDataMap : IPlutusData
{
    public Dictionary<IPlutusData, IPlutusData> Value { get; set; } = new Dictionary<IPlutusData, IPlutusData>();

    public CBORObject GetCBOR()
    {
        var cborDatum = CBORObject.NewMap();
        if (Value == null)
            return cborDatum;

        foreach (var dataPair in Value)
            cborDatum.Add(dataPair.Key.GetCBOR(), dataPair.Value.GetCBOR());

        return cborDatum;
    }

    public byte[] Serialize()
    {
        return GetCBOR().EncodeToBytes();
    }
}

public static partial class PlutusDataExtensions
{
    public static PlutusDataMap GetPlutusDataMap(this CBORObject dataCbor)
    {
        if (dataCbor == null)
            throw new ArgumentNullException(nameof(dataCbor));

        if (dataCbor.Type != CBORType.Map)
            throw new ArgumentException("dataCbor is not expected type CBORType.Map");

        PlutusDataMap plutusDataMap = new PlutusDataMap();
        Dictionary<IPlutusData, IPlutusData> plutusDatas = new Dictionary<IPlutusData, IPlutusData>();
        foreach (var key in dataCbor.Keys)
        {
            IPlutusData plutusDataKey = key.GetPlutusData();
            IPlutusData plutusDataValue = dataCbor[key].GetPlutusData();
            plutusDatas.Add(plutusDataKey, plutusDataValue);
        }

        plutusDataMap.Value = plutusDatas;
        return plutusDataMap;
    }
}
