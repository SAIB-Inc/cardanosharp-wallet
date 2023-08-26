using System.Threading.Tasks;
using CardanoSharp.Wallet.Enums;

namespace CardanoSharp.Wallet.Providers;

public interface IAProviderService
{
    Task Initialize();
}

public abstract class AProviderService : IAProviderService
{
    public virtual async Task Initialize()
    {
        await Task.CompletedTask;
    }
}
