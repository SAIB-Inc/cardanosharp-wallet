﻿using System;
using System.Linq;
using System.Text.Json;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Derivations;
using CardanoSharp.Wallet.Models.Keys;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Xunit;

namespace CardanoSharp.Wallet.Test
{
    /// <summary>
    /// HD derivation can be seen as a tree with many branches, where keys live at each node and leaf
    /// such that an entire sub-tree can be recovered from only a parent key (and seemingly,
    /// the whole tree can be recovered from the root master key).
    ///
    /// <para>
    ///     References:
    ///     * HD Wallets: https://input-output-hk.github.io/adrestia/docs/key-concepts/hierarchical-deterministic-wallets/
    ///     * Deriving new keys from parent keys: https://input-output-hk.github.io/adrestia/user-guide/Ed25519_BIP.pdf
    /// </para>
    /// </summary>
    public class DerivationTests
    {
        private readonly IMnemonicService _keyService;
        const string __mnemonic = "art forum devote street sure rather head chuckle guard poverty release quote oak craft enemy";

        public DerivationTests()
        {
            _keyService = new MnemonicService();
        }

        [Theory]
        [InlineData(__mnemonic)]
        public void HDPolicyKeysDerivationTest(string words)
        {
            // Arrange
            var mnemonic = new MnemonicService().Restore(words);
            var rootKey = mnemonic.GetRootKey();

            (var paymentPrv1, var paymentPub1) = getKeyPairFromPath("m/1855'/1815'/0'", rootKey);

            // Act
            // Fluent derivation API
            var derivation = rootKey.Derive()                   // IMasterNodeDerivation
                .Derive(PurposeType.PolicyKeys)    // IPurposeNodeDerivation
                .Derive(CoinType.Ada)           // ICoinNodeDerivation
                .Derive(0);                      // IAccountNodeDerivation

            // Assert
            Assert.Equal(paymentPrv1.Key, derivation.PrivateKey.Key);
            Assert.Equal(paymentPrv1.Chaincode, derivation.PrivateKey.Chaincode);

            Assert.Equal(paymentPub1.Key, derivation.PublicKey.Key);
            Assert.Equal(paymentPub1.Chaincode, derivation.PublicKey.Chaincode);
        }

        [Theory]
        [InlineData(__mnemonic)]
        public void PathDerivationTest(string words)
        {
            // Arrange
            var mnemonic = new MnemonicService().Restore(words);
            var rootKey = mnemonic.GetRootKey();
            var testKey = getTestRootKey(mnemonic);

            (var paymentPrv1, var paymentPub1) = getKeyPairFromPath("m/1852'/1815'/0'/0/0", rootKey);

            // Act
            // Fluent derivation API
            var derivation = testKey.Derive()                   // IMasterNodeDerivation
                .Derive(PurposeType.Shelley)    // IPurposeNodeDerivation
                .Derive(CoinType.Ada)           // ICoinNodeDerivation
                .Derive(0)                      // IAccountNodeDerivation
                .Derive(RoleType.ExternalChain) // IRoleNodeDerivation
                .Derive(0);                     // IIndexNodeDerivation

            // Assert
            Assert.Equal(paymentPrv1.Key, derivation.PrivateKey.Key);
            Assert.Equal(paymentPrv1.Chaincode, derivation.PrivateKey.Chaincode);

            Assert.Equal(paymentPub1.Key, derivation.PublicKey.Key);
            Assert.Equal(paymentPub1.Chaincode, derivation.PublicKey.Chaincode);
        }


        [Theory]
        [InlineData(__mnemonic, "m/1852'")]
        [InlineData(__mnemonic, "m/1852'/1815'")]
        [InlineData(__mnemonic, "m/1852'/1815'/0'")]
        [InlineData(__mnemonic, "m/1852'/1815'/0'/0")]
        [InlineData(__mnemonic, "m/1852'/1815'/0'/0/0")]
        public void PartialPathDerivationTest(string words, string path)
        {
            var depth = path.Split("/").Slice(1).Count(); // strip master node to calc zero based depth

            // create two payment addresses from same root key
            //arrange
            var mnemonic = new MnemonicService().Restore(words);
            var rootKey = mnemonic.GetRootKey();
            var testKey = getTestRootKey(mnemonic);

            (var prv, var pub) = getKeyPairFromPath(path, rootKey);

            // Act
            // Fluent derivation API
            var master = testKey.Derive();
            var purpose = master.Derive(PurposeType.Shelley);
            var coin = purpose.Derive(CoinType.Ada);
            var account = coin.Derive(0);
            var role = account.Derive(RoleType.ExternalChain);
            var index = role.Derive(0);

            if (depth == 1) AssertDerivedKeys(prv, pub, purpose);
            if (depth == 2) AssertDerivedKeys(prv, pub, coin);
            if (depth == 3) AssertDerivedKeys(prv, pub, account);
            if (depth == 4) AssertDerivedKeys(prv, pub, role);
            if (depth == 5) AssertDerivedKeys(prv, pub, index);
        }

