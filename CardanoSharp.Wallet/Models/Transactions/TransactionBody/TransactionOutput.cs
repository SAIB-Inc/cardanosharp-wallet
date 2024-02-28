using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;

namespace CardanoSharp.Wallet.Models.Transactions;

//     transaction_output = legacy_transaction_output / post_alonzo_transaction_output ; New
//
//     legacy_transaction_output =
//     [ address
//       , amount : value
//       , ? datum_hash : $hash32
//     ]
//
//     post_alonzo_transaction_output =
//     {   0 : address
//         , 1 : value
//         , ? 2 : datum_option ; New; datum option
//         , ? 3 : script_ref   ; New; script reference
//      }
public partial class TransactionOutput
{
    public byte[] Address { get; set; } = default!;
    public TransactionOutputValue Value { get; set; } = default!;
    public DatumOption? DatumOption { get; set; }
    public ScriptReference? ScriptReference { get; set; }
    public OutputPurpose OutputPurpose { get; set; }
}
