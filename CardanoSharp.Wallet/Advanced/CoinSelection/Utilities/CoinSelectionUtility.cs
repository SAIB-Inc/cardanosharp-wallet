// using System.Collections.Generic;
// using System.Threading.Tasks;
// using CardanoSharp.Wallet.Advanced.CoinSelection.Enums;
// using CardanoSharp.Wallet.CIPs.CIP2;
// using CardanoSharp.Wallet.Enums;
// using CardanoSharp.Wallet.Extensions;
// using CardanoSharp.Wallet.Extensions.Models;
// using CardanoSharp.Wallet.Extensions.Models.Transactions;
// using CardanoSharp.Wallet.Models;
// using CardanoSharp.Wallet.Models.Addresses;
// using CardanoSharp.Wallet.Models.Transactions;
// using CardanoSharp.Wallet.Providers.Blockfrost;
// using CardanoSharp.Wallet.TransactionBuilding;
// using CardanoSharp.Wallet.Utilities;

// namespace CardanoSharp.Wallet.Advanced.CoinSelection.Utilities;

// public static class AdvancedCoinSelectionService
// {
//     public static async Task<TransactionBodyBuilder> UseCoinSelection(
//         this TransactionBodyBuilder transactionBodyBuilder,
//         BlockfrostService blockfrostService,
//         Address address,
//         TokenBundleBuilder? mint = null,
//         List<Utxo>? candidateUtxos = null,
//         List<Utxo>? requiredUtxos = null,
//         List<Utxo>? spentUtxos = null,
//         int limit = 50,
//         ulong feeBuffer = 1000000,
//         long maxTxSize = 12000,
//         TxChainingType txChainingType = TxChainingType.Filter,
//         bool isSmartContract = false
//     )
//     {
//         if (isSmartContract)
//             await transactionBodyBuilder.UseCoinAndCollateralSelection(
//                 blockfrostService,
//                 address,
//                 mint,
//                 candidateUtxos: candidateUtxos,
//                 requiredUtxos: requiredUtxos,
//                 spentUtxos: spentUtxos,
//                 limit,
//                 feeBuffer,
//                 maxTxSize,
//                 txChainingType
//             );
//         else
//             await transactionBodyBuilder.UseStandardCoinSelection(
//                 blockfrostService,
//                 address,
//                 mint,
//                 candidateUtxos: candidateUtxos,
//                 requiredUtxos: requiredUtxos,
//                 spentUtxos: spentUtxos,
//                 limit,
//                 feeBuffer,
//                 maxTxSize,
//                 txChainingType
//             );
//         return transactionBodyBuilder;
//     }

//     //---------------------------------------------------------------------------------------------------//
//     // Coin Selection Functions
//     //---------------------------------------------------------------------------------------------------//
//     public static async Task<TransactionBodyBuilder> UseStandardCoinSelection(
//         this TransactionBodyBuilder transactionBodyBuilder,
//         BlockfrostService blockfrostService,
//         Address address,
//         TokenBundleBuilder? mint = null,
//         List<Utxo>? candidateUtxos = null,
//         List<Utxo>? requiredUtxos = null,
//         List<Utxo>? spentUtxos = null,
//         int limit = 50,
//         ulong feeBuffer = 0,
//         long maxTxSize = 12000,
//         TxChainingType txChainingType = TxChainingType.Filter
//     )
//     {
//         string paymentAddress = address.ToString();
//         (HashSet<Utxo> inputUtxos, HashSet<Utxo> outputUtxos) = await GetMempoolUtxos(blockfrostService, paymentAddress);
//         HashSet<Utxo> spentUtxoSet = new();
//         if (spentUtxos != null)
//             spentUtxoSet = new HashSet<Utxo>(spentUtxos);

