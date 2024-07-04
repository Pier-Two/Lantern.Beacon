using System.Security.Cryptography;
using SszSharp;
using Cortex.Containers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Basic;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;

namespace Lantern.Beacon.Sync.Helpers;

public static class Phase0Helpers
{
    public static DateTime SlotToDateTime(ulong slot, ulong genesisTime)
    {
        var time = DateTimeOffset.FromUnixTimeSeconds((long)genesisTime);
        var secondsPerSlot = Config.Config.SecondsPerSlot; 
        var secondsSinceGenesis = slot * (ulong)secondsPerSlot;
        var slotTime = time.AddSeconds(secondsSinceGenesis);

        return slotTime.DateTime;
    }
    
    public static bool HasSufficientPropagationTimeElapsed(DateTime signatureSlotStartTime)
    {
        var elapsedTime = DateTime.UtcNow - signatureSlotStartTime;
        var requiredTime = (double)Config.Config.SecondsPerSlot / Phase0Preset.SlotsPerEpoch + Constants.MaximumGossipClockDisparity / 1000.0;
        return elapsedTime.TotalSeconds >= requiredTime;
    }
    
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
    
    public static ForkType ComputeForkType(byte[] forkDigest, SyncProtocolOptions options)
    {
        var denebForkDigest = ComputeForkDigest(ConvertToLittleEndian(Config.Config.DenebForkVersion), options);
        var capellaForkDigest = ComputeForkDigest(ConvertToLittleEndian(Config.Config.CapellaForkVersion), options);
        var bellatrixForkDigest = ComputeForkDigest(ConvertToLittleEndian(Config.Config.BellatrixForkVersion), options);
        var altairForkDigest = ComputeForkDigest(ConvertToLittleEndian(Config.Config.AltairForkVersion), options);
        
        if (denebForkDigest.SequenceEqual(forkDigest))
        {
            return ForkType.Deneb;
        }
        
        if (capellaForkDigest.SequenceEqual(forkDigest))
        {
            return ForkType.Capella;
        }
        
        if (bellatrixForkDigest.SequenceEqual(forkDigest))
        {
            return ForkType.Bellatrix;
        }
        
        if (altairForkDigest.SequenceEqual(forkDigest))
        {
            return ForkType.Altair;
        }
        
        return ForkType.Phase0;
    }
    
    public static byte[] ComputeForkVersion(ulong epoch)
    {
        if (epoch >= Config.Config.DenebForkEpoch)
        {
            return ConvertToLittleEndian(Config.Config.DenebForkVersion);
        }
        
        if (epoch >= Config.Config.CapellaForkEpoch)
        {
            return ConvertToLittleEndian(Config.Config.CapellaForkVersion);
        }

        if (epoch >= Config.Config.BellatrixForkEpoch)
        {
            return ConvertToLittleEndian(Config.Config.BellatrixForkVersion);
        }

        if (epoch >= Config.Config.AltairForkEpoch)
        {
            return ConvertToLittleEndian(Config.Config.AltairForkVersion);
        }       
        
        return ConvertToLittleEndian(Config.Config.GenesisForkVersion);
    }

    public static ulong ComputeCurrentSlot(ulong genesisTime)
    {
        var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var secondsPerSlot = (ulong)Config.Config.SecondsPerSlot;
        
        return ((currentTime - genesisTime) / secondsPerSlot);
    }
    
    public static byte[] ComputeDomain(uint domainType, byte[]? forkVersion, SyncProtocolOptions options)
    {
        if(forkVersion == null)
        {
            forkVersion = ComputeForkVersion(ComputeEpochAtSlot(ComputeCurrentSlot(options.GenesisTime)));
        }
        
        if(options.GenesisValidatorsRoot == null)
        {
            options.GenesisValidatorsRoot = new byte[Constants.RootLength];
        }
        
        var forkDataRoot = ComputeForkDataRoot(forkVersion, options).Take(28);
        
        return ConvertToLittleEndian(domainType).Concat(forkDataRoot).ToArray();
    }

    public static byte[] ComputeForkDigest(byte[] currentVersion, SyncProtocolOptions options)
    {
        return ComputeForkDataRoot(currentVersion, options).Take(4).ToArray();
    }
    
    public static byte[] ComputeForkDataRoot(byte[] currentVersion, SyncProtocolOptions options)
    {
        var forkData = ForkData.CreateFrom(currentVersion, options.GenesisValidatorsRoot);
        return forkData.GetHashTreeRoot(options.Preset);
    }

    public static byte[] ComputeSigningRoot<T>(T sszObject, byte[] domain, SizePreset sizePreset)
    {
        var sszContainer = SszContainer.GetContainer<T>(sizePreset);
        var signingData = SigningData.CreateFrom(sszContainer.HashTreeRoot(sszObject), domain);
        return signingData.GetHashTreeRoot(sizePreset);
    }
    
    private static byte[] ConvertToLittleEndian(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        
        return bytes;
    }
}