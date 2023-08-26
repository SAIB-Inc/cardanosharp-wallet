using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk;
using CardanoSharp.Blockfrost.Sdk.Contracts;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;

namespace CardanoSharp.Wallet.Providers.Blockfrost;

public interface IBlockfrostService : IAProviderService { }

public partial class BlockfrostService : AProviderService, IBlockfrostService
{
    private readonly IAccountClient _accountClient;
    private readonly IAddressesClient _addressesClient;
    private readonly IAssetsClient _assetsClient;
    private readonly IBlocksClient _blocksClient;
    private readonly IEpochsClient _epochsClient;
    private readonly INetworkClient _networkClient;
    private readonly IPoolsClient _poolsClient;
    private readonly IScriptsClient _scriptsClient;
    private readonly ITransactionsClient _transactionsClient;

    public BlockfrostService(
        IAccountClient accountClient,
        IAddressesClient addressesClient,
        IAssetsClient assetsClient,
        IBlocksClient blocksClient,
        IEpochsClient epochsClient,
        INetworkClient networkClient,
        IPoolsClient poolsClient,
        IScriptsClient scriptsClient,
        ITransactionsClient transactionsClient
    )
    {
        _accountClient = accountClient;
        _addressesClient = addressesClient;
        _assetsClient = assetsClient;
        _blocksClient = blocksClient;
        _epochsClient = epochsClient;
        _networkClient = networkClient;
        _poolsClient = poolsClient;
        _scriptsClient = scriptsClient;
        _transactionsClient = transactionsClient;
    }

    public override Task Initialize()
    {
        throw new System.NotImplementedException();
    }

    //---------------------------------------------------------------------------------------------------//
    // Account Functions
    //---------------------------------------------------------------------------------------------------//
    public async Task<string?> GetMainAddress(string? address, string order = "asc")
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
            var blockfrostAddresses = await _accountClient.GetAccountAssociatedAddresses(stakeAddress, countPerPage, pageNumber, order);
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
    public async Task<List<Utxo>> GetSingleAddressUtxos(string address)
    {
        return await GetUtxosHelper(address);
    }

    // public static async Task<List<Utxo>> GetUtxos(string address, bool filterSmartContractAddresses = false)
    // {
    //     return await GetUtxosParallelized(address, filterSmartContractAddresses);
    // }

    //---------------------------------------------------------------------------------------------------//

    //---------------------------------------------------------------------------------------------------//
    // Helper Functions
    //---------------------------------------------------------------------------------------------------//
    public static Balance GetBalance(List<Amount> amounts)
    {
        ulong lovelaces = 0;
        List<Asset> assets = new();
        foreach (Amount amount in amounts)
        {
            if (amount.Unit == "lovelace")
            {
                lovelaces = ulong.Parse(amount.Quantity);
            }
            else
            {
                Asset asset =
                    new()
                    {
                        PolicyId = amount.Unit[..56],
                        Name = amount.Unit[56..],
                        Quantity = long.Parse(amount.Quantity)
                    };
                assets.Add(asset);
            }
        }

        return new Balance() { Lovelaces = lovelaces, Assets = assets };
    }
    //---------------------------------------------------------------------------------------------------//
}