//         // First Address
//         List<Utxo> utxos;
//         if (candidateUtxos != null && candidateUtxos.Count > 0)
//             utxos = TxChainingUtxos(paymentAddress, candidateUtxos, inputUtxos, outputUtxos, spentUtxoSet, txChainingType);
//         else
//             utxos = TxChainingUtxos(
//                 paymentAddress,
//                 await blockfrostService.GetSingleAddressUtxos(paymentAddress),
//                 inputUtxos,
//                 outputUtxos,
//                 spentUtxoSet,
//                 txChainingType
//             );
//         try
//         {
//             transactionBodyBuilder.UseStandardCoinSelection(
//                 utxos,
//                 paymentAddress,
//                 mint: mint,
//                 requiredUtxos: requiredUtxos,
//                 limit: limit,
//                 feeBuffer: feeBuffer,
//                 maxTxSize: maxTxSize
//             );
//             return transactionBodyBuilder;
//         }
//         catch { }

//         // First and Last Address Utxos
//         string? lastAddress = await blockfrostService.GetMainAddress(paymentAddress, ESortOrder.Desc);
//         if (lastAddress != null && !IsSmartContractAddress(lastAddress))
//         {
//             (inputUtxos, outputUtxos) = await GetMempoolUtxos(blockfrostService, paymentAddress, inputUtxos, outputUtxos);
//             List<Utxo> singleFirstAndLastAddressUtxos = TxChainingUtxos(
//                 lastAddress,
//                 await blockfrostService.GetSingleAddressUtxos(lastAddress),
//                 inputUtxos,
//                 outputUtxos,
//                 spentUtxoSet,
//                 txChainingType
//             );
//             utxos.AddRange(singleFirstAndLastAddressUtxos);
//         }
//         try
//         {
//             transactionBodyBuilder.UseStandardCoinSelection(
//                 utxos,
//                 paymentAddress,
//                 mint: mint,
//                 requiredUtxos: requiredUtxos,
//                 limit: limit,
//                 feeBuffer: feeBuffer,
//                 maxTxSize: maxTxSize
//             );
//             return transactionBodyBuilder;
//         }
//         catch { }

//         // All Address Utxos
//         // If there is not enough ada in the first and last addresses, get all UTXOs from the wallet.
//         utxos = TxChainingUtxos(
//             paymentAddress,
//             await BlockfrostService.GetUtxos(paymentAddress, true),
//             inputUtxos,
//             outputUtxos,
//             spentUtxoSet,
//             txChainingType
//         );
//         transactionBodyBuilder.UseStandardCoinSelection(
//             utxos,
//             paymentAddress,
//             mint: mint,
//             requiredUtxos: requiredUtxos,
//             limit: limit,
//             feeBuffer: feeBuffer,
//             maxTxSize: maxTxSize
//         );
//         return transactionBodyBuilder;
//     }

//     public static async Task<TransactionBodyBuilder> UseCoinAndCollateralSelection(
//         this TransactionBodyBuilder transactionBodyBuilder,
//         BlockfrostService blockfrostService,
//         Address address,
//         TokenBundleBuilder? mint = null,
//         List<Utxo>? candidateUtxos = null,
//         List<Utxo>? requiredUtxos = null,
//         List<Utxo>? spentUtxos = null,
//         int limit = 20,
//         ulong feeBuffer = 0,
//         long maxTxSize = 12000,
//         TxChainingType txChainingType = TxChainingType.Filter
//     )
//     {
//         string paymentAddress = address.ToString();
//         (HashSet<Utxo> inputUtxos, HashSet<Utxo> outputUtxos) = await GetMempoolUtxos(blockfrostService, paymentAddress);
//         HashSet<Utxo> spentUtxoSet = new();
//         if (spentUtxos != null)
//             spentUtxoSet = new HashSet<Utxo>(spentUtxos);