        /// <summary>
        /// Use Case:
        /// I'm writing a light client (similar to Yoroi, Nami, C64Minter, etc).
        /// User enters in the Mnemonic and Spending Password.
        /// I derive down to the Account level and get the Public Key.
        /// I use the Spending Password to 2-Way encrypt the Private Key and then store both the encrypted Private Key and the plain Public Key.
        /// </summary>
        [Theory]
        [InlineData(__mnemonic)]
        public void AccountKeyDerivation_UsingExtensionMethods(string words)
        {
            // Arrange
            var accountPath = WalletPath.Parse("m/1852'/1815'/0'");
            var paymentPath = WalletPath.Parse("0/1");

            // User enters in the Mnemonic and Spending Password.
            var mnemonic = _keyService.Restore(words);
            PrivateKey rootKey = mnemonic.GetRootKey();

            var account = rootKey
                .Derive() // m
                .Derive(accountPath.Purpose)
                .Derive(accountPath.Coin)
                .Derive(accountPath.AccountIndex);

            account.SetPublicKey();

            // use the Spending Password to 2-Way encrypt the Private Key
            // and then store both the encrypted Private Key and the plain Public Key.
            var blob = new Tuple<PrivateKey, PublicKey>(account.PrivateKey.Encrypt("password"), account.PublicKey);
            var store = JsonSerializer.Serialize(blob);

            // The user wants to generate a random/incremented address.
            // I take the Public Key and Derive (0/1).
            // This should be used to test the Public Key Derivation
            var pub = account.PublicKey
                .Derive(paymentPath.Role)
                .Derive(paymentPath.Index);

            // The user now wants to send a transaction.
            // They enter in the ADDR, Amount to Send, and their Spending Password.
            // I can now decrypt the Account Private Key...
            var load = JsonSerializer.Deserialize<Tuple<PrivateKey, PublicKey>>(store);
            var loadedPrv = load.Item1.Decrypt("password");

            // ...and derive down to the "0/1" Index Private Key.
            // This allows me to now sign the Transaction.
            var index = loadedPrv
                .Derive(paymentPath.Role)
                .Derive(paymentPath.Index);

            // Assert
            var prv = rootKey.Derive("m/1852'/1815'/0'/0/1");
            AssertDerivedKeys(prv, pub.PublicKey, index);

            Assert.Null(pub.PrivateKey);
        }

        /// <summary>
        /// Use Case:
        /// I'm writing a light client (similar to Yoroi, Nami, C64Minter, etc).
        /// User enters in the Mnemonic and Spending Password.
        /// I derive down to the Account level and get the Public Key.
        /// I use the Spending Password to 2-Way encrypt the Private Key and then store both the encrypted Private Key and the plain Public Key.
        /// </summary>
        [Theory]
        [InlineData(__mnemonic)]
        public void AccountKeyDerivation_UsingBaseKeyMethods(string words)
        {
            // Arrange
            var accountPath = "m/1852'/1815'/0'";
            var paymentPath = "0/1";

            // User enters in the Mnemonic and Spending Password.
            var mnemonic = _keyService.Restore(words);
            PrivateKey rootKey = mnemonic.GetRootKey();

            var accountPrv = rootKey.Derive(accountPath);
            var accountPub = accountPrv.GetPublicKey(false);

            // use the Spending Password to 2-Way encrypt the Private Key
            // and then store both the encrypted Private Key and the plain Public Key.
            var blob = new Tuple<PrivateKey, PublicKey>(accountPrv.Encrypt("password"), accountPub);
            var store = JsonSerializer.Serialize(blob);

            // The user wants to generate a random/incremented address.
            // I take the Public Key and Derive (0/1).
            // This should be used to test the Public Key Derivation
            var indexPub = accountPub.Derive(paymentPath);

            // The user now wants to send a transaction.
            // They enter in the ADDR, Amount to Send, and their Spending Password.
            // I can now decrypt the Account Private Key...
            var load = JsonSerializer.Deserialize<Tuple<PrivateKey, PublicKey>>(store);
            var loadedPrv = load.Item1.Decrypt("password");

            // ...and derive down to the "0/1" Index Private Key.
            // This allows me to now sign the Transaction.
            var indexPrv = loadedPrv.Derive(paymentPath);

            // Assert
            var fullPathPrv = rootKey.Derive("m/1852'/1815'/0'/0/1");
            var fullPathPub = fullPathPrv.GetPublicKey(false);

            // Assert
            Assert.Equal(indexPrv.Key, fullPathPrv.Key);
            Assert.Equal(indexPrv.Chaincode, fullPathPrv.Chaincode);

            Assert.Equal(indexPub.Key, fullPathPub.Key);
            Assert.Equal(indexPub.Chaincode, fullPathPub.Chaincode);
        }

