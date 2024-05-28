using IronSnappy;
using Nethereum.Hex.HexConvertors.Extensions;
using YamlDotNet;
using YamlDotNet.Serialization;

namespace Lantern.Beacon.Sync.Tests;

public class FileFixtureBase
{
    protected dynamic? _yamlData;
    protected byte[]? _sszData;

    protected void LoadYamlFixture(FileFixtureAttribute fileFixture)
    {
        if (fileFixture == null) 
            return;
        
        var filePath = fileFixture.FilePath;
        
        _yamlData = LoadYamlFile(filePath);
    }
    
    protected void LoadSszFixture(FileFixtureAttribute sszFixture)
    {
        if (sszFixture == null) 
            return;
        
        var filePath = sszFixture.FilePath;
        
        _sszData = LoadSszFile(filePath);
    }

    private static dynamic LoadYamlFile(string filePath)
    {
        var yaml = new Deserializer();
        using var streamReader = new StreamReader(filePath);
        return yaml.Deserialize<dynamic>(streamReader);
    }
    
    private static byte[] LoadSszFile(string filePath)
    {
        return Snappy.Decode(File.ReadAllBytes(filePath));
    }
}