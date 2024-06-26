using System;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.Utilities;
using PeterO.Cbor2;

namespace CardanoSharp.Wallet.Extensions.Models;

public static class DatumOptionExtension
{
    public static CBORObject GetCBOR(this DatumOption datumOption)
    {
        var cborDatum = CBORObject.NewArray();

        //datum_option = [ 0, $hash32 // 1, data ]
        if (datumOption.Hash is not null)
        {
            cborDatum.Add(0);
            cborDatum.Add(CBORObject.FromObject(datumOption.Hash));
        }
        else if (datumOption.Data is not null)
        {
            var inlineDatum = CBORObject.FromObject(datumOption.Data.Serialize());
            cborDatum.Add(1);
            cborDatum.Add(inlineDatum.WithTag(24));
        }

        return cborDatum;
    }

    public static DatumOption GetDatumOption(this CBORObject datumOptionCbor)
    {
        if (datumOptionCbor == null)
        {
            throw new ArgumentNullException(nameof(datumOptionCbor));
        }

        if (datumOptionCbor.Type != CBORType.Array)
        {
            throw new ArgumentException("datumOptionCbor is not expected type CBORType.Array");
        }

        if (datumOptionCbor.Count != 2)
        {
            throw new ArgumentException("datumOptionCbor has unexpected number of elements (expected 2)");
        }

        DatumOption datumOption = new DatumOption();
        var datumType = datumOptionCbor[0].DecodeValueToInt32();
        if (datumType == 0)
        {
            datumOption.Hash = ((string)datumOptionCbor[1].DecodeValueByCborType()).HexToByteArray();
        }
        else if (datumType == 1)
        {
            var dataCbor = datumOptionCbor[1].Untag();
            var rawCbor = dataCbor.GetByteString();
            var datumCbor = CBORObject.DecodeFromBytes(rawCbor);
            datumOption.Data = datumCbor.GetPlutusData();
        }

        return datumOption;
    }

    public static byte[] HashDatum(this DatumOption datumOption)
    {
        return HashUtility.Blake2b256(datumOption.Data!.Serialize());
    }

    public static byte[] Serialize(this DatumOption datumOption)
    {
        return datumOption.GetCBOR().EncodeToBytes();
    }

    public static DatumOption DeserializeDatumOption(this byte[] bytes)
    {
        return CBORObject.DecodeFromBytes(bytes).GetDatumOption();
    }

    // Inline Datum Functions
    public static CBORObject GetCBORFromInlineDatum(this byte[] bytes)
    {
        var cborDatum = CBORObject.NewArray();

        //datum_option = [ 0, $hash32 // 1, data ]
        var inlineDatum = CBORObject.FromObject(bytes);
        cborDatum.Add(1);
        cborDatum.Add(inlineDatum.WithTag(24));

        return cborDatum;
    }

    public static DatumOption DeserializeFromInlineDatum(this byte[] bytes)
    {
        return bytes.GetCBORFromInlineDatum().GetDatumOption();
    }
}
