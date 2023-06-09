namespace CardanoSharp.Wallet.Models
{
    public class Utxo
    {
        public string TxHash { get; set; } = null!;

        public uint TxIndex { get; set; }

        public string OutputAddress { get; set; } = null!;

        public Balance Balance { get; set; } = new Balance();

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Utxo other = (Utxo)obj;
            return TxHash == other.TxHash && TxIndex == other.TxIndex;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // Use a prime number to avoid hash collisions
            int hash = 17;

            // Assuming TxHash is never null
            hash = hash * 23 + (TxHash != null ? TxHash.GetHashCode() : 0);

            // Use ^ operator for TxIndex because it's a simple type
            hash = hash * 23 + TxIndex.GetHashCode();

            return hash;
        }
    }
}