//         // First Address
//         List<Utxo> utxos;
//         if (candidateUtxos != null && candidateUtxos.Count > 0)
//             utxos = TxChainingUtxos(paymentAddress, candidateUtxos, inputUtxos, outputUtxos, spentUtxoSet, txChainingType);
//         else
//             utxos = TxChainingUtxos(
//                 paymentAddress,
//                 await blockfrostService.GetSingleAddressUtxos(paymentAddress),
//                 inputUtxos,
//                 outputUtxos,
//                 spentUtxoSet,
//                 txChainingType
//             );
//         try
//         {
//             transactionBodyBuilder.UseCoinAndCollateralSelection(
//                 utxos,
//                 paymentAddress,
//                 mint: mint,
//                 requiredUtxos: requiredUtxos,
//                 limit: limit,
//                 feeBuffer: feeBuffer,
//                 maxTxSize: maxTxSize
//             );
//             return transactionBodyBuilder;
//         }
//         catch { }

//         // First and last Address Utxos
//         string? lastAddress = await blockfrostService.GetMainAddress(paymentAddress, ESortOrder.Desc);
//         if (lastAddress != null)
//         {
//             (inputUtxos, outputUtxos) = await GetMempoolUtxos(blockfrostService, paymentAddress, inputUtxos, outputUtxos);
//             List<Utxo> singleFirstAndLastAddressUtxos = TxChainingUtxos(
//                 lastAddress,
//                 await blockfrostService.GetSingleAddressUtxos(lastAddress),
//                 inputUtxos,
//                 outputUtxos,
//                 spentUtxoSet,
//                 txChainingType
//             );
//             utxos.AddRange(singleFirstAndLastAddressUtxos);
//         }
//         try
//         {
//             transactionBodyBuilder.UseCoinAndCollateralSelection(
//                 utxos,
//                 paymentAddress,
//                 mint: mint,
//                 requiredUtxos: requiredUtxos,
//                 limit: limit,
//                 feeBuffer: feeBuffer,
//                 maxTxSize: maxTxSize
//             );
//             return transactionBodyBuilder;
//         }
//         catch { }

//         // All Address Utxos
//         // If there is not enough ada in the first and last addresses, get all UTXOs from the wallet.
//         // This functions gets Utxos from an address that has the same stake key hash as the payment address.
//         utxos = TxChainingUtxos(
//             paymentAddress,
//             await BlockfrostService.GetUtxos(paymentAddress, true),
//             inputUtxos,
//             outputUtxos,
//             spentUtxoSet,
//             txChainingType
//         );
//         transactionBodyBuilder.UseCoinAndCollateralSelection(
//             utxos,
//             paymentAddress,
//             mint: mint,
//             requiredUtxos: requiredUtxos,
//             limit: limit,
//             feeBuffer: feeBuffer,
//             maxTxSize: maxTxSize
//         );
//         return transactionBodyBuilder;
//     }

//     //---------------------------------------------------------------------------------------------------//
//     // Coin Selection Helper Functions
//     //---------------------------------------------------------------------------------------------------//
//     private static TransactionBodyBuilder UseCoinAndCollateralSelection(
//         this TransactionBodyBuilder transactionBodyBuilder,
//         List<Utxo> utxos,
//         string paymentAddress,
//         TokenBundleBuilder? mint = null,
//         List<Utxo>? requiredUtxos = null,
//         int limit = 20,
//         ulong feeBuffer = 0,
//         long maxTxSize = 12000
//     )
//     {
//         CoinSelection? coinSelection = CoinSelection(
//             transactionBodyBuilder,
//             utxos,
//             paymentAddress,
//             mint: mint,
//             requiredUtxos: requiredUtxos,
//             limit: limit,
//             feeBuffer: feeBuffer,
//             maxTxSize: maxTxSize
//         );
//         if (coinSelection == null)
//             throw new InsufficientFundsException(
//                 "Not enough ada or NFTs in wallet to build transaction. Please add more ada or wait for your pending transactions to resolve on chain"
//             );

//         transactionBodyBuilder.UseCollateralSelection(
//             utxos,
//             paymentAddress,
//             feeBuffer: feeBuffer,
//             maxTxSize: maxTxSize - GetCoinSelectionSize(coinSelection)
//         );

