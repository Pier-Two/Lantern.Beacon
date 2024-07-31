<div align="center">
  <h1 align="center">Lantern.Beacon</h1>
</div>

Lantern.Beacon is a .NET library that allows lightweight verification of Ethereum's consensus, built using [Libp2p](https://github.com/NethermindEth/dotnet-libp2p) and [Discv5](https://github.com/Pier-Two/Lantern.Discv5).

## Installation

*Note: These instructions assume you are familiar with the .NET Core development environment. If not, please refer to the [official documentation](https://docs.microsoft.com/en-us/dotnet/core/introduction) to get started.*

1. Install [.NET Core SDK](https://docs.microsoft.com/en-us/dotnet/core/install/) on your system if you haven't already.

2. Clone the repository:

   ```bash
   git clone https://github.com/Pier-Two/Lantern.Beacon.git --recursive
   ```

3. Change to the `Lantern.Beacon` directory:

   ```bash
   cd Lantern.Beacon
   ```

4. Build the project:

   ```bash
   dotnet build
   ```

5. **If you are using Linux, run the following commands before executing the tests:**

   For **Debian/Ubuntu-based** distributions:
   ```bash
   sudo apt-get update && sudo apt-get install -y libc6-dev

   sudo ln -s /lib/x86_64-linux-gnu/libdl.so.2 /lib/x86_64-linux-gnu/libdl.so
   ```

   For **Red Hat-based** distributions (such as Fedora or CentOS):
   ```bash
   sudo yum install glibc-devel

   sudo ln -s /usr/lib64/libdl.so.2 /usr/lib64/libdl.so
   ```

   For **Arch Linux**:
   ```bash
   sudo pacman -Sy glibc

   sudo ln -s /usr/lib/libdl.so.2 /usr/lib/libdl.so
   ```
   
6. Execute tests:
   ```bash
   dotnet test
   ```

## Usage
To integrate this library in a C# project, please refer to the [GitBook](https://piertwo.gitbook.io/lantern.beacon/) documentation. 

## Contributing
We welcome contributions to this library. To get involved, please read our [Contributing Guidelines](https://piertwo.gitbook.io/lantern.beacon/contribution-guidelines) for the process for submitting pull requests to this repository.

## License
This project is licensed under the [MIT License](https://github.com/Pier-Two/Lantern.Beacon/blob/main/LICENSE).
