using System.Collections.Generic;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.TransactionBuilding;

namespace CardanoSharp.Wallet.CIPs.CIP2
{
    public interface ICoinSelectionStrategy
    {
        void SelectInputs(CoinSelection coinSelection, List<Utxo> utxos, long amount, Asset? asset = null, List<Utxo>? requiredUtxos = null, int limit = 20);
        void SelectRequiredInputs(CoinSelection coinSelection, List<Utxo>? requiredUtxos = null);
    }
}
