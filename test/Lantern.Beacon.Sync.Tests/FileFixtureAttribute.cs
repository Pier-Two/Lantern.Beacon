using NUnit.Framework;

namespace Lantern.Beacon.Sync.Tests;

public class FileFixtureAttribute(string filePath) : NUnitAttribute
{
    public string FilePath { get; private set; } = filePath;
}