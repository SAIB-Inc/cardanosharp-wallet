namespace CardanoSharp.Wallet.Models.Transactions;

//pub enum CertificateKind
//{
//    StakeRegistration,
//    StakeDeregistration,
//    StakeDelegation,
//    PoolRegistration,
//    PoolRetirement,
//    GenesisKeyDelegation,
//    MoveInstantaneousRewardsCert,
//}
public partial class Certificate
{
    public StakeRegistration StakeRegistration { get; set; } = default!;
    public StakeDeregistration StakeDeregistration { get; set; } = default!;
    public StakeDelegation StakeDelegation { get; set; } = default!;
    public PoolRegistration PoolRegistration { get; set; } = default!;
    public PoolRetirement PoolRetirement { get; set; } = default!;
    public GenesisKeyDelegation GenesisKeyDelegation { get; set; } = default!;
    public MoveInstantaneousRewardsCert MoveInstantaneousRewardsCert { get; set; } = default!;
}
