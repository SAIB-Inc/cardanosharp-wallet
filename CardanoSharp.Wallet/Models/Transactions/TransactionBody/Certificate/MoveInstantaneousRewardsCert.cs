using System.Collections.Generic;

namespace CardanoSharp.Wallet.Models.Transactions;

public partial class MoveInstantaneousRewardsCert
{
    public MIRPot MIRPot { get; set; }
    public Dictionary<byte[], uint> Rewards { get; set; } = default!;
}

public enum MIRPot
{
    Reserves,
    Treasury
}
