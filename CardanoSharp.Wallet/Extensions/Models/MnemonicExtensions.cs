using System;
using System.Security.Cryptography;
using CardanoSharp.Wallet.Models.Derivations;
using CardanoSharp.Wallet.Models.Keys;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace CardanoSharp.Wallet.Extensions.Models;

public static class MnemonicExtensions
{
    public static PrivateKey GetRootKey(this Mnemonic mnemonic, string password = "")
    {
        var rootKey = KeyDerivation.Pbkdf2(password, mnemonic.Entropy, KeyDerivationPrf.HMACSHA512, 4096, 96);
        rootKey[0] &= 248;
        rootKey[31] &= 31;
        rootKey[31] |= 64;

        return new PrivateKey(rootKey.Slice(0, 64), rootKey.Slice(64));
    }

    public static MasterNodeDerivation GetMasterNode(this Mnemonic mnemonic, string password = "")
    {
        return new MasterNodeDerivation(mnemonic.GetRootKey(password));
    }

    // Ledger Functions
    // THE BELOW CODE IS NOT YET TESTED 5/7/2023, this is meant to get a Cardano Wallet from a ledger seed phrase
    public static PrivateKey GenerateLedgerMasterKey(this Mnemonic mnemonic, string password = "")
    {
        var rootKey = KeyDerivation.Pbkdf2(password, mnemonic.Entropy, KeyDerivationPrf.HMACSHA512, 2048, 64);
        byte[] masterSeed = rootKey.Slice(0, 64);

        byte[] message = new byte[masterSeed.Length + 1];
        message[0] = 1;
        Buffer.BlockCopy(masterSeed, 0, message, 1, masterSeed.Length);

        byte[] cc;
        using (HMACSHA256 hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes("ed25519 seed")))
        {
            cc = hmac.ComputeHash(message);
        }

        byte[] i = HashRepeatedly(masterSeed);
        byte[] tweaked = TweakBits(i);

        byte[] masterKey = new byte[tweaked.Length + cc.Length];
        Buffer.BlockCopy(tweaked, 0, masterKey, 0, tweaked.Length);
        Buffer.BlockCopy(cc, 0, masterKey, tweaked.Length, cc.Length);

        return new PrivateKey(masterKey.Slice(0, 64), masterKey.Slice(64));
    }

    private static byte[] HashRepeatedly(byte[] message)
    {
        byte[] i;
        using (HMACSHA512 hmac = new HMACSHA512(System.Text.Encoding.UTF8.GetBytes("ed25519 seed")))
        {
            i = hmac.ComputeHash(message);
        }

        if ((i[31] & 0b0010_0000) != 0)
        {
            return HashRepeatedly(i);
        }

        return i;
    }

    private static byte[] TweakBits(byte[] data)
    {
        // Clone the data to prevent modifying the original array
        byte[] copiedData = (byte[])data.Clone();

        copiedData[0] &= 0b1111_1000;
        copiedData[31] &= 0b0111_1111;
        copiedData[31] |= 0b0100_0000;

        return copiedData;
    }
}
