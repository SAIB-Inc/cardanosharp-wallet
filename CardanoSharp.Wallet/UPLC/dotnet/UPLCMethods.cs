using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.Utilities;

namespace CsBindgen
{
    public static class UPLCMethods
    {
        public static string ApplyParamsToPlutusScript(PlutusDataArray parameters, string plutusScriptCbor)
        {
            byte[] paramsArray = parameters.Serialize();
            byte[] plutusScriptBytes = plutusScriptCbor.HexToByteArray();
            nuint paramsLength = (nuint)paramsArray.Length;
            nuint plutusScriptLength = (nuint)plutusScriptBytes.Length;

            PlutusScriptResult result;
            string scriptHex;
            unsafe
            {
                fixed (byte* paramsPtr = &paramsArray[0])
                fixed (byte* plutusScriptPtr = &plutusScriptBytes[0])

                    result = UPLCNativeMethods.apply_params_to_plutus_script(paramsPtr, plutusScriptPtr, paramsLength, plutusScriptLength);
                if (!result.success)
                    return null!;

                byte[] byteArray = new byte[result.length];
                Marshal.Copy((IntPtr)result.value, byteArray, 0, (int)result.length);

                scriptHex = byteArray.ToStringHex();
            }

            return scriptHex;
        }

        public static TransactionEvaluation GetExUnits(Transaction transaction, NetworkType networkType)
        {
            byte[] txBytes = transaction.Serialize();

            List<byte[]> inputsList = new List<byte[]>();
            List<nuint> inputsLengthList = new List<nuint>();
            List<byte[]> resolvedInputsList = new List<byte[]>();
            List<nuint> resolvedInputsLengthList = new List<nuint>();
            foreach (TransactionInput input in transaction.TransactionBody.TransactionInputs)
            {
                byte[] inputBytes = input.Serialize();
                inputsList.Add(inputBytes);
                inputsLengthList.Add((nuint)inputBytes.Length);

                byte[] resolvedInputBytes = input.Output!.Serialize();
                resolvedInputsList.Add(resolvedInputBytes);
                resolvedInputsLengthList.Add((nuint)resolvedInputBytes.Length);
            }

            if (transaction.TransactionBody.ReferenceInputs != null)
            {
                foreach (TransactionInput referenceInput in transaction.TransactionBody.ReferenceInputs)
                {
                    byte[] referenceInputBytes = referenceInput.Serialize();
                    inputsList.Add(referenceInputBytes);
                    inputsLengthList.Add((nuint)referenceInputBytes.Length);

                    byte[] resolvedReferenceInputBytes = referenceInput.Output!.Serialize();
                    resolvedInputsList.Add(resolvedReferenceInputBytes);
                    resolvedInputsLengthList.Add((nuint)resolvedReferenceInputBytes.Length);
                }
            }

            byte[][] inputs = inputsList.ToArray();
            nuint[] inputsLength = inputsLengthList.ToArray();
            byte[][] resolvedInputs = resolvedInputsList.ToArray();
            nuint[] resolvedInputsLength = resolvedInputsLengthList.ToArray();

            byte[] costMdls = CostModelUtility.PlutusV2CostModel.Serialize();

            ulong initialBudgetMem = FeeStructure.MaxTxExMem;
            ulong initialBudgetStep = FeeStructure.MaxTxExSteps;

            SlotNetworkConfig slotNetworkConfig = SlotUtility.GetSlotNetworkConfig(networkType);
            ulong slotConfigZeroTime = (ulong)slotNetworkConfig.ZeroTime;
            ulong slotConfigZeroSlot = (ulong)slotNetworkConfig.ZeroSlot;
            uint slotConfigSlotLength = (uint)slotNetworkConfig.SlotLength;

            nuint txLength = (nuint)txBytes.Length;
            nuint length = (nuint)inputs.Length;
            nuint costMdlsLength = (nuint)costMdls.Length;

            ExUnitsResult result;
            byte[][] redeemersByteArray = null!;
            byte[] errorByteArray = null!;
            unsafe
            {
                byte** inputsPtr = ConvertByteArrayToByteArrayPointer(inputs);
                byte** resolvedInputsPtr = ConvertByteArrayToByteArrayPointer(resolvedInputs);
                fixed (byte* txPtr = &txBytes[0])
                fixed (byte* costMdlsPtr = &costMdls[0])
                fixed (nuint* inputsLengthPtr = &inputsLength[0])
                fixed (nuint* resolvedInputsLengthPtr = &resolvedInputsLength[0])

                    result = UPLCNativeMethods.get_ex_units(
                        txPtr,
                        inputsPtr,
                        resolvedInputsPtr,
                        costMdlsPtr,
                        initialBudgetMem,
                        initialBudgetStep,
                        slotConfigZeroTime,
                        slotConfigZeroSlot,
                        slotConfigSlotLength,
                        txLength,
                        length,
                        inputsLengthPtr,
                        resolvedInputsLengthPtr,
                        costMdlsLength
                    );
                if (!result.success)
                {
                    errorByteArray = ConvertByteArrayPointerToByteArray(result.error, result.error_length);
                }
                else
                {
                    redeemersByteArray = ConvertByteArrayPointerToByteArray(result.value, result.length, result.length_value);
                }
            }

            TransactionEvaluation evaluation = new TransactionEvaluation();
            if (redeemersByteArray != null)
            {
                List<Redeemer> redeemers = new List<Redeemer>();
                foreach (byte[] redeemerByteArray in redeemersByteArray)
                {
                    Redeemer redeemer = RedeemerExtensions.Deserialize(redeemerByteArray);
                    redeemers.Add(redeemer);
                }
                evaluation.Redeemers = redeemers;
            }
            else if (errorByteArray != null)
            {
                evaluation.Error = errorByteArray.ToStringUTF8();
            }

            return evaluation;
        }

