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
        bool keysEqual = Key == null && other.Key == null || Key != null && other.Key != null && Key.SequenceEqual(other.Key);
        bool chaincodesEqual =
            Chaincode == null && other.Chaincode == null || Chaincode != null && other.Chaincode != null && Chaincode.SequenceEqual(other.Chaincode);

        return keysEqual && chaincodesEqual;
    }

    public override int GetHashCode()
    {
        string keyHex = Key?.ToStringHex() ?? string.Empty;
        string chaincodeHex = Chaincode?.ToStringHex() ?? string.Empty;
        return System.HashCode.Combine(keyHex, chaincodeHex);
    }
}