        [Theory]
        [InlineData(__mnemonic)]
        public void AccountKeyDerivation_UsingExplicitNodeDerivations(string words)
        {
            var accountPath = WalletPath.Parse("m/1852'/1815'/0'");
            var paymentPath = WalletPath.Parse("0/1");

            // User enters in the Mnemonic and Spending Password.
            var mnemonic = _keyService.Restore(words);
            PrivateKey rootKey = mnemonic.GetRootKey();

            var account = new MasterNodeDerivation(rootKey)
                .Derive(accountPath.Purpose)
                .Derive(accountPath.Coin)
                .Derive(accountPath.AccountIndex);

            account.SetPublicKey();

            // use the Spending Password to 2-Way encrypt the Private Key
            // and then store both the encrypted Private Key and the plain Public Key.
            var blob = new Tuple<PrivateKey, PublicKey>(account.PrivateKey.Encrypt("password"), account.PublicKey);
            var store = JsonSerializer.Serialize(blob);

            // The user wants to generate a random/incremented address.
            // I take the Public Key and Derive (0/1).
            var roleNodePub = new RoleNodeDerivation(account.PublicKey, paymentPath.Role);
            var payment = roleNodePub.Derive(paymentPath.Index);

            // The user now wants to send a transaction.
            // They enter in the ADDR, Amount to Send, and their Spending Password.
            // I can now decrypt the Account Private Key...
            var load = JsonSerializer.Deserialize<Tuple<PrivateKey, PublicKey>>(store);
            var loadedPrv = load.Item1.Decrypt("password");

            // ...and derive down to the "0/1" Index Private Key.
            // This allows me to now sign the Transaction.
            var roleNodePrv = new RoleNodeDerivation(loadedPrv, paymentPath.Role);
            var index = roleNodePrv.Derive(paymentPath.Index);

            // Assert
            var prv = rootKey.Derive("m/1852'/1815'/0'/0/1");
            AssertDerivedKeys(prv, prv.GetPublicKey(false), index);

            Assert.Null(payment.PrivateKey);
        }

        private static void AssertDerivedKeys(PrivateKey prv, PublicKey pub, IPathDerivation derivation)
        {
            // Assert
            Assert.Equal(prv.Key, derivation.PrivateKey.Key);
            Assert.Equal(prv.Chaincode, derivation.PrivateKey.Chaincode);

            Assert.Equal(pub.Key, derivation.PublicKey.Key);
            Assert.Equal(pub.Chaincode, derivation.PublicKey.Chaincode);
        }

        /// <summary>
        /// Getting the key from path as descibed in https://github.com/cardano-foundation/CIPs/blob/master/CIP-1852/CIP-1852.md
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rootKey"></param>
        /// <returns></returns>
        private (PrivateKey, PublicKey) getKeyPairFromPath(string path, PrivateKey rootKey)
        {
            var privateKey = rootKey.Derive(path);
            return (privateKey, privateKey.GetPublicKey(false));
        }

        private PrivateKey getTestRootKey(Mnemonic mnemonic, string password = "")
        {
            var rootKey = KeyDerivation.Pbkdf2(password, mnemonic.Entropy, KeyDerivationPrf.HMACSHA512, 4096, 96);
            rootKey[0] &= 248;
            rootKey[31] &= 31;
            rootKey[31] |= 64;

            return new PrivateKey(rootKey.Slice(0, 64), rootKey.Slice(64));
        }

        #region AccountDiscovery
        /// <summary>
        /// Path levels
        /// <para>
        /// Cardano wallet defines the following path levels:
        /// m / purpose_H / coin_type_H / account_H / account_type / address_index
        /// </para>
        /// <para>
        /// purpose_H is set to 1852'
        /// </para>
        /// <para>
        /// coin_type_H is set to 1815'
        /// </para>
        /// <para>
        /// account_H is set to 0'
        /// </para>
        /// <para>
        /// account_type is either
        /// <para>0 to indicate an address on the external chain (public)</para>
        /// <para>1 to indicate an address on the inernal chain (change)</para>
        /// <para>2 to indicate a reward account address (delegation)</para>
        /// </para>
        /// <para>
        /// address_index is either
        /// * 0 if the account_type is 2
        /// * 0 - 2^31 otherwise
        /// </para>
        /// </summary>
        // [Fact]
        // public void AccountDiscoveryTest()
        // {
        //     // arrange
        //     var walletService = new WalletService();
        //     var keyService = new MnemonicService();
        //     var wordlist = Enums.WordLists.Japanese;
        //     var mnemonic = keyService.Generate(24, wordlist);

        //     // act
        //     var accounts = walletService.DiscoverAccounts(mnemonic);

        //     // assert
        //     Assert.NotNull(accounts);
        // }
        #endregion
    }
}
