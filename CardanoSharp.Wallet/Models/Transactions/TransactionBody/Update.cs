using System.Collections.Generic;

namespace CardanoSharp.Wallet.Models.Transactions
{
    //update = [proposed_protocol_parameter_updates
    //     , epoch
    //     ]
    public partial class Update
    {
        public Dictionary<GenesisKeyDelegation, ProtocolParamUpdate> ProposedProtocolParameterUpdates { get; set; } = default!;
        public uint Epoch { get; set; }
    }
}
