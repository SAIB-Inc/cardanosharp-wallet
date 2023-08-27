using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using Refit;

namespace CardanoSharp.Wallet.Providers.Blockfrost;

public interface IBlockfrostService : IAProviderService { }

public partial class BlockfrostService : AProviderService, IBlockfrostService
{
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
        this.accountClient = accountClient;
        this.addressesClient = addressesClient;
        this.assetsClient = assetsClient;
        this.blocksClient = blocksClient;
        this.epochsClient = epochsClient;
        this.mempoolClient = mempoolClient;
        this.networkClient = networkClient;
        this.poolsClient = poolsClient;
        this.scriptsClient = scriptsClient;
        this.transactionsClient = transactionsClient;
    }

    public override Task Initialize()
    {
        throw new System.NotImplementedException();
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
            var blockfrostAddresses = await accountClient.GetAccountAssociatedAddresses(stakeAddress, countPerPage, pageNumber, order);
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
        var mempoolTransactionTasks = txHash.Select(hash => mempoolClient.GetMempoolTransactionAsync(hash)).ToList();
        var mempoolTransactionContents = await Task.WhenAll(mempoolTransactionTasks);
        var mempoolTransactions = mempoolTransactionContents.Where(content => content.Content != null).Select(content => content.Content!).ToArray();
        return mempoolTransactions;
    }
    //---------------------------------------------------------------------------------------------------//
}