//         // Set Inputs and outputs
//         foreach (TransactionOutput changeOutput in coinSelection.ChangeOutputs)
//             transactionBodyBuilder.AddOutput(changeOutput);

//         foreach (TransactionInput input in coinSelection.Inputs)
//             transactionBodyBuilder.AddInput(input);

//         return transactionBodyBuilder;
//     }

//     private static TransactionBodyBuilder UseStandardCoinSelection(
//         this TransactionBodyBuilder transactionBodyBuilder,
//         List<Utxo> utxos,
//         string paymentAddress,
//         TokenBundleBuilder? mint = null,
//         List<Utxo>? requiredUtxos = null,
//         int limit = 50,
//         ulong feeBuffer = 0,
//         long maxTxSize = 12000
//     )
//     {
//         CoinSelection? coinSelection = CoinSelection(
//             transactionBodyBuilder,
//             utxos,
//             paymentAddress,
//             mint: mint,
//             requiredUtxos: requiredUtxos,
//             limit: limit,
//             feeBuffer: feeBuffer,
//             maxTxSize: maxTxSize
//         );
//         if (coinSelection == null)
//             throw new InsufficientFundsException(
//                 "Not enough ada or NFTs in wallet to build transaction. Please add more ada or wait for your pending transactions to resolve on chain"
//             );

//         // Set Inputs and outputs
//         foreach (TransactionOutput changeOutput in coinSelection.ChangeOutputs)
//             transactionBodyBuilder.AddOutput(changeOutput);

//         foreach (TransactionInput input in coinSelection.Inputs)
//             transactionBodyBuilder.AddInput(input);

//         return transactionBodyBuilder;
//     }

//     //---------------------------------------------------------------------------------------------------//

//     //---------------------------------------------------------------------------------------------------//
//     // Standard Coin Selection Functions
//     //---------------------------------------------------------------------------------------------------//
//     public static CoinSelection? CoinSelection(
//         TransactionBodyBuilder transactionBodyBuilder,
//         List<Utxo> utxos,
//         string changeAddress,
//         TokenBundleBuilder? mint = null,
//         List<Utxo>? requiredUtxos = null,
//         int limit = 50,
//         ulong feeBuffer = 0,
//         long maxTxSize = 12000
//     )
//     {
//         // Filter out all required utxos from utxos
//         if (requiredUtxos != null && requiredUtxos.Count > 0)
//         {
//             var requiredUtxosSet = new HashSet<Utxo>(requiredUtxos);
//             utxos.RemoveAll(u => requiredUtxosSet.Contains(u));
//         }

//         // If random improve fails, fallback to largest first
//         CoinSelection? coinSelection = null;
//         try
//         {
//             coinSelection = transactionBodyBuilder.UseRandomImprove(utxos, changeAddress, mint!, requiredUtxos, limit, feeBuffer);
//             long totalSize = GetCoinSelectionSize(coinSelection);
//             if (totalSize > maxTxSize)
//                 coinSelection = transactionBodyBuilder.UseLargestFirst(utxos, changeAddress, mint!, requiredUtxos, limit, feeBuffer);

//             return coinSelection;
//         }
//         catch { }

//         try
//         {
//             coinSelection = transactionBodyBuilder.UseLargestFirst(utxos, changeAddress, mint!, requiredUtxos, limit, feeBuffer);
//         }
//         catch { }

//         return coinSelection;
//     }

//     public static CoinSelection UseLargestFirst(
//         TransactionBodyBuilder transactionBodyBuilder,
//         List<Utxo> utxos,
//         string changeAddress,
//         TokenBundleBuilder? mint = null,
//         List<Utxo>? requiredUtxos = null,
//         int limit = 50,
//         ulong feeBuffer = 0
//     )
//     {
//         CoinSelection coinSelection = transactionBodyBuilder.UseLargestFirst(utxos, changeAddress, mint!, requiredUtxos, limit, feeBuffer);
//         return coinSelection;
//     }

