// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

namespace Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub;

public interface ITopic
{
    event Action<byte[]>? OnMessage;
    void Publish(byte[] bytes);
}
