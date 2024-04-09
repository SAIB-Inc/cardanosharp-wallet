using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace CardanoSharp.Wallet.Extensions;

public static class ByteArrayExtension
{
    /// <summary>
    /// Concatinates two given byte arrays and returns a new byte array containing all the elements.
    /// </summary>
    /// <remarks>
    /// This is a lot faster than Linq (~30 times)
    /// </remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <param name="firstArray">First set of bytes in the final array.</param>
    /// <param name="secondArray">Second set of bytes in the final array.</param>
    /// <returns>The concatinated array of bytes.</returns>
    public static byte[] ConcatFast(this byte[] firstArray, byte[] secondArray)
    {
        if (firstArray == null)
            throw new ArgumentNullException(nameof(firstArray), "First array can not be null!");
        if (secondArray == null)
            throw new ArgumentNullException(nameof(secondArray), "Second array can not be null!");

        byte[] result = new byte[firstArray.Length + secondArray.Length];
        Buffer.BlockCopy(firstArray, 0, result, 0, firstArray.Length);
        Buffer.BlockCopy(secondArray, 0, result, firstArray.Length, secondArray.Length);
        return result;
    }

    /// <summary>
    /// Creates a new array from the given array by taking a specified number of items starting from a given index.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IndexOutOfRangeException"/>
    /// <param name="sourceArray">The array containing bytes to take.</param>
    /// <param name="index">Starting index in <paramref name="sourceArray"/>.</param>
    /// <param name="count">Number of elements to take.</param>
    /// <returns>An array of bytes.</returns>
    public static byte[] SubArray(this byte[] sourceArray, int index, int count)
    {
        if (sourceArray == null)
            throw new ArgumentNullException(nameof(sourceArray), "Input can not be null!");
        if (index < 0 || count < 0)
            throw new IndexOutOfRangeException("Index or count can not be negative.");
        if (sourceArray.Length != 0 && index > sourceArray.Length - 1 || sourceArray.Length == 0 && index != 0)
            throw new IndexOutOfRangeException("Index can not be bigger than array length.");
        if (count > sourceArray.Length - index)
            throw new IndexOutOfRangeException("Array is not long enough.");

        byte[] result = new byte[count];
        Buffer.BlockCopy(sourceArray, index, result, 0, count);
        return result;
    }

    /// <summary>
    /// Creates a new array from the given array by taking items starting from a given index.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IndexOutOfRangeException"/>
    /// <param name="sourceArray">The array containing bytes to take.</param>
    /// <param name="index">Starting index in <paramref name="sourceArray"/>.</param>
    /// <returns>An array of bytes.</returns>
    public static byte[] SubArray(this byte[] sourceArray, int index)
    {
        if (sourceArray == null)
            throw new ArgumentNullException(nameof(sourceArray), "Input can not be null!");
        if (sourceArray.Length != 0 && index > sourceArray.Length - 1 || sourceArray.Length == 0 && index != 0)
            throw new IndexOutOfRangeException("Index can not be bigger than array length.");

        return SubArray(sourceArray, index, sourceArray.Length - index);
    }

    public static T[] Slice<T>(this T[] source, int start, int end)
    {
        if (end < 0)
            end = source.Length;

        var len = end - start;

        // Return new array.
        var res = new T[len];
        for (var i = 0; i < len; i++)
            res[i] = source[i + start];
        return res;
    }

    public static T[] Slice<T>(this T[] source, int start)
    {
        return Slice<T>(source, start, -1);
    }

    //Research speed of string to byte[]
    public static byte[] HexToByteArray(this string hex)
    {
        if (hex.Length % 2 == 1)
            throw new Exception("The binary key cannot have an odd number of digits");

        byte[] arr = new byte[hex.Length >> 1];

        for (int i = 0; i < hex.Length >> 1; ++i)
        {
            arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
        }

        return arr;
    }

    public static int GetHexVal(char hex)
    {
        int val = (int)hex;
        //For uppercase A-F letters:
        //return val - (val < 58 ? 48 : 55);
        //For lowercase a-f letters:
        //return val - (val < 58 ? 48 : 87);
        //Or the two combined, but a bit slower:
        return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
    }

    public static bool SequenceEqual(this byte[] x, byte[] y)
    {
        return MemoryExtensions.SequenceEqual<byte>(x, y);
    }

    public static byte[] NewEncrypt(
        this byte[] message,
        string password,
        int keySize = 256,
        int blockSize = 128,
        int derivationIterations = 1000,
        int saltSize = 16
    )
    {
        byte[] salt;
        using (var rng = RandomNumberGenerator.Create())
        {
            salt = new byte[saltSize];
            rng.GetBytes(salt);
        }

        using var keyDerivationFunction = new Rfc2898DeriveBytes(password, salt, derivationIterations);
        using var aesAlg = Aes.Create();
        aesAlg.KeySize = keySize;
        aesAlg.BlockSize = blockSize;
        aesAlg.Key = keyDerivationFunction.GetBytes(keySize / 8);
        aesAlg.IV = keyDerivationFunction.GetBytes(blockSize / 8);
        aesAlg.Padding = PaddingMode.PKCS7;

        using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using var msEncrypt = new MemoryStream();
        msEncrypt.Write(salt, 0, saltSize);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            csEncrypt.Write(message, 0, message.Length);
        }

