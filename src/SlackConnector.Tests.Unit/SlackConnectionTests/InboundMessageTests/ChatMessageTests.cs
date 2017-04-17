﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Should;
using SlackConnector.BotHelpers;
using SlackConnector.Connections.Models;
using SlackConnector.Connections.Sockets;
using SlackConnector.Connections.Sockets.Messages.Inbound;
using SlackConnector.Models;
using SlackConnector.Tests.Unit.Stubs;
using SpecsFor.ShouldExtensions;

namespace SlackConnector.Tests.Unit.SlackConnectionTests.InboundMessageTests
{
    internal class ChatMessageTests
    {
        [Test, AutoMoqData]
        public async Task should_raise_event(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                Users = { { "userABC", new SlackUser { Id = "userABC", Name = "i-have-a-name" } } },
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                User = "userABC",
                MessageType = MessageType.Message,
                Text = "amazing-text",
                RawData = "I am raw data yo"
            };

            SlackMessage receivedMessage = null;
            slackConnection.OnMessageReceived += message =>
            {
                receivedMessage = message;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            receivedMessage.ShouldLookLike(new SlackMessage
            {
                Text = "amazing-text",
                User = new SlackUser { Id = "userABC", Name = "i-have-a-name" },
                RawData = inboundMessage.RawData
            });
        }

        [Test, AutoMoqData]
        public async Task should_raise_event_given_user_information_is_missing_from_cache(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                User = "userABC",
                MessageType = MessageType.Message
            };

            SlackMessage receivedMessage = null;
            slackConnection.OnMessageReceived += message =>
            {
                receivedMessage = message;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            receivedMessage.ShouldLookLike(new SlackMessage
            {
                User = new SlackUser { Id = "userABC", Name = string.Empty }
            });
        }
    }
    
    internal class given_connector_is_setup_when_inbound_message_arrives_that_isnt_message_type : ChatMessageTest
    {
        protected override void Given()
        {
            InboundMessage = new ChatMessage
            {
                MessageType = MessageType.Unknown
            };

            base.Given();
        }

        [Test]
        public void then_should_not_call_callback()
        {
            MessageRaised.ShouldBeFalse();
        }
    }

    internal class given_null_message_when_inbound_message_arrives : ChatMessageTest
    {
        protected override void Given()
        {
            InboundMessage = null;

            base.Given();
        }

        [Test]
        public void then_should_not_call_callback()
        {
            MessageRaised.ShouldBeFalse();
        }
    }

    internal class given_channel_already_defined_when_inbound_message_arrives_with_channel : ChatMessageTest
    {
        protected override void Given()
        {
            base.Given();

            ConnectionInfo.SlackChatHubs.Add("channelId", new SlackChatHub { Id = "channelId", Name = "NaMe23" });

            InboundMessage = new ChatMessage
            {
                Channel = ConnectionInfo.SlackChatHubs.First().Key,
                MessageType = MessageType.Message,
                User = "irmBrady"
            };
        }

        [Test]
        public void then_should_return_expected_channel_information()
        {
            SlackChatHub expected = ConnectionInfo.SlackChatHubs.First().Value;
            Result.ChatHub.ShouldEqual(expected);
        }
    }


    internal class given_bot_was_mentioned_in_text : ChatMessageTest
    {
        protected override void Given()
        {
            base.Given();

            ConnectionInfo.Self = new ContactDetails { Id = "self-id", Name = "self-name" };

            InboundMessage = new ChatMessage
            {
                Channel = "idy",
                MessageType = MessageType.Message,
                Text = "please send help... :-p",
                User = "lalala"
            };

            GetMockFor<IMentionDetector>()
                .Setup(x => x.WasBotMentioned(ConnectionInfo.Self.Name, ConnectionInfo.Self.Id, InboundMessage.Text))
                .Returns(true);
        }

        [Test]
        public void then_should_return_expected_channel_information()
        {
            Result.MentionsBot.ShouldBeTrue();
        }
    }

    internal class given_message_is_from_self : ChatMessageTest
    {
        protected override void Given()
        {
            base.Given();

            ConnectionInfo.Self = new ContactDetails { Id = "self-id", Name = "self-name" };

            InboundMessage = new ChatMessage
            {
                MessageType = MessageType.Message,
                User = ConnectionInfo.Self.Id
            };
        }

        [Test]
        public void then_should_not_raise_message()
        {
            MessageRaised.ShouldBeFalse();
        }
    }

    internal class given_message_is_missing_user_information : ChatMessageTest
    {
        protected override void Given()
        {
            base.Given();

            InboundMessage = new ChatMessage
            {
                MessageType = MessageType.Message,
                User = null
            };
        }

        [Test]
        public void then_should_not_raise_message()
        {
            MessageRaised.ShouldBeFalse();
        }
    }

    [TestFixture]
    internal class given_exception_thrown_when_handling_inbound_message : ChatMessageTest
    {
        private WebSocketClientStub WebSocket { get; set; }

        protected override void Given()
        {
            base.Given();

            WebSocket = new WebSocketClientStub();
            ConnectionInfo.WebSocket = WebSocket;

            SUT.OnMessageReceived += message =>
            {
                throw new NotImplementedException();
            };
        }

        [Test]
        public void should_not_throw_exception_when_error_is_thrown()
        {
            var message = new ChatMessage
            {
                User = "something",
                MessageType = MessageType.Message
            };

            WebSocket.RaiseOnMessage(message);
        }
    }

    internal abstract class ChatMessageTest : BaseTest<ChatMessage>
    {
        protected override void When()
        {
            SUT.Initialise(ConnectionInfo).Wait();

            if (!string.IsNullOrEmpty(InboundMessage?.Channel))
            {
                GetMockFor<IWebSocketClient>()
                    .Raise(x => x.OnMessage += null, null, new ChannelJoinedMessage { Channel = new Channel { Id = InboundMessage.Channel } });
            }

            GetMockFor<IWebSocketClient>()
                .Raise(x => x.OnMessage += null, null, InboundMessage);
        }
    }
}
