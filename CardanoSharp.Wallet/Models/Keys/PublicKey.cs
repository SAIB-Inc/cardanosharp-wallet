using System.Linq;
using CardanoSharp.Wallet.Extensions;

namespace CardanoSharp.Wallet.Models.Keys;

public class PublicKey
{
    public byte[] Key { get; set; }
    public byte[] Chaincode { get; set; }

    public PublicKey(byte[] key, byte[] chaincode)
    {
        Key = key;
        Chaincode = chaincode;
    }

    public override bool Equals(object? obj)
    {
        // Check for null and compare run-time types.
        if (obj == null || GetType() != obj.GetType())
            return false;

        PublicKey other = (PublicKey)obj;
        return Key.SequenceEqual(other.Key) && Chaincode.SequenceEqual(other.Chaincode);
    }

    public override int GetHashCode()
    {
        string keyHex = Key.ToStringHex();
        string chaincodeHex = Chaincode.ToStringHex();
        return System.HashCode.Combine(keyHex, chaincodeHex);
    }
}
