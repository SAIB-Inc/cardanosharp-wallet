using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.TransactionBuilding;
using PeterO.Cbor2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardanoSharp.Wallet.Extensions.Models.Certificates;

public static class CertificateExtensions
{
    public static CBORObject GetCBOR(this Certificate certificate)
    {
        /*
        certificate =
        [stake_registration
        // stake_deregistration
        // stake_delegation
        // pool_registration
        // pool_retirement
        // genesis_key_delegation
        // move_instantaneous_rewards_cert
        ]

        stake_registration = (0, stake_credential)
        stake_deregistration = (1, stake_credential)
        stake_delegation = (2, stake_credential, pool_keyhash)
        pool_registration = (3, pool_params)
        pool_retirement = (4, pool_keyhash, epoch)
        genesis_key_delegation = (5, genesishash, genesis_delegate_hash, vrf_keyhash)
        move_instantaneous_rewards_cert = (6, move_instantaneous_reward)*/
        //Certificates are byte[] but we may need to denote type...
        //add a new Certificate

        if (certificate == null)
        {
            throw new ArgumentNullException(nameof(certificate));
        }
        else if (certificate.StakeRegistration == null && certificate.StakeDeregistration == null && certificate.StakeDelegation == null)
        {
            throw new ArgumentException("certificate must have a StakeRegistration, StakeDeregistration, or StakeDelegation");
        }

        CBORObject cborObject = null!;
        if (certificate.StakeRegistration != null)
        {
            cborObject = CBORObject
                .NewArray()
                .Add(0)
                .Add(CBORObject.NewArray().Add(certificate.StakeRegistration.StakeCredentialType).Add(certificate.StakeRegistration.StakeCredential));
        }
        else if (certificate.StakeDeregistration != null)
        {
            cborObject = CBORObject
                .NewArray()
                .Add(1)
                .Add(
                    CBORObject
                        .NewArray()
                        .Add(certificate.StakeDeregistration.StakeCredentialType)
                        .Add(certificate.StakeDeregistration.StakeCredential)
                );
        }
        else if (certificate.StakeDelegation != null)
        {
            cborObject = certificate.StakeDelegation.GetCBOR();
        }

        //if (certificate.PoolRegistration != null)
        //{

        //}

        //if (certificate.PoolRetirement != null)
        //{

        //}

        //if (certificate.GenesisKeyDelegation != null)
        //{

        //}

        //if (certificate.GenesisKeyDelegation != null)
        //{

        //}

        return cborObject;
    }

    public static Certificate GetCertificate(this CBORObject certificateCbor)
    {
        //validation
        if (certificateCbor == null)
        {
            throw new ArgumentNullException(nameof(certificateCbor));
        }
        if (certificateCbor.Type != CBORType.Array)
        {
            throw new ArgumentException("certificateCbor is expected type CBORType.Array");
        }

        if (!certificateCbor.Values.First().IsNumber)
        {
            throw new ArgumentException("certificateCbor array item has invalid first element (expected number)");
        }

        var certificate = new Certificate();
        var index = certificateCbor.Values.First().DecodeValueToInt32();
        switch (index)
        {
            case 0: //stake registration
                var regCertIndex = certificateCbor[1][0].DecodeValueToInt32();
                if (regCertIndex != 0 && regCertIndex != 1)
                {
                    throw new NotImplementedException("stake_registration accompanying cbor map index has unexpected value (expected 0)");
                }
                var regCert = ((string)certificateCbor[1][1].DecodeValueByCborType()).HexToByteArray();

                certificate.StakeRegistration = new StakeRegistration { StakeCredentialType = regCertIndex, StakeCredential = regCert };
                break;
            case 1: //stake deregistration
                var deregCertIndex = certificateCbor[1][0].DecodeValueToInt32();
                if (deregCertIndex != 0 && deregCertIndex != 1)
                {
                    throw new NotImplementedException("stake_deregistration accompanying cbor map index has unexpected value (expected 0)");
                }
                var deregCert = ((string)certificateCbor[1][1].DecodeValueByCborType()).HexToByteArray();
                certificate.StakeDeregistration = new StakeDeregistration { StakeCredentialType = deregCertIndex, StakeCredential = deregCert };
                break;
            case 2: //stake delegation
                var delegationCertIndex = certificateCbor[1][0].DecodeValueToInt32();
                if (delegationCertIndex != 0)
                {
                    throw new NotImplementedException("stake_delegation accompanying cbor map index has unexpected value (expected 0)");
                }
                certificate.StakeDelegation = certificateCbor.GetStakeDelegation();
                break;
            default:
                throw new ArgumentException("certificateCbor array item had unexpected index value (expected 0 or 1)");
        }

        return certificate;
    }

    public static byte[] Serialize(this Certificate certificate)
    {
        return certificate.GetCBOR().EncodeToBytes();
    }

    public static Certificate DeserializeCertificate(this byte[] bytes)
    {
        return CBORObject.DecodeFromBytes(bytes).GetCertificate();
    }
}
