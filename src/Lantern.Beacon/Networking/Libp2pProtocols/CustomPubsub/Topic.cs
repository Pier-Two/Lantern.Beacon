// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

namespace Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub;

class Topic : ITopic
{
    private readonly CustomPubsubRouter router;
    private readonly string topicName;

    public Topic(CustomPubsubRouter router, string topicName)
    {
        this.router = router;
        this.topicName = topicName;
        router.OnMessage += (topicName, message) =>
        {
            if (OnMessage is not null && this.topicName == topicName)
            {
                OnMessage(message);
            }
        };
    }

    public DateTime LastPublished { get; set; }

    public event Action<byte[]>? OnMessage;

    public void Publish(byte[] value)
    {
        router.Publish(topicName, value);
    }
}