        public unsafe static byte** ConvertByteArrayToByteArrayPointer(byte[][] bytes)
        {
            int numRows = bytes.Length;
            IntPtr[] pointers = new IntPtr[numRows];

            for (int i = 0; i < numRows; i++)
            {
                pointers[i] = Marshal.AllocHGlobal(bytes[i].Length);
                Marshal.Copy(bytes[i], 0, pointers[i], bytes[i].Length);
            }

            byte** bytePtr = (byte**)Marshal.AllocHGlobal(numRows * sizeof(IntPtr));

            for (int i = 0; i < numRows; i++)
            {
                ((IntPtr*)bytePtr)[i] = pointers[i];
            }

            return bytePtr;
        }

        public unsafe static byte[][] ConvertByteArrayPointerToByteArray(byte** value, nuint length, nuint* length_value)
        {
            byte[][] result = new byte[length][];
            for (nuint i = 0; i < length; i++)
            {
                int rowLength = (int)length_value[i];
                result[i] = new byte[rowLength];
                Marshal.Copy((IntPtr)value[i], result[i], 0, rowLength);
            }

            return result;
        }

        public unsafe static byte[] ConvertByteArrayPointerToByteArray(byte* value, nuint length)
        {
            byte[] result = new byte[length];
            IntPtr tempPtr = (IntPtr)value;
            Marshal.Copy(tempPtr, result, 0, (int)length);
            UPLCNativeMethods.free_rust_string((byte*)tempPtr); // free the memory allocated by Rust
            return result;
        }
    }

    public class TransactionEvaluation
    {
        public List<Redeemer>? Redeemers { get; set; }
        public string? Error { get; set; }

        public override string ToString()
        {
            if (Error != null)
                return Error;

            string returnString = "";
            if (Redeemers != null)
            {
                foreach (Redeemer redeemer in Redeemers)
                    returnString += $"Redeemer: {redeemer.Tag} {redeemer.Index} {{mem: {redeemer.ExUnits.Mem}, step: {redeemer.ExUnits.Steps}}} \n";
            }

            return returnString;
        }
    }
}
