using System;
using System.Collections.Generic;
using CardanoSharp.Wallet.Extensions;

namespace CardanoSharp.Wallet.Models.Transactions
{
    //transaction_input = [transaction_id: $hash32
    //                    , index : uint
    //                    ]
    public partial class TransactionInput
    {
        public byte[] TransactionId { get; set; } = default!;
        public uint TransactionIndex { get; set; }
        public string? OutputAddress { get; set; } = null; // Used to calculate the number of addresses in a transaction for fee estimation
    }

    public class TransactionInputComparer : IComparer<TransactionInput>
    {
        public int Compare(TransactionInput x, TransactionInput y)
        {
            int txCompare = string.Compare(x.TransactionId.ToStringHex(), y.TransactionId.ToStringHex());
            if (txCompare != 0)
                return txCompare;
            else
                return x.TransactionIndex.CompareTo(y.TransactionIndex);
        }
    }

    public class TransactionEqualityInputComparer : IEqualityComparer<TransactionInput>
    {
        public bool Equals(TransactionInput x, TransactionInput y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            // Adjust this as necessary. It might be that you have to convert the byte arrays to strings
            // or something similar to properly compare them
            return x.TransactionId.SequenceEqual(y.TransactionId) && x.TransactionIndex == y.TransactionIndex;
        }

        public int GetHashCode(TransactionInput obj)
        {
            // Use prime numbers to calculate hash code
            int hash = 17;
            hash = hash * 31 + (obj.TransactionId != null ? BitConverter.ToInt32(obj.TransactionId, 0) : 0);
            hash = hash * 31 + (int)obj.TransactionIndex;
            return hash;
        }
    }
}
