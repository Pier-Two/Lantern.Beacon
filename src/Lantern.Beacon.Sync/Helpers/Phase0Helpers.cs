using System.Security.Cryptography;
using Nethereum.Hex.HexConvertors.Extensions;
using SszSharp;
using Cortex.Containers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types.Phase0;

namespace Lantern.Beacon.Sync.Helpers;

public static class Phase0Helpers
{
    public static bool IsValidMerkleBranch(byte[] leaf, byte[][] branch, int depth, int index, byte[] root)
    {
        var value = leaf;

        for (var i = 0; i < depth; i++)
        {
            var currentIndex = (index >> i) & 1;
            
            if (currentIndex == 1)
            {
                value = SHA256.HashData(branch[i].AsSpan().ToArray().Concat(value).ToArray());                
            }
            else
            {
                value = SHA256.HashData(value.Concat(branch[i].AsSpan().ToArray()).ToArray());
            }
        }

        return value.SequenceEqual(root);
    }
    
    public static bool IsBranchUpdate(byte[][] branch)
    {
        foreach (var item in branch)
        {
            if (item is not { Length: Bytes32.Length })
            {
                return false;
            }
            if (item.Any(byteValue => byteValue != 0))
            {
                return true;
            }
        }
        return false;
    }

    public static bool AreBranchesEqual(byte[][] branch1, byte[][] branch2)
    {
        if (branch1.Length != branch2.Length)
        {
            return false;
        }

        for (var i = 0; i < branch1.Length; i++)
        {
            if (!branch1[i].SequenceEqual(branch2[i]))
            {
                return false;
            }
        }

        return true;
    }
    
    public static ulong ComputeEpochAtSlot(ulong slot)
    {
        return slot / (ulong)Phase0Preset.SlotsPerEpoch;
    }
    
    public static uint ComputeForkVersion(ulong epoch)
    {
        if (epoch >= Config.Config.CapellaForkEpoch)
        {
            return Config.Config.CapellaForkVersion;
        }

        if (epoch >= Config.Config.BellatrixForkEpoch)
        {
            return Config.Config.BellatrixForkVersion;
        }

        if (epoch >= Config.Config.AltairForkEpoch)
        {
            return Config.Config.AltairForkVersion;
        }       
        
        return Config.Config.GenesisForkVersion;
    }
    
    public static byte[] ComputeDomain(uint domainType, uint? forkVersion, byte[]? genesisValidatorsRoot, SizePreset preset)
    {
        if(!forkVersion.HasValue)
        {
            forkVersion = Config.Config.GenesisForkVersion;
        }
        
        if(genesisValidatorsRoot == null)
        {
            genesisValidatorsRoot = new byte[Constants.RootLength];
        }
        
        var forkDataRoot = ComputeForkDataRoot(forkVersion.Value, genesisValidatorsRoot, preset).Take(28);
        var domainTypeBytes = BitConverter.GetBytes(domainType);
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(domainTypeBytes);
        }
        
        return domainTypeBytes.Concat(forkDataRoot).ToArray();
    }
    
    public static byte[] ComputeForkDataRoot(uint currentVersion, byte[] genesisValidatorsRoot, SizePreset preset)
    {
        var currentVersionBytes = BitConverter.GetBytes(currentVersion);
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(currentVersionBytes);
        }
        
        var forkData = ForkData.CreateFrom(currentVersionBytes, genesisValidatorsRoot);
        return forkData.GetHashTreeRoot(preset);
    }

    public static byte[] ComputeForkDigest(uint currentVersion, byte[] genesisValidatorsRoot, SizePreset preset)
    {
        return ComputeForkDataRoot(currentVersion, genesisValidatorsRoot, preset).Take(4).ToArray();
    }

    public static byte[] ComputeSigningRoot<T>(T sszObject, byte[] domain, SizePreset sizePreset)
    {
        var sszContainer = SszContainer.GetContainer<T>(sizePreset);
        var signingData = SigningData.CreateFrom(sszContainer.HashTreeRoot(sszObject), domain);
        return signingData.GetHashTreeRoot(sizePreset);
    }
}