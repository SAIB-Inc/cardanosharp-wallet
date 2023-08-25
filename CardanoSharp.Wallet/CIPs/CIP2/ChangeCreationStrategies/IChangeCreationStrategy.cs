using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Models;

namespace CardanoSharp.Wallet.CIPs.CIP2.ChangeCreationStrategies;

public interface IChangeCreationStrategy
{
    void CalculateChange(CoinSelection coinSelection, Balance balance, string changeAddress, ulong feeBuffer = 0);
}