//     public static CoinSelection UseRandomImprove(
//         TransactionBodyBuilder transactionBodyBuilder,
//         List<Utxo> utxos,
//         string changeAddress,
//         TokenBundleBuilder? mint = null,
//         List<Utxo>? requiredUtxos = null,
//         int limit = 50,
//         ulong feeBuffer = 0
//     )
//     {
//         CoinSelection coinSelection = transactionBodyBuilder.UseRandomImprove(utxos, changeAddress, mint!, requiredUtxos, limit, feeBuffer);
//         return coinSelection;
//     }

//     public static long GetCoinSelectionSize(CoinSelection coinSelection)
//     {
//         long totalSize = 0;
//         foreach (TransactionInput transactionInput in coinSelection.Inputs)
//             totalSize += transactionInput.Serialize().Length;

//         foreach (TransactionOutput transactionOutput in coinSelection.ChangeOutputs)
//             totalSize += transactionOutput.Serialize().Length;

//         return totalSize;
//     }

//     //---------------------------------------------------------------------------------------------------//

//     //---------------------------------------------------------------------------------------------------//
//     // Helper Functions
//     //---------------------------------------------------------------------------------------------------//
//     private static async Task<(HashSet<Utxo>, HashSet<Utxo>)> GetMempoolUtxos(
//         BlockfrostService blockfrostService,
//         string address,
//         HashSet<Utxo>? currentInputUtxos = null,
//         HashSet<Utxo>? currentOutputUtxos = null
//     )
//     {
//         // Determine if the wallet has any pending transactions
//         List<BlockfrostMempool> mempoolTxHashes = await blockfrostService.GetMempoolByAddress(address);
//         List<string> pendingTxHashes = mempoolTxHashes.Select(mempoolTxHash => mempoolTxHash.tx_hash).ToList()!;

//         HashSet<Utxo> inputUtxos = new();
//         HashSet<Utxo> outputUtxos = new();
//         if (pendingTxHashes != null && pendingTxHashes.Count > 0)
//         {
//             List<BlockfrostMempoolTransaction> mempoolTransactions = await blockfrostService.GetMempoolTransactions(pendingTxHashes);
//             (inputUtxos, outputUtxos) = blockfrostService.GetUtxosFromMempoolTransactions(mempoolTransactions);
//         }

//         if (currentInputUtxos != null)
//             inputUtxos.UnionWith(currentInputUtxos);

//         if (currentOutputUtxos != null)
//             outputUtxos.UnionWith(currentOutputUtxos);

//         return (inputUtxos, outputUtxos);
//     }

//     private static List<Utxo> TxChainingUtxos(
//         string address,
//         List<Utxo> candidateUtxos,
//         HashSet<Utxo> inputUtxos,
//         HashSet<Utxo> outputUtxos,
//         HashSet<Utxo> spentUtxos,
//         TxChainingType txChainingType = TxChainingType.Filter
//     )
//     {
//         // Add all Utxos that are not inputs or outputs of the previous tx. We are adding outputs back later to ensure no duplicate utxos are added
//         List<Utxo> utxos = new();
//         if (txChainingType == TxChainingType.Filter || txChainingType == TxChainingType.Chain)
//         {
//             foreach (Utxo utxo in candidateUtxos)
//             {
//                 if (inputUtxos.Contains(utxo) || outputUtxos.Contains(utxo) || spentUtxos.Contains(utxo))
//                     continue;

//                 utxos.Add(utxo);
//             }
//         }

//         // Add all Utxos that are outputs of the previous tx to this address
//         if (txChainingType == TxChainingType.Chain)
//         {
//             foreach (Utxo utxo in outputUtxos)
//             {
//                 if (utxo.OutputAddress != address || spentUtxos.Contains(utxo))
//                     continue;

