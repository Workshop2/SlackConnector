﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Ploeh.AutoFixture;
using SlackConnector.Connections.Sockets;
using SlackConnector.Connections.Sockets.Messages.Inbound;
using SlackConnector.Models;
using SlackConnector.Tests.Unit.TestExtensions;
using Xunit;
using SlackConnector.Connections.Sockets.Messages.Inbound.ReactionItem;

namespace SlackConnector.Tests.Unit.SlackConnectionTests.InboundMessageTests
{
    public class ReactionTests
    {
        [Theory, AutoMoqData]
        private async Task should_raise_message_reaction_event(
            Mock<IWebSocketClient> webSocket,
            SlackConnection slackConnection)
        {
            // given
            var fixture = new Fixture();
            var connectionInfo = new ConnectionInformation
            {
                Users =
                {
                    { "userABC", new SlackUser { Id = "userABC", Name = "i-have-a-name" } },
                    { "secondUser", new SlackUser { Id = "secondUser", Name = "i-have-a-name-too-thanks" } }
                },
                SlackChatHubs = new Dictionary<string, SlackChatHub>
                {
                    { "chat-hub-1", new SlackChatHub() }
                },
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            ISlackReaction lastReaction = null;
            slackConnection.OnReaction += reaction =>
            {
                lastReaction = reaction;
                return Task.CompletedTask;
            };

            var inboundMessage = new ReactionMessage
            {
                User = "userABC",
                Reaction = fixture.Create<string>(),
                RawData = fixture.Create<string>(),
                ReactingToUser = "secondUser",
                Timestamp = fixture.Create<double>(),
                ReactingTo = new MessageReaction
                {
                    Channel = "chat-hub-1"
                }
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            lastReaction.ShouldLookLike(new SlackMessageReaction
            {
                User = connectionInfo.Users["userABC"],
                Reaction = inboundMessage.Reaction,
                RawData = inboundMessage.RawData,
                ChatHub = connectionInfo.SlackChatHubs["chat-hub-1"],
                ReactingToUser = connectionInfo.Users["secondUser"],
                Timestamp = inboundMessage.Timestamp
            });
        }

        [Theory, AutoMoqData]
        private async Task should_raise_file_reaction_event(
            Mock<IWebSocketClient> webSocket,
            SlackConnection slackConnection)
        {
            // given
            var fixture = new Fixture();
            var connectionInfo = new ConnectionInformation
            {
                Users =
                {
                    { "some-user", new SlackUser { Id = "some-user", Name = "i-have-a-name" } },
                    { "another-user", new SlackUser { Id = "another-user", Name = "i-have-a-name-too-thanks" } }
                },
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            ISlackReaction lastReaction = null;
            slackConnection.OnReaction += reaction =>
            {
                lastReaction = reaction;
                return Task.CompletedTask;
            };

            var file = fixture.Create<string>();
            var inboundMessage = new ReactionMessage
            {
                User = "some-user",
                Reaction = fixture.Create<string>(),
                RawData = fixture.Create<string>(),
                ReactingToUser = "another-user",
                Timestamp = fixture.Create<double>(),
                ReactingTo = new FileReaction
                {
                    File = file
                }
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            lastReaction.ShouldLookLike(new SlackFileReaction
            {
                User = connectionInfo.Users["some-user"],
                Reaction = inboundMessage.Reaction,
                File = file,
                RawData = inboundMessage.RawData,
                ReactingToUser = connectionInfo.Users["another-user"],
                Timestamp = inboundMessage.Timestamp
            });
        }

        //[Theory, AutoMoqData]
        //private async Task should_not_raise_event_given_missing_user_info(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        //{
        //    // given
        //    var connectionInfo = new ConnectionInformation { WebSocket = webSocket.Object };
        //    await slackConnection.Initialise(connectionInfo);

        //    SlackUser lastUser = null;
        //    slackConnection.OnUserJoined += user =>
        //    {
        //        lastUser = user;
        //        return Task.CompletedTask;
        //    };

        //    var inboundMessage = new UserJoinedMessage
        //    {
        //        User = new User
        //        {
        //            Id = null
        //        }
        //    };

        //    // when
        //    webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

        //    // then
        //    lastUser.ShouldBeNull();
        //    slackConnection.UserCache.ShouldBeEmpty();
        //}

        //[Theory, AutoMoqData]
        //private async Task should_not_raise_exception(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        //{
        //    // given
        //    var connectionInfo = new ConnectionInformation { WebSocket = webSocket.Object };
        //    await slackConnection.Initialise(connectionInfo);
        //    slackConnection.OnUserJoined += user => throw new NotImplementedException("THIS SHOULDN'T BUBBLE UP");

        //    var inboundMessage = new UserJoinedMessage
        //    {
        //        User = new User
        //        {
        //            Id = null
        //        }
        //    };

        //    // when & then (does not throw)
        //    webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);
        //}
    }
}