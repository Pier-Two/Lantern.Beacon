// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub;

public interface ITopic
{
    event Action<PeerId, byte[]>? OnMessage;
    void Publish(byte[] bytes);
}
