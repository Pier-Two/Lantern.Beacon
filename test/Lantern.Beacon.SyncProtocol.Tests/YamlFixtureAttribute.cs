using NUnit.Framework;

namespace Lantern.Beacon.SyncProtocol.Tests;

public class YamlFixtureAttribute(string filePath) : NUnitAttribute
{
    public string FilePath { get; private set; } = filePath;
}