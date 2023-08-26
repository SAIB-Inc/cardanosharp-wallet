using System;
using System.Threading.Tasks;
using CardanoSharp.Blockfrost.Sdk;
using CardanoSharp.Blockfrost.Sdk.Common;
using CardanoSharp.Wallet.Enums;
using Microsoft.Extensions.DependencyInjection;

//using CardanoSharp.Blockfrost.Sdk;

namespace CardanoSharp.Wallet.Providers.Blockfrost;

public interface IBlockfrostService : IAProviderService { }

public partial class BlockfrostService : AProviderService, IBlockfrostService
{
    private readonly INetworkClient _networkClient;
    private readonly ITransactionsClient _transactionsClient;
    private readonly IAssetsClient _assetsClient;
    private readonly IScriptsClient _scriptsClient;
    private readonly IBlocksClient _blocksClient;
    private readonly IEpochsClient _epochsClient;
    private readonly IAddressesClient _addressesClient;
    private readonly IPoolsClient _poolsClient;
    private readonly IAccountClient _accountClient;

    public BlockfrostService(
        INetworkClient networkClient,
        ITransactionsClient transactionsClient,
        IAssetsClient assetsClient,
        IScriptsClient scriptsClient,
        IBlocksClient blocksClient,
        IEpochsClient epochsClient,
        IAddressesClient addressesClient,
        IPoolsClient poolsClient,
        IAccountClient accountClient
    )
    {
        _networkClient = networkClient;
        _transactionsClient = transactionsClient;
        _assetsClient = assetsClient;
        _scriptsClient = scriptsClient;
        _blocksClient = blocksClient;
        _epochsClient = epochsClient;
        _addressesClient = addressesClient;
        _poolsClient = poolsClient;
        _accountClient = accountClient;
    }

    public override Task Initialize()
    {
        throw new System.NotImplementedException();
    }
}
