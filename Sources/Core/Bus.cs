﻿using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using MessageBus.Binding.RabbitMQ;
using MessageBus.Core.API;

namespace MessageBus.Core
{
    public class Bus : IBus
    {
        private readonly System.ServiceModel.Channels.Binding _binding = new RabbitMQBinding
            {
                ApplicationId = Guid.NewGuid().ToString(),
                IgnoreSelfPublished = true,
                AutoBindExchange = "amq.fanout",
                OneWayOnly = true,
                ExactlyOnce = false,
                PersistentDelivery = false
            };

        private readonly IChannelFactory<IOutputChannel> _channelFactory;
        private readonly IChannelListener<IInputChannel> _listener;
        
        public Bus()
        {
            _channelFactory = _binding.BuildChannelFactory<IOutputChannel>();

            _channelFactory.Open();

            _listener = _binding.BuildChannelListener<IInputChannel>(new Uri("amqp://localhost/"));

            _listener.Open();
        }

        public void Dispose()
        {
            _channelFactory.Close();
            _listener.Close();
        }

        public IPublisher CreatePublisher()
        {
            IOutputChannel outputChannel = _channelFactory.CreateChannel(new EndpointAddress("amqp://localhost/amq.fanout"));

            return new Publisher(outputChannel, _binding.MessageVersion);
        }

        public ISubscriber CreateSubscriber()
        {
            IInputChannel inputChannel = _listener.AcceptChannel();

            return new Subscriber(inputChannel);
        }
    }
}
