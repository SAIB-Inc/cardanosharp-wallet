using System;
using System.Numerics;
using CardanoSharp.Wallet.Extensions.Models;
using PeterO.Cbor2;

namespace CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

// big_int = int / big_uint / big_nint
// big_uint = #6.2(bounded_bytes)
// big_nint = #6.3(bounded_bytes)

// int
public class PlutusDataInt : IPlutusData
{
    public int Value { get; set; }

    public PlutusDataInt() { }

    public PlutusDataInt(int number)
    {
        this.Value = number;
    }

    public CBORObject GetCBOR()
    {
        return CBORObject.FromObject(Value);
    }

    public byte[] Serialize()
    {
        return GetCBOR().EncodeToBytes();
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        PlutusDataInt other = (PlutusDataInt)obj;
        return Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }
}

// big_uint = #6.2(bounded_bytes)
public class PlutusDataUInt : IPlutusData
{
    public BigInteger Value { get; set; }

    public PlutusDataUInt() { }

    public PlutusDataUInt(long number)
    {
        Value = new BigInteger(number);
    }

    public CBORObject GetCBOR()
    {
        return CBORObject.FromObject((long)Value);
    }

    public byte[] Serialize()
    {
        return GetCBOR().EncodeToBytes();
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;
        PlutusDataUInt other = (PlutusDataUInt)obj;
        return Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }
}

// big_nint = #6.3(bounded_bytes)
public class PlutusDataNInt : IPlutusData
{
    public BigInteger Value { get; set; }

    public PlutusDataNInt() { }

    public PlutusDataNInt(long number)
    {
        Value = new BigInteger(number);
    }

    public CBORObject GetCBOR()
    {
        return CBORObject.FromObject((long)Value);
    }

    public byte[] Serialize()
    {
        return GetCBOR().EncodeToBytes();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PlutusDataNInt)
            return false;

        PlutusDataNInt other = (PlutusDataNInt)obj;
        return this.Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }
}

public static partial class PlutusDataExtensions
{
    public static IPlutusData GetPlutusDataBigInt(this CBORObject dataCbor)
    {
        if (dataCbor == null)
            throw new ArgumentNullException(nameof(dataCbor));

        if (dataCbor.Type != CBORType.Integer)
            throw new ArgumentException("dataCbor is not expected type CBORType.Integer");

        var number = dataCbor.AsNumber();
        if (number.CanFitInInt32())
            return dataCbor.GetPlutusDataInt();

        if (number.IsNegative())
            return dataCbor.GetPlutusDataNInt();

        return dataCbor.GetPlutusDataUInt();
    }

    public static PlutusDataInt GetPlutusDataInt(this CBORObject dataCbor)
    {
        if (dataCbor == null)
            throw new ArgumentNullException(nameof(dataCbor));

        if (dataCbor.Type != CBORType.Integer)
            throw new ArgumentException("dataCbor is not expected type CBORType.Integer");

        var number = dataCbor.AsNumber();
        if (!number.CanFitInInt32())
            throw new ArgumentException("Attempting to deserialize dataCbor as int but number is larger than size int");

        int data = dataCbor.DecodeValueToInt32();
        PlutusDataInt plutusDataInt = new() { Value = data };
        return plutusDataInt;
    }

    public static PlutusDataUInt GetPlutusDataUInt(this CBORObject dataCbor)
    {
        if (dataCbor == null)
            throw new ArgumentNullException(nameof(dataCbor));

        if (dataCbor.Type != CBORType.Integer)
            throw new ArgumentException("dataCbor is not expected type CBORType.Integer");

        var number = dataCbor.AsNumber();
        if (!number.CanFitInInt64())
            throw new ArgumentException("Attempting to deserialize dataCbor as uint but number is larger than size uint");

        long data = dataCbor.DecodeValueToInt64();
        PlutusDataUInt plutusDataUInt = new(data);
        return plutusDataUInt;
    }

    public static PlutusDataNInt GetPlutusDataNInt(this CBORObject dataCbor)
    {
        if (dataCbor == null)
            throw new ArgumentNullException(nameof(dataCbor));

        if (dataCbor.Type != CBORType.Integer)
            throw new ArgumentException("dataCbor is not expected type CBORType.Integer");

        var number = dataCbor.AsNumber();
        if (!number.IsNegative() || !number.CanFitInInt64())
            throw new ArgumentException("Attempting to deserialize dataCbor as nint but number is not negative");

        long data = dataCbor.DecodeValueToInt64();
        PlutusDataNInt plutusDataNInt = new(data);
        return plutusDataNInt;
    }
}
