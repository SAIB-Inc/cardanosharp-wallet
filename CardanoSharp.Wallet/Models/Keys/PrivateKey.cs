using System.Linq;
using CardanoSharp.Wallet.Extensions;

namespace CardanoSharp.Wallet.Models.Keys;

public class PrivateKey
{
    public byte[] Key { get; }
    public byte[] Chaincode { get; }

    public PrivateKey(byte[] key, byte[] chaincode)
    {
        Key = key;
        Chaincode = chaincode;
    }

    public override bool Equals(object? obj)
    {
        // Check for null and compare run-time types.
        if (obj == null || GetType() != obj.GetType())
            return false;

        PrivateKey other = (PrivateKey)obj;
        return Key.SequenceEqual(other.Key) && Chaincode.SequenceEqual(other.Chaincode);
    }

    public override int GetHashCode()
    {
        string keyHex = Key.ToStringHex();
        string chaincodeHex = Chaincode.ToStringHex();
        return System.HashCode.Combine(keyHex, chaincodeHex);
    }
}
