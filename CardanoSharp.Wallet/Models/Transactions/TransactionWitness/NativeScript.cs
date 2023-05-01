using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.NativeScripts;

namespace CardanoSharp.Wallet.Models.Transactions.TransactionWitness
{
    //  native_script =
    //     [script_pubkey
    //     // script_all
    //     // script_any
    //     // script_n_of_k
    //     // invalid_before
    //        ; Timelock validity intervals are half-open intervals [a, b).
    //        ; This field specifies the left(included) endpoint a.
    //     // invalid_hereafter
    //        ; Timelock validity intervals are half-open intervals [a, b).
    //        ; This field specifies the right(excluded) endpoint b.
    //     ]
    public partial class NativeScript
    {
        public ScriptPubKey ScriptPubKey { get; set; } = default!;
        public ScriptAll ScriptAll { get; set; } = default!;
        public ScriptAny ScriptAny { get; set; } = default!;
        public ScriptNofK ScriptNofK { get; set; } = default!;
        public ScriptInvalidAfter InvalidAfter { get; set; } = default!;
        public ScriptInvalidBefore InvalidBefore { get; set; } = default!;
    }
}