        return msEncrypt.ToArray();
    }

    public static byte[] NewDecrypt(
        this byte[] encryptedMessageWithSalt,
        string password,
        int keySize = 256,
        int blockSize = 128,
        int derivationIterations = 1000,
        int saltSize = 16
    )
    {
        byte[] salt = new byte[saltSize];
        Array.Copy(encryptedMessageWithSalt, 0, salt, 0, saltSize);

        byte[] encryptedMessage = new byte[encryptedMessageWithSalt.Length - saltSize];
        Array.Copy(encryptedMessageWithSalt, saltSize, encryptedMessage, 0, encryptedMessage.Length);

        using var keyDerivationFunction = new Rfc2898DeriveBytes(password, salt, derivationIterations);
        using var aesAlg = Aes.Create();
        aesAlg.KeySize = keySize;
        aesAlg.BlockSize = blockSize;
        aesAlg.Key = keyDerivationFunction.GetBytes(keySize / 8);
        aesAlg.IV = keyDerivationFunction.GetBytes(blockSize / 8);
        aesAlg.Padding = PaddingMode.PKCS7;

        using var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        using var msDecrypt = new MemoryStream(encryptedMessage);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var msResult = new MemoryStream();
        csDecrypt.CopyTo(msResult);
        return msResult.ToArray();
    }

    // Old Encrypt Function, this only works for messages that are multiples of 16
    [Obsolete("This method is obsolete, for new data use NewEncrypt instead")]
    public static byte[] Encrypt(this byte[] bytesToEncrypt, string password)
    {
        using var f = RandomNumberGenerator.Create();
        byte[] ivSeed = Guid.NewGuid().ToByteArray();
        f.GetBytes(ivSeed);

        var rfc = new Rfc2898DeriveBytes(password, ivSeed);
        byte[] Key = rfc.GetBytes(16);
        byte[] IV = rfc.GetBytes(16);

        byte[] encrypted;
        using (var mstream = new MemoryStream())
        {
            using var aes = Aes.Create();
            using CryptoStream cryptoStream = new CryptoStream(mstream, aes.CreateEncryptor(Key, IV), CryptoStreamMode.Write);
            cryptoStream.Write(bytesToEncrypt, 0, bytesToEncrypt.Length);

            encrypted = mstream.ToArray();
        }

        var messageLengthAs32Bits = Convert.ToInt32(bytesToEncrypt.Length);
        var messageLength = BitConverter.GetBytes(messageLengthAs32Bits);

        encrypted = encrypted.Prepend(ivSeed);
        encrypted = encrypted.Prepend(messageLength);

        return encrypted;
    }

    // Old Decrypt Function, this only works for messages that are multiples of 16
    [Obsolete("This method is obsolete, for new data use NewDecrypt instead")]
    public static byte[] Decrypt(this byte[] bytesToDecrypt, string password)
    {
        (byte[] messageLengthAs32Bits, byte[] bytesWithIv) = bytesToDecrypt.Shift(4); // get the message length
        (byte[] ivSeed, byte[] encrypted) = bytesWithIv.Shift(16); // get the initialization vector

        var length = BitConverter.ToInt32(messageLengthAs32Bits, 0);

        var rfc = new Rfc2898DeriveBytes(password, ivSeed);
        byte[] Key = rfc.GetBytes(16);
        byte[] IV = rfc.GetBytes(16);

        using var mStream = new MemoryStream(encrypted);
        using var aes = Aes.Create();
        aes.Padding = PaddingMode.None;
        using var cryptoStream = new CryptoStream(mStream, aes.CreateDecryptor(Key, IV), CryptoStreamMode.Read);
        cryptoStream.Read(encrypted, 0, length);
        return mStream.ToArray().Take(length).ToArray();
    }

    public static byte[] Prepend(this byte[] bytes, byte[] bytesToPrepend)
    {
        var tmp = new byte[bytes.Length + bytesToPrepend.Length];
        bytesToPrepend.CopyTo(tmp, 0);
        bytes.CopyTo(tmp, bytesToPrepend.Length);
        return tmp;
    }

    public static (byte[] left, byte[] right) Shift(this byte[] bytes, int size)
    {
        var left = new byte[size];
        var right = new byte[bytes.Length - size];

        Array.Copy(bytes, 0, left, 0, left.Length);
        Array.Copy(bytes, left.Length, right, 0, right.Length);

        return (left, right);
    }

    /// <summary>
    /// Returns the last n bits of the byte
    /// </summary>
    /// <param name="b"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    public static int LastBits(this byte b, int n)
    {
        if (n > 8)
        {
            throw new InvalidOperationException($"{nameof(n)} must be <= 8");
        }

        int mask = ~(0xff >> n << n);
        return b & mask;
    }
}