//                 utxos.Add(utxo);
//             }
//         }

//         return utxos;
//     }

//     public static bool IsSmartContractAddress(string address)
//     {
//         Address addressObj = new(address);
//         return addressObj.AddressType == AddressType.Script
//             || addressObj.AddressType == AddressType.ScriptWithScriptDelegation
//             || addressObj.AddressType == AddressType.ScriptWithPtrDelegation
//             || addressObj.AddressType == AddressType.EnterpriseScript
//             || addressObj.AddressType == AddressType.ScriptStake;
//     }

//     public static List<string> FilterSmartContractAddresses(List<string> addresses)
//     {
//         List<string> filteredAddresses = new();
//         foreach (string address in addresses)
//         {
//             if (!IsSmartContractAddress(address))
//                 filteredAddresses.Add(address);
//         }

//         return filteredAddresses;
//     }

//     public async static Task<List<Utxo>> CalculateInitialCandidates(
//         BlockfrostService blockfrostService,
//         string paymentAddress,
//         List<Utxo>? spentUtxos = null
//     )
//     {
//         (HashSet<Utxo> inputUtxos, HashSet<Utxo> outputUtxos) = await GetMempoolUtxos(blockfrostService, paymentAddress);
//         HashSet<Utxo> spentUtxoSet = new();
//         if (spentUtxos != null)
//             spentUtxoSet = new HashSet<Utxo>(spentUtxos);

//         List<Utxo> blockfrostUtxos = await blockfrostService.GetSingleAddressUtxos(paymentAddress);
//         List<Utxo> initialCandidateUtxos = TxChainingUtxos(
//             paymentAddress,
//             blockfrostUtxos,
//             inputUtxos,
//             outputUtxos,
//             spentUtxoSet,
//             TxChainingType.Chain
//         );
//         return initialCandidateUtxos;
//     }

//     public static List<Utxo> CalculateNewCandidates(string paymentAddress, List<Utxo> candidateUtxos, List<Transaction> transactions)
//     {
//         List<Utxo> newCandidates = new();
//         List<Utxo> tempCandidates = new();
//         tempCandidates.AddRange(candidateUtxos);

//         // Get Spent inputs and new Outputs into a Hashset
//         HashSet<Utxo> inputs = new();
//         HashSet<Utxo> outputs = new();
//         foreach (Transaction transaction in transactions)
//         {
//             if (transaction == null)
//                 continue;

//             foreach (TransactionInput input in transaction.TransactionBody!.TransactionInputs)
//             {
//                 Utxo utxo = new() { TxHash = input.TransactionId.ToStringHex(), TxIndex = input.TransactionIndex };
//                 inputs.Add(utxo);
//             }

//             string transactionId = HashUtility.Blake2b256(transaction.TransactionBody.Serialize(transaction.AuxiliaryData!)).ToStringHex();
//             uint outputIndex = 0;
//             foreach (TransactionOutput output in transaction.TransactionBody.TransactionOutputs)
//             {
//                 Utxo utxo =
//                     new()
//                     {
//                         TxHash = transactionId,
//                         TxIndex = outputIndex,
//                         Balance = output.Value.GetBalance(),
//                         OutputAddress = new Address(output.Address).ToString()!,
//                         OutputDatumOption = output.DatumOption
//                     };
//                 outputs.Add(utxo);

//                 outputIndex += 1;
//             }
//         }

//         foreach (Utxo utxo in outputs)
//         {
//             string address = new Address(utxo.OutputAddress).ToString()!;
//             if (address != paymentAddress)
//                 continue;

//             tempCandidates.Add(utxo);
//         }

//         foreach (Utxo tempCandidate in tempCandidates)
//         {
//             if (inputs.Contains(tempCandidate))
//                 continue;

//             newCandidates.Add(tempCandidate);
//         }

//         return newCandidates;
//     }

//     //---------------------------------------------------------------------------------------------------//
// }
