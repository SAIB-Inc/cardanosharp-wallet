namespace CardanoSharp.Wallet.Common
{
    public class ProtocolParameters
    {
        // Constants taken from protocol params as of mainnet epoch 345
        public uint MinFeeA { get; set; } = 44;
        public uint MinFeeB { get; set; } = 155381;
        public ulong MaxTxExMem { get; set; } = 14000000;
        public ulong MaxTxExSteps { get; set; } = 10000000000;
        public double PriceMem { get; set; } = 0.0577;
        public double PriceStep { get; set; } = 0.0000721;

        public ProtocolParameters() { }
    }
}
