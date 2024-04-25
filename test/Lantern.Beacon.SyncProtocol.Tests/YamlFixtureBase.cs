using YamlDotNet;
using YamlDotNet.Serialization;

namespace Lantern.Beacon.SyncProtocol.Tests;

public class YamlFixtureBase
{
    protected dynamic _yamlData;

    protected void LoadYamlFixture(YamlFixtureAttribute yamlFixture)
    {
        if (yamlFixture == null) 
            return;
        
        var filePath = yamlFixture.FilePath;
        
        _yamlData = LoadYamlFile(filePath);
    }

    private dynamic LoadYamlFile(string filePath)
    {
        var yaml = new Deserializer();
        using var streamReader = new StreamReader(filePath);
        return yaml.Deserialize<dynamic>(streamReader);
    }
}