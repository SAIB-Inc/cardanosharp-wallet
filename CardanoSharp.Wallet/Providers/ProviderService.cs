using System.Collections.Generic;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Models;

namespace CardanoSharp.Wallet.Providers;

public interface IAProviderService
{
    Task Initialize();

    //---------------------------------------------------------------------------------------------------//
    // Account Functions
    //---------------------------------------------------------------------------------------------------//
    public Task<string?> GetMainAddress(string? address, string order = "desc");

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Address Functions
    //---------------------------------------------------------------------------------------------------//
    public Task<List<Utxo>> GetSingleAddressUtxos(string address);
    public Task<List<Utxo>> GetUtxos(string address, bool filterSmartContractAddresses = false);

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Mempool Functions
    //---------------------------------------------------------------------------------------------------//
    Task<MempoolTransaction[]> GetMempoolTransactions(List<string> txHash);
    //---------------------------------------------------------------------------------------------------//
}

public class ProviderData
{
    public Block Block { get; set; } = default!;
    public EpochParameters? ProtocolParameters { get; set; } = default!;
}

public abstract class AProviderService : IAProviderService
{
    public IAccountClient accountClient { get; set; } = default!;
    public IAddressesClient addressesClient { get; set; } = default!;
    public IAssetsClient assetsClient { get; set; } = default!;
    public IBlocksClient blocksClient { get; set; } = default!;
    public IEpochsClient epochsClient { get; set; } = default!;
    public IMempoolClient mempoolClient { get; set; } = default!;
    public INetworkClient networkClient { get; set; } = default!;
    public IPoolsClient poolsClient { get; set; } = default!;
    public IScriptsClient scriptsClient { get; set; } = default!;
    public ITransactionsClient transactionsClient { get; set; } = default!;

    // Provider Data
    public ProviderData ProviderData { get; set; } = new();

    public virtual async Task Initialize()
    {
        this.ProviderData.Block = (await blocksClient.GetLatestBlockAsync())?.Content!;
        this.ProviderData.ProtocolParameters = (await epochsClient.GetLatestParamtersAsync())?.Content!;
    }

    //---------------------------------------------------------------------------------------------------//
    // Account Functions
    //---------------------------------------------------------------------------------------------------//
    public virtual Task<string?> GetMainAddress(string? address, string order = "desc")
    {
        throw new System.NotImplementedException();
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Address Functions
    //---------------------------------------------------------------------------------------------------//
    public virtual Task<List<Utxo>> GetSingleAddressUtxos(string address)
    {
        throw new System.NotImplementedException();
    }

    public virtual Task<List<Utxo>> GetUtxos(string address, bool filterSmartContractAddresses = false)
    {
        throw new System.NotImplementedException();
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Mempool Functions
    //---------------------------------------------------------------------------------------------------//
    public virtual Task<MempoolTransaction[]> GetMempoolTransactions(List<string> txHash)
    {
        throw new System.NotImplementedException();
    }
    //---------------------------------------------------------------------------------------------------//
}
