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
