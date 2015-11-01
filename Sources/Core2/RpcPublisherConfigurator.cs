﻿using MessageBus.Core.API;

namespace MessageBus.Core
{
    public class RpcPublisherConfigurator : PublisherConfigurator, IRpcPublisherConfigurator
    {
        private bool _useFastReply;
        private string _replyExchange;

        public RpcPublisherConfigurator(string exchange, bool useFastReply, string replyExchange, IPublishingErrorHandler errorHandler) : base(exchange, errorHandler)
        {
            _useFastReply = useFastReply;
            _replyExchange = replyExchange;
        }

        public bool UseFastReply
        {
            get { return _useFastReply; }
        }

        public string ReplyExchange
        {
            get { return _replyExchange; }
        }

        public IRpcPublisherConfigurator DisableFastReply()
        {
            _useFastReply = false;

            return this;
        }


        public IRpcPublisherConfigurator SetReplyExchange(string replyExchange)
        {
            _replyExchange = replyExchange;

            return this;
        }
    }
}