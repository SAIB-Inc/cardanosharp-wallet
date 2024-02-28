using PeterO.Cbor2;

namespace CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

public partial class DatumOption
{
    public byte[]? Hash { get; set; }

    public IPlutusData? Data
    {
        get => CBORObject.DecodeFromBytes(_rawData).GetPlutusData();
        set
        {
            RawData = value?.Serialize();
        }
    }

    private byte[]? _rawData;
    public byte[]? RawData
    {
        get => _rawData;
        set
        {
            _rawData = value;
        }
    }
}
