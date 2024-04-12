<div align="center">
  <h1 align="center">Lantern.Beacon</h1>
</div>

Lantern.Beacon is a .NET library that allows connecting with Ethereum's consensus layer nodes using [Libp2p](https://github.com/NethermindEth/dotnet-libp2p) and [Discv5](https://github.com/Pier-Two/Lantern.Discv5) for running a [syncing protocol](https://github.com/ethereum/consensus-specs/blob/dev/specs/altair/light-client/sync-protocol.md).

## Installation

*Note: These instructions assume you are familiar with the .NET Core development environment. If not, please refer to the [official documentation](https://docs.microsoft.com/en-us/dotnet/core/introduction) to get started.*

1. Install [.NET Core SDK](https://docs.microsoft.com/en-us/dotnet/core/install/) on your system if you haven't already.

2. Clone the repository:

   ```bash
   git clone https://github.com/Pier-Two/Lantern.Beacon.git
   ```

3. Change to the `Lantern.Beacon` directory:

   ```bash
   cd Lantern.Discv5
   ```

4. Build the project:

   ```bash
   dotnet build
   ```

5. Execute tests:
   ```bash
   dotnet test
   ```

## License
This project is licensed under the [MIT License](https://github.com/Pier-Two/Lantern.Beacon/blob/main/LICENSE).