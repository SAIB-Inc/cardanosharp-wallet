using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoSharp.Wallet.Utilities;
using CsBindgen;
using Xunit;
using Xunit.Abstractions;

namespace CardanoSharp.Wallet.Test
{
    public class UPLCTests
    {
        [Fact]
        public void NativeParameterizedContractTest()
        {
            string expectedScriptCBORHex =
                "5902920100003323232323232323232323232323232232222533300c323232323232323232323232323232533302030230021323232533301e3370e9000000899299980f99b87003480084cdc780200b8a50301c32533301f3370e9000180f00088008a99810a4812a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e001632323300100d23375e6603a603e002900000c18008009112999813001099ba5480092f5c026464a6660466006004266e952000330290024bd700999802802800801981500198140010a99980f19b8700233702900024004266e3c00c058528180e0099bad3020002375c603c0022a6603a9201334c6973742f5475706c652f436f6e73747220636f6e7461696e73206d6f7265206974656d73207468616e206578706563746564001630210013200132323232533301d3370e90010008a5eb7bdb1804c8c8004dd59812800980d801180d8009980080180518008009112999810801099ba5480092f5c0264646464a66604066e3c0140044cdd2a40006604c6e980092f5c0266600e00e00600a6eb8c08800cdd59811001181280198118011bab301f001301f001301e001301d001301c00237586034002602000a6eb8c060004c0394ccc040cdc3a4000601e00220022a660249212a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e0016301600130160023014001300a001149858dd7000980080091129998068010a4c2660126002601e00466600600660200040026600200290001111199980399b8700100300e233330050053370000890011808000801001118039baa001230053754002ae695cdab9c5573aaae7955cfaba05742ae89300104439873450001";

            // Parameters
            // This is the Aiken CBOr from the "gift card" example
            string compiledAikenCBOR =
                "590288010000323232323232323232323232323232232222533300c323232323232323232323232323232533302030230021323232533301e3370e9000000899299980f99b87003480084cdc780200b8a50301c32533301f3370e9000180f00088008a99810a492a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e001632323300100d23375e6603a603e002900000c18008009112999813001099ba5480092f5c026464a6660466006004266e952000330290024bd700999802802800801981500198140010a99980f19b8700233702900024004266e3c00c058528180e0099bad3020002375c603c0022a6603a9201334c6973742f5475706c652f436f6e73747220636f6e7461696e73206d6f7265206974656d73207468616e206578706563746564001630210013200132323232533301d3370e90010008a5eb7bdb1804c8c8004dd59812800980d801180d8009980080180518008009112999810801099ba5480092f5c0264646464a66604066e3c0140044cdd2a40006604c6e980092f5c0266600e00e00600a6eb8c08800cdd59811001181280198118011bab301f001301f001301e001301d001301c00237586034002602000a6eb8c060004c0394ccc040cdc3a4000601e00220022a660249212a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e0016301600130160023014001300a001149858dd7000980080091129998068010a4c2660126002601e00466600600660200040026600200290001111199980399b8700100300e233330050053370000890011808000801001118039baa001230053754002ae695cdab9c5573aaae7955cfaba05742ae89";

            // Plutus TokenName data
            byte[] tokenName = "987345".HexToByteArray();
            PlutusDataBytes plutusDataBytes = new PlutusDataBytes() { Value = tokenName };
            IPlutusData[] plutusDataParameters = new IPlutusData[] { plutusDataBytes };
            PlutusDataArray plutusDataArray = new PlutusDataArray() { Value = plutusDataParameters };

            // Get Untyped Plutus Core (UPLC) Function Parameters
            byte[] paramsArray = plutusDataArray.Serialize();
            byte[] plutusScriptArray = compiledAikenCBOR.HexToByteArray();

            nuint paramsLength = (nuint)paramsArray.Length;
            nuint plutusScriptLength = (nuint)plutusScriptArray.Length;

            PlutusScriptResult result;
            string actualScriptCBORHex = "";
            unsafe
            {
                fixed (byte* paramsPtr = &paramsArray[0])
                fixed (byte* plutusScriptPtr = &plutusScriptArray[0])

                    result = UPLCNativeMethods.apply_params_to_plutus_script(paramsPtr, plutusScriptPtr, paramsLength, plutusScriptLength);

                byte[] byteArray = new byte[result.length];
                Marshal.Copy((IntPtr)result.value, byteArray, 0, (int)result.length);

                actualScriptCBORHex = byteArray.ToStringHex();
            }
            ;

            Assert.Equal(expectedScriptCBORHex, actualScriptCBORHex);
        }

        [Fact]
        public void ParameterizedContractTest()
        {
            string expectedScriptCBORHex =
                "5902920100003323232323232323232323232323232232222533300c323232323232323232323232323232533302030230021323232533301e3370e9000000899299980f99b87003480084cdc780200b8a50301c32533301f3370e9000180f00088008a99810a4812a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e001632323300100d23375e6603a603e002900000c18008009112999813001099ba5480092f5c026464a6660466006004266e952000330290024bd700999802802800801981500198140010a99980f19b8700233702900024004266e3c00c058528180e0099bad3020002375c603c0022a6603a9201334c6973742f5475706c652f436f6e73747220636f6e7461696e73206d6f7265206974656d73207468616e206578706563746564001630210013200132323232533301d3370e90010008a5eb7bdb1804c8c8004dd59812800980d801180d8009980080180518008009112999810801099ba5480092f5c0264646464a66604066e3c0140044cdd2a40006604c6e980092f5c0266600e00e00600a6eb8c08800cdd59811001181280198118011bab301f001301f001301e001301d001301c00237586034002602000a6eb8c060004c0394ccc040cdc3a4000601e00220022a660249212a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e0016301600130160023014001300a001149858dd7000980080091129998068010a4c2660126002601e00466600600660200040026600200290001111199980399b8700100300e233330050053370000890011808000801001118039baa001230053754002ae695cdab9c5573aaae7955cfaba05742ae89300104439873450001";

            // Parameters
            // This is the Aiken CBOr from the "gift card" example
            string compiledAikenCBOR =
                "590288010000323232323232323232323232323232232222533300c323232323232323232323232323232533302030230021323232533301e3370e9000000899299980f99b87003480084cdc780200b8a50301c32533301f3370e9000180f00088008a99810a492a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e001632323300100d23375e6603a603e002900000c18008009112999813001099ba5480092f5c026464a6660466006004266e952000330290024bd700999802802800801981500198140010a99980f19b8700233702900024004266e3c00c058528180e0099bad3020002375c603c0022a6603a9201334c6973742f5475706c652f436f6e73747220636f6e7461696e73206d6f7265206974656d73207468616e206578706563746564001630210013200132323232533301d3370e90010008a5eb7bdb1804c8c8004dd59812800980d801180d8009980080180518008009112999810801099ba5480092f5c0264646464a66604066e3c0140044cdd2a40006604c6e980092f5c0266600e00e00600a6eb8c08800cdd59811001181280198118011bab301f001301f001301e001301d001301c00237586034002602000a6eb8c060004c0394ccc040cdc3a4000601e00220022a660249212a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e0016301600130160023014001300a001149858dd7000980080091129998068010a4c2660126002601e00466600600660200040026600200290001111199980399b8700100300e233330050053370000890011808000801001118039baa001230053754002ae695cdab9c5573aaae7955cfaba05742ae89";

            // Plutus TokenName data
            byte[] tokenName = "987345".HexToByteArray();
            PlutusDataBytes plutusDataBytes = new PlutusDataBytes() { Value = tokenName };
            IPlutusData[] plutusDataParameters = new IPlutusData[] { plutusDataBytes };
            PlutusDataArray plutusDataArray = new PlutusDataArray() { Value = plutusDataParameters };

            string actualScriptCBORHex = UPLCMethods.ApplyParamsToPlutusScript(plutusDataArray, compiledAikenCBOR);
            Assert.Equal(expectedScriptCBORHex, actualScriptCBORHex);
        }

        private readonly ITestOutputHelper _output;

        public UPLCTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void LocalExUnitsTest()
        {
            /*
            Evaluation has {"memory":2301,"steps":586656 }
            */
            string alwaysSucceedTx =
                "84aa0081825820332b6b1c2fecaf5bf92d174ed1824d08aaf2adda60584c603eb04d381bfc96eb00018282583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af1a004c4b4082583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af1a00495a83021a0002f0bd031a01fa2d50081a01fa1f400b5820b7b74b4c9ce6c59f13e41c6204bc52607cea9a80230717f1f9c4c3e807c642240d81825820332b6b1c2fecaf5bf92d174ed1824d08aaf2adda60584c603eb04d381bfc96eb011082583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af821b000000037693cae7a1581c713f4553aa0dc44da1e60c611631a25bc4b02a36f54808df26d9ec20a14288881a000186a0111a003d090012818258207066561cc055011baa4bbf1fc30c96fae92e6a9d3728a162a6b5050e50e1354200a2008182582047153df6f377305107ec04d588cb2efef05596b081f6ec7d486bec0d1e37aed65840db1f5a92408550bcf833518b6c8066fbc1eca6abacdb3cd3bb7253bd3a12340d8afa21311bf6ae549595573037f1a4f208a369c9100adf96574816cf6d65330f0581840000d87980821909711a00096635f5f6";

            // Create the transaction and resolved inputs
            Transaction transaction = alwaysSucceedTx.HexToByteArray().DeserializeTransaction();

            PlutusDataConstr constr = new PlutusDataConstr
            {
                Alternative = 0,
                Value = new PlutusDataArray { Value = new IPlutusData[] { } }
            };
            DatumOption datum = new DatumOption() { Data = constr };
            TransactionOutput resolvedInput = new TransactionOutput()
            {
                Address = new Address("addr_test1wzg9jffqkv5luz8sayu5dmx5qhjfkayq090z0jmp3uqzmzq480snu").GetBytes(),
                Value = new TransactionOutputValue { Coin = (ulong)(10 * CardanoUtility.adaToLovelace) },
                DatumOption = datum
            };
            transaction.TransactionBody.TransactionInputs[0].Output = resolvedInput;

            // Resolve Reference Input
            TransactionOutput resolvedReferenceInput = new TransactionOutput()
            {
                Address = new Address("addr_test1wzg9jffqkv5luz8sayu5dmx5qhjfkayq090z0jmp3uqzmzq480snu").GetBytes(),
                Value = new TransactionOutputValue { Coin = (ulong)(50 * CardanoUtility.adaToLovelace) },
                ScriptReference = new ScriptReference
                {
                    // CBOR here is NOT double encoded
                    PlutusV2Script = new PlutusV2Script { script = "500100003222253330044a22930b2b9a01".HexToByteArray() }
                }
            };
            transaction.TransactionBody.ReferenceInputs[0].Output = resolvedReferenceInput;

            TransactionEvaluation redeemerResult = UPLCMethods.GetExUnits(transaction, Enums.NetworkType.Preprod);
            List<Redeemer> redeemers = redeemerResult.Redeemers;

            Assert.True(redeemers != null);
            Assert.True(redeemers.Count == 1);
            Assert.True(redeemers[0].ExUnits.Mem == 2301);
            Assert.True(redeemers[0].ExUnits.Steps == 586656);
        }

        [Fact]
        public void LocalFailTraceExUnitsTest()
        {
            string alwaysFailWithTraceTx =
                "84aa00818258200817c4d930fa267a9aae7d63492b83ea5afd55d0ec14e1c0dcb37fdd0aa5053d00018282583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af1a004c4b4082583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af1a00320653021a001a44ed031a01fa4520081a01fa37100b582043b31788c13c33b43109980f166b1d109b449e1c47c74de8d26461125f835b8b0d81825820332b6b1c2fecaf5bf92d174ed1824d08aaf2adda60584c603eb04d381bfc96eb011082583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af821b000000037693cae7a1581c713f4553aa0dc44da1e60c611631a25bc4b02a36f54808df26d9ec20a14288881a000186a0111a003d09001281825820d975e3832afffb5cb17d287a7e8a13045afb2a6afcd47c3b15bc924e8770bd2700a2008182582047153df6f377305107ec04d588cb2efef05596b081f6ec7d486bec0d1e37aed65840bb71f7442d81e41625ee66956f0aebac97f8a703cce0fab5e40e236dbd4a500b08494d8e851218b2689bf6ae9eab1738fd91631f6647cf4f8a48985c3f9da9020581840000d87980821a00d59f801b00000002540be400f5f6";

            // Create the transaction and resolved inputs
            Transaction transaction = alwaysFailWithTraceTx.HexToByteArray().DeserializeTransaction();

            PlutusDataConstr constr = new PlutusDataConstr
            {
                Alternative = 0,
                Value = new PlutusDataArray { Value = new IPlutusData[] { } }
            };
            DatumOption datum = new DatumOption() { Data = constr };
            TransactionOutput resolvedInput = new TransactionOutput()
            {
                Address = new Address("addr_test1wp35ns6m2qnpk93xdem055jw2amc29ygqe8x3xumxjqzffsw6eykl").GetBytes(),
                Value = new TransactionOutputValue { Coin = (ulong)(10 * CardanoUtility.adaToLovelace) },
                DatumOption = datum
            };
            transaction.TransactionBody.TransactionInputs[0].Output = resolvedInput;

            // Resolve Reference Input
            TransactionOutput resolvedReferenceInput = new TransactionOutput()
            {
                Address = new Address("addr_test1wp35ns6m2qnpk93xdem055jw2amc29ygqe8x3xumxjqzffsw6eykl").GetBytes(),
                Value = new TransactionOutputValue { Coin = (ulong)(1.09905 * CardanoUtility.adaToLovelace) },
                ScriptReference = new ScriptReference
                {
                    // CBOR here is NOT double encoded
                    PlutusV2Script = new PlutusV2Script
                    {
                        script = "582d0100003232222533300453330044a0294454cc0152410c626c6f62203f2046616c73650014a02930b2b9a57381".HexToByteArray()
                    }
                }
            };
            transaction.TransactionBody.ReferenceInputs[0].Output = resolvedReferenceInput;

            TransactionEvaluation redeemerResult = UPLCMethods.GetExUnits(transaction, Enums.NetworkType.Preprod);
            Assert.True(redeemerResult.Error != null);
            Assert.Equal(
                "Redeemer (Spend, 0): The provided Plutus code called 'error'.\n\nExBudget {\n    mem: 134,\n    cpu: 373554,\n}\n\nblob ? False",
                redeemerResult.Error
            );
        }

        [Fact]
        public void LocalExUnitsTestTwo()
        {
            /*
            Evaluation has {"memory":81180,"steps":30977210 }asdasd
            */
            string tx =
                "84aa00828258205a46bc0cc168f1e3d83a374d39eb690373cd78bb8d83044f213120e3e524a04d00825820a399e577ef6706e54a8c12628aaed74c344e63cc9c62c083378ff8a47d2d9a8a01018382583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af1a00989680a300581d702175cd0ced46e4388cb4247b5a16e801abdace48e15ab1356b00afa9011a00b71b00028201d81844d879810182583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af821b0000000367783487a2581c713f4553aa0dc44da1e60c611631a25bc4b02a36f54808df26d9ec20a14288881a0001869f581c90592520b329fe08f0e93946ecd405e49b7480795e27cb618f002d88a142888801021a001a56cd031a02023bf5081a02022de50b58205c87b702651e9e31f087a3685e20f9c1f233c70ba296b927be337261c48858930d81825820a399e577ef6706e54a8c12628aaed74c344e63cc9c62c083378ff8a47d2d9a8a011082583900822e6c88ec6e1bf5358a3de9652c41f323dc2c0be9043afd213dc675aec396d617e9e377262302acfd8f7f86cad0b45dd102bec154c581af821b00000003680c9d54a2581c713f4553aa0dc44da1e60c611631a25bc4b02a36f54808df26d9ec20a14288881a0001869f581c90592520b329fe08f0e93946ecd405e49b7480795e27cb618f002d88a142888801111a003d09001281825820932ff882cb14f112fdfe306efcde152d09f902cd823154538b89f9ee1ce36c2100a2008182582047153df6f377305107ec04d588cb2efef05596b081f6ec7d486bec0d1e37aed6584087b0f8868be55029d3a11bfa17c8a191e69d7e5cb22f8e1639bc57e21d3001dd6c20054fb3f3f508cca1217fe22d55b0fb2bb28af45cb4a27dc90f57f77ead0a0581840000d879814412345678821a00d59f801b00000002540be400f5f6";
            string scriptCbor =
                "590305010000323232323232323232323232322223232533300b32323232533300f3370e6eb4cc02cc0340092000375a66016601a00a9000099b8f375c66016601a00c9000244104123456780014a060120026026002601464a66601a66e1d2004300c0011001153300e49012a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e001633008300a33008300a0014800920043323223232323017001300e3253330113370e9000180800088008a998092492a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e001633004375866018601c66018601c006900024000466ebccc034c03c00520000023015001300c32533300f3370e9001180700088008a998082492a4578706563746564206f6e20696e636f727265637420636f6e7374727563746f722076617269616e742e00163300a300c00148008c0040048894ccc0480085300103d87a8000132325333010300300213374a90001980a80125eb804ccc01401400400cc05800cc05000800c526163200530050043200332533300a3370e900000089919299980818098010a4c2a6601a921334c6973742f5475706c652f436f6e73747220636f6e7461696e73206d6f7265206974656d73207468616e2065787065637465640016375c602200260100062a660169212b436f6e73747220696e64657820646964206e6f74206d6174636820616e7920747970652076617269616e740016300800223253330073370e900000089919299980698080010a4c2a66014921334c6973742f5475706c652f436f6e73747220636f6e7461696e73206d6f7265206974656d73207468616e2065787065637465640016375a601c002600a0042a660109212b436f6e73747220696e64657820646964206e6f74206d6174636820616e7920747970652076617269616e740016300500133001001480008888cccc01ccdc38008018069199980280299b8000448008c03c0040080088c018dd5000918021baa0015734ae7155ceaab9e5573eae815d0aba21";

            // Create the transaction and resolved inputs
            Transaction transaction = tx.HexToByteArray().DeserializeTransaction();

            PlutusDataConstr constr = new PlutusDataConstr
            {
                Alternative = 0,
                Value = new PlutusDataArray { Value = new IPlutusData[] { new PlutusDataInt { Value = 1 } } }
            };
            DatumOption datum = new DatumOption() { Data = constr };
            TransactionOutput resolvedInput = new TransactionOutput()
            {
                Address = new Address("addr_test1wqshtngva4rwgwyvksj8kkskaqq6hkkwfrs44vf4dvq2l2g9f98xt").GetBytes(),
                Value = new TransactionOutputValue { Coin = (ulong)(10 * CardanoUtility.adaToLovelace) },
                DatumOption = datum
            };
            transaction.TransactionBody.TransactionInputs[0].Output = resolvedInput;

            TokenBundleBuilder tokenBundleBuilder = (TokenBundleBuilder)
                TokenBundleBuilder.Create
                    .AddToken("90592520b329fe08f0e93946ecd405e49b7480795e27cb618f002d88".HexToByteArray(), "8888".HexToByteArray(), 1)
                    .AddToken("713f4553aa0dc44da1e60c611631a25bc4b02a36f54808df26d9ec20".HexToByteArray(), "8888".HexToByteArray(), 99999);
            TransactionOutput resolvedInput2 = new TransactionOutput()
            {
                Address = new Address(
                    "addr_test1qzpzumyga3hphaf43g77jefvg8ej8hpvp05sgwhayy7uvadwcwtdv9lfudmjvgcz4n7c7luxetgtghw3q2lvz4x9sxhsctks99"
                ).GetBytes(),
                Value = new TransactionOutputValue
                {
                    Coin = (ulong)(14620.832903 * CardanoUtility.adaToLovelace),
                    MultiAsset = tokenBundleBuilder.Build()
                }
            };
            transaction.TransactionBody.TransactionInputs[1].Output = resolvedInput2;

            // Resolve Reference Input
            TransactionOutput resolvedReferenceInput = new TransactionOutput()
            {
                Address = new Address("addr_test1wqshtngva4rwgwyvksj8kkskaqq6hkkwfrs44vf4dvq2l2g9f98xt").GetBytes(),
                Value = new TransactionOutputValue { Coin = (ulong)(50 * CardanoUtility.adaToLovelace) },
                ScriptReference = new ScriptReference
                {
                    // CBOR here is NOT double encoded
                    PlutusV2Script = new PlutusV2Script { script = scriptCbor.HexToByteArray() }
                }
            };
            transaction.TransactionBody.ReferenceInputs[0].Output = resolvedReferenceInput;

            TransactionEvaluation redeemerResult = UPLCMethods.GetExUnits(transaction, Enums.NetworkType.Preprod);
            List<Redeemer> redeemers = redeemerResult.Redeemers;

            Assert.True(redeemers != null);
            Assert.True(redeemers.Count == 1);
            Assert.True(redeemers[0].ExUnits.Mem == 81180);
            Assert.True(redeemers[0].ExUnits.Steps == 30977210);
        }
    }
}
