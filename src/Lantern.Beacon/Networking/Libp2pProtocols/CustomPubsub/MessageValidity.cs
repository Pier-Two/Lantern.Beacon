// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

namespace Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub;
public enum MessageValidity
{
    Accepted,
    Ignored,
    Rejected,
    Trottled
}
