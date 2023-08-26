using System;

namespace CardanoSharp.Wallet.CIPs.CIP2;

public class InsufficientFundsException : Exception
{
    public InsufficientFundsException() : base() { }

    public InsufficientFundsException(string message) : base(message) { }
}
