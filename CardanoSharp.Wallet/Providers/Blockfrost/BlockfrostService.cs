using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk;
using CardanoSharp.Blockfrost.Sdk.Common;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using Microsoft.Extensions.DependencyInjection;

namespace CardanoSharp.Wallet.Providers.Blockfrost;

public interface IBlockfrostService : IAProviderService { }

public partial class BlockfrostService : AProviderService, IBlockfrostService
{
    public BlockfrostService(string apiKey, string url)
    {
        var authConfig = new AuthHeaderConfiguration(apiKey, url);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddBlockfrost(authConfig);

        this.provider = serviceCollection.BuildServiceProvider();
        this.AccountClient = provider.GetRequiredService<IAccountClient>();
        this.AddressesClient = provider.GetRequiredService<IAddressesClient>();
        this.AssetsClient = provider.GetRequiredService<IAssetsClient>();
        this.BlocksClient = provider.GetRequiredService<IBlocksClient>();
        this.EpochsClient = provider.GetRequiredService<IEpochsClient>();
        this.MempoolClient = provider.GetRequiredService<IMempoolClient>();
        this.NetworkClient = provider.GetRequiredService<INetworkClient>();
        this.PoolsClient = provider.GetRequiredService<IPoolsClient>();
        this.ScriptsClient = provider.GetRequiredService<IScriptsClient>();
        this.TransactionsClient = provider.GetRequiredService<ITransactionsClient>();
    }

    public BlockfrostService(
        IAccountClient accountClient,
        IAddressesClient addressesClient,
        IAssetsClient assetsClient,
        IBlocksClient blocksClient,
        IEpochsClient epochsClient,
        IMempoolClient mempoolClient,
        INetworkClient networkClient,
        IPoolsClient poolsClient,
        IScriptsClient scriptsClient,
        ITransactionsClient transactionsClient
    )
    {
        this.AccountClient = accountClient;
        this.AddressesClient = addressesClient;
        this.AssetsClient = assetsClient;
        this.BlocksClient = blocksClient;
        this.EpochsClient = epochsClient;
        this.MempoolClient = mempoolClient;
        this.NetworkClient = networkClient;
        this.PoolsClient = poolsClient;
        this.ScriptsClient = scriptsClient;
        this.TransactionsClient = transactionsClient;
    }

    //---------------------------------------------------------------------------------------------------//
    // Account Functions
    //---------------------------------------------------------------------------------------------------//
    public override async Task<string?> GetMainAddress(string? address, string order = "asc")
    {
        try
        {
            if (address == null)
                return null;

            Address addr = new(address);
            Address stakeAddr = addr.GetStakeAddress();
            string stakeAddress = stakeAddr.ToString();

            int pageNumber = 1;
            int countPerPage = 1;
            var blockfrostAddresses = await AccountClient.GetAccountAssociatedAddresses(stakeAddress, countPerPage, pageNumber, order);
            if (blockfrostAddresses.Content == null || blockfrostAddresses.Content.Length <= 0)
                return null;

            string? mainAddress = blockfrostAddresses.Content?[0].Address;
            return mainAddress;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error in the GetMainAddress Function: {exception}");
            return null;
        }
    }

    public override async Task<List<Asset>> GetAccountAssets(string mainAddress)
    {
        return await GetAccountAssetsParallelized(mainAddress);
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Address Functions
    //---------------------------------------------------------------------------------------------------//
    public override async Task<List<Utxo>> GetSingleAddressUtxos(string address)
    {
        return await GetUtxosHelper(address);
    }

    public override async Task<List<Utxo>> GetUtxos(string address, bool filterSmartContractAddresses = false)
    {
        return await GetUtxosParallelized(address, filterSmartContractAddresses);
    }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Mempool Functions
    //---------------------------------------------------------------------------------------------------//
    public override async Task<MempoolTransaction[]> GetMempoolTransactions(List<string> txHash)
    {
        var mempoolTransactionTasks = txHash.Select(hash => MempoolClient.GetMempoolTransactionAsync(hash)).ToList();
        var mempoolTransactionContents = await Task.WhenAll(mempoolTransactionTasks);
        var mempoolTransactions = mempoolTransactionContents.Where(content => content.Content != null).Select(content => content.Content!).ToArray();
        return mempoolTransactions;
    }
    //---------------------------------------------------------------------------------------------------//
}
