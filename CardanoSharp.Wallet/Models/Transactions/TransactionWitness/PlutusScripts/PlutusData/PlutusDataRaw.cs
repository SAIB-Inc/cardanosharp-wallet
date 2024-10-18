using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using PeterO.Cbor2;

public class PlutusDataRaw(byte[] Raw) : IPlutusData
{
    public CBORObject GetCBOR()
    {
        return CBORObject.DecodeFromBytes(Raw);
    }

    public byte[] Serialize()
    {
        return Raw;
    }
}