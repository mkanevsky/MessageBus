﻿using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using ZMQ;

namespace MessageBus.Binding.ZeroMQ
{
    public abstract class ZMQChannelBase : ChannelBase
    {
        protected readonly MessageEncoder _encoder;
        private readonly BindingContext _context;
        protected readonly Socket _socket;
        protected readonly BufferManager _bufferManager;

        private readonly Action<Message> _onSendHandler;
        private readonly SocketMode _socketMode;

        protected ZMQChannelBase(ChannelManagerBase channelManager, BindingContext context, Socket socket, SocketMode socketMode)
            : base(channelManager)
        {
            MessageEncodingBindingElement encoderElem = context.BindingParameters.Find<MessageEncodingBindingElement>();
            
            _encoder = encoderElem.CreateMessageEncoderFactory().Encoder;
            
            _context = context;
            _socket = socket;
            _socketMode = socketMode;

            _onSendHandler = Send;

            _bufferManager = context.BindingParameters.Find<BufferManager>();
        }

        protected BindingContext Context
        {
            get { return _context; }
        }

        protected Socket Socket
        {
            get { return _socket; }
        }
        
        protected SocketMode SocketMode
        {
            get { return _socketMode; }
        }

        protected Message ConstructMessage(byte[] buffer)
        {
            Message message = _encoder.ReadMessage(new MemoryStream(buffer), 8192);
            
            return message;
        }
        
        protected Message ConstructMessage(ArraySegment<byte> buffer, BufferManager bufferManager)
        {
            Message message = _encoder.ReadMessage(buffer, bufferManager);
            
            return message;
        }

        #region Send

        public void Send(Message message, TimeSpan timeout)
        {
            Send(message);
        }

        public void Send(Message message)
        {
            if (_bufferManager == null)
            {
                byte[] body;
#if VERBOSE
                DebugHelper.Start();
#endif
                using (MemoryStream str = new MemoryStream())
                {
                    _encoder.WriteMessage(message, str);
                    body = str.ToArray();
                }
#if VERBOSE
                DebugHelper.Stop(" #### Message.Serialize {{\n\tAction={2}, \n\tBytes={1}, \n\tTime={0}ms}}.",
                    body.Length,
                    message.Headers.Action);
#endif
                byte[] lengthBytes = BitConverter.GetBytes(body.Length);
#if VERBOSE
                DebugHelper.Start();
#endif
                _socket.SendMore(lengthBytes);
                _socket.Send(body);
#if VERBOSE
                DebugHelper.Stop(" #### Message.Publish {{\n\tAction={2}, \n\tBytes={1}, \n\tTime={0}ms}}.",
                    body.Length,
                    message.Headers.Action);
#endif
            }
            else
            {
#if VERBOSE
                DebugHelper.Start();
#endif
                ArraySegment<byte> body = _encoder.WriteMessage(message, 100 * 1024 * 1024, _bufferManager);
#if VERBOSE
                DebugHelper.Stop(" #### Message.Serialize {{\n\tAction={2}, \n\tBytes={1}, \n\tTime={0}ms}}.",
                    body.Count,
                    message.Headers.Action);
#endif
                try
                {
                    byte[] lengthBytes = BitConverter.GetBytes(body.Count);
#if VERBOSE
                    DebugHelper.Start();
#endif
                    _socket.SendMore(lengthBytes);
                    _socket.Send(body.Array, body.Offset, body.Count);
#if VERBOSE
                    DebugHelper.Stop(" #### Message.Publish {{\n\tAction={2}, \n\tBytes={1}, \n\tTime={0}ms}}.",
                        body.Count,
                        message.Headers.Action);
#endif
                }
                finally
                {
                    _bufferManager.ReturnBuffer(body.Array);
                }
            }
        }

        #endregion
        
        #region Async Send

        public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _onSendHandler.BeginInvoke(message, callback, state);
        }

        public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
        {
            return _onSendHandler.BeginInvoke(message, callback, state);
        }

        public void EndSend(IAsyncResult result)
        {
            _onSendHandler.EndInvoke(result);
        }

        #endregion

        #region Abort \ Close

        protected override void OnAbort()
        {
            OnClose(TimeSpan.Zero);
        }
        
        #endregion

        #region Asyn Open\Close

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return Task.Factory.StartNew(s => OnClose(timeout), state).ContinueWith(task => callback(task));
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            Task.Factory.FromAsync(result, asyncResult => { }).Wait();
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return Task.Factory.StartNew(s => OnOpen(timeout), state).ContinueWith(task => callback(task));
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            Task.Factory.FromAsync(result, asyncResult => { }).Wait();
        }

        #endregion
    }
}