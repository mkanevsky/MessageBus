﻿using System;
using System.Linq;
using System.ServiceModel.Channels;
using MessageBus.Core.API;

namespace MessageBus.Core
{
    public abstract class Bus : IBus
    {
        private readonly string _busId;

        protected Bus(string busId)
        {
            _busId = busId ?? Guid.NewGuid().ToString();
        }

        public string BusId
        {
            get { return _busId; }
        }

        public IPublisher CreatePublisher(Action<IPublisherConfigurator> configure = null)
        {
            PublisherConfigurator configurator = new PublisherConfigurator();

            if (configure != null)
            {
                configure(configurator);
            }

            return OnCreatePublisher(configurator);
        }

        public ISubscriber CreateSubscriber(Action<ISubscriberConfigurator> configure = null)
        {
            var configurator = CreateConfigurator(configure);

            IInputChannel inputChannel = OnCreateInputChannel(configurator);
            
            IMessageFilter messageFilter = OnCreateMessageFilter(inputChannel);

            ICallbackDispatcher dispatcher = new CallBackDispatcher(configurator.ErrorSubscriber, BusId);

            return new Subscriber(inputChannel, messageFilter, dispatcher);
        }


        public ISubscribtion RegisterSubscribtion<T>(T instance, Action<ISubscriberConfigurator> configure = null)
        {
            var configurator = CreateConfigurator(configure);

            IInputChannel inputChannel = OnCreateInputChannel(configurator);

            IMessageFilter messageFilter = OnCreateMessageFilter(inputChannel);

            ISubscriptionDispatcher dispatcher = new SubscriptionDispatcher(configurator.ErrorSubscriber, BusId);

            return new Subscribtion(inputChannel, messageFilter, dispatcher, instance);
        }

        private static SubscriberConfigurator CreateConfigurator(Action<ISubscriberConfigurator> configure)
        {
            SubscriberConfigurator configurator = new SubscriberConfigurator();

            if (configure != null)
            {
                configure(configurator);
            }

            return configurator;
        }
        
        internal abstract IPublisher OnCreatePublisher(PublisherConfigurator configuration);

        internal abstract IInputChannel OnCreateInputChannel(SubscriberConfigurator configurator);

        protected object[] CreateParameters(params object[] parameters)
        {
            return parameters.Where(o => o != null).ToArray();
        }

        public abstract void Dispose();

        internal abstract IMessageFilter OnCreateMessageFilter(IInputChannel channel);
    }
}
