namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using Microsoft.ServiceBus.Messaging;

    public static class BrokeredMessageConverter
    {
        public static TransportMessage ToTransportMessage(BrokeredMessage message)
        {
            TransportMessage t;
            var rawMessage = message.GetBody<byte[]>();

            if (message.Properties.Count == 0)
            {
                t = DeserializeMessage(rawMessage);
            }
            else
            {
                t = new TransportMessage(message.MessageId,
                    message.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as string))
                {
                    CorrelationId = message.CorrelationId,
                    TimeToBeReceived = message.TimeToLive,
                    MessageIntent = (MessageIntentEnum)
                        Enum.Parse(typeof(MessageIntentEnum), message.Properties[Headers.MessageIntent].ToString())
                };

                if ( !String.IsNullOrWhiteSpace( message.ReplyTo ) )
                {
                    t.ReplyToAddress = Address.Parse( message.ReplyTo ); // Will this work?
                }

                t.Body = rawMessage;
            }

            return t;
        }

        private static TransportMessage DeserializeMessage(byte[] rawMessage)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream(rawMessage))
            {
                var message = formatter.Deserialize(stream) as TransportMessage;

                if (message == null)
                {
                    throw new SerializationException("Failed to deserialize message");
                }

                return message;
            }
        }
    }
}