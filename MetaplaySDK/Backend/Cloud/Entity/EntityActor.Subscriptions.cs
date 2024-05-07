// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Cloud.Sharding;
using Metaplay.Core;
using Metaplay.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Metaplay.Cloud.Entity
{
    partial class EntityActor
    {
        protected Dictionary<int, EntitySubscriber>     _subscribers    = new Dictionary<int, EntitySubscriber>();
        protected Dictionary<int, EntitySubscription>   _subscriptions  = new Dictionary<int, EntitySubscription>();

        // Temporary state for the duration of OnNewSubscriber
        EntitySubscriber                    _ongoingNewSubscriber           = null;
        List<PubSub.PubSubMessage>          _ongoingNewSubscriberMsgQueue   = null;

        async Task ReceiveWatchedEntityTerminated(PubSub.WatchedEntityTerminated terminated)
        {
            EntityId entityId = terminated.EntityId;

            // Resolve all lost subscribers & subscriptions (take copies to avoid mutating dictionaries while traversing)
            // \todo [petri] allocates empty lists when no matching entities
            List<EntitySubscriber> lostSubscribers = _subscribers.Values.Where(subscriber => subscriber.EntityId == entityId).ToList();
            List<EntitySubscription> lostSubscriptions = _subscriptions.Values.Where(subscription => subscription.EntityId == entityId).ToList();
            _log.Info("Watched entity {EntityId} terminated unexpectedly (subscribers={Subscribers}, subscriptions={Subscriptions})!",
                entityId,
                string.Join(",", lostSubscribers.Select(sub => sub.EntityId)),
                string.Join(",", lostSubscriptions.Select(sub => sub.EntityId)));

            // Handle lost subscribers (if any)
            bool didLoseSubscribers = false;
            foreach (EntitySubscriber subscriber in lostSubscribers)
            {
                if (_subscribers.Remove(subscriber.InChannelId))
                {
                    OnSubscriberTerminated(subscriber);
                    OnSubscriberLost(subscriber);
                    didLoseSubscribers = true;
                }
            }

            // Handle lost subscriptions (if any)
            foreach (EntitySubscription subscription in lostSubscriptions)
            {
                _subscriptions.Remove(subscription.InChannelId);
                await OnSubscriptionLost(subscription);
            }

            if (didLoseSubscribers)
                AfterSubscribersLostInternal();
        }

        async Task ReceivePubSubMessage(PubSub.PubSubMessage envelope)
        {
            EntityId fromEntityId = envelope.FromEntityId;

            if (_subscriptions.TryGetValue(envelope.ChannelId, out EntitySubscription subscription))
            {
                MetaMessage msg = envelope.Payload.Deserialize(resolver: null, logicVersion: null);
                Type msgType = msg.GetType();

                if (_dispatcher.TryGetPubSubSubscriptionDispatchFunc(msgType, out var pubSubDispatchFunc))                  await pubSubDispatchFunc(this, subscription, msg).ConfigureAwait(false);
                else if (_dispatcher.TryGetMessageDispatchFunc(msgType, out var msgDispatchFunc))                           await msgDispatchFunc(this, fromEntityId, msg).ConfigureAwait(false);
                else if (_dispatcher.TryGetPubSubSubscriptionDispatchFunc(typeof(MetaMessage), out pubSubDispatchFunc))     await pubSubDispatchFunc(this, subscription, msg).ConfigureAwait(false);
                else                                                                                                        await HandleUnknownMessage(fromEntityId, msg).ConfigureAwait(false);
            }
            else if (_subscribers.TryGetValue(envelope.ChannelId, out EntitySubscriber subscriber))
            {
                MetaMessage msg = envelope.Payload.Deserialize(resolver: null, logicVersion: null);
                Type msgType = msg.GetType();

                if (_dispatcher.TryGetPubSubSubscriberDispatchFunc(msgType, out var pubSubDispatchFunc))                await pubSubDispatchFunc(this, subscriber, msg).ConfigureAwait(false);
                else if (_dispatcher.TryGetMessageDispatchFunc(msgType, out var msgDispatchFunc))                       await msgDispatchFunc(this, fromEntityId, msg).ConfigureAwait(false);
                else if (_dispatcher.TryGetPubSubSubscriberDispatchFunc(typeof(MetaMessage), out pubSubDispatchFunc))   await pubSubDispatchFunc(this, subscriber, msg).ConfigureAwait(false);
                else                                                                                                    await HandleUnknownMessage(fromEntityId, msg).ConfigureAwait(false);
            }
            else
            {
                _log.Info("Received PubSub message from {EntityId} on unknown channel ${ChannelId}: {MessageType}", fromEntityId, envelope.ChannelId, MetaSerializationUtil.PeekMessageName(envelope.Payload));
            }
        }

        async Task InternalHandlePubSubSubscribeAskAsync(EntityShard.EntityAsk ask, PubSub.Subscribe subscribe)
        {
            // \note Multiple subscriptions from same Entity are allowed
            _log.Debug("New subscriber: {SubscriberId} / {SubscriberActor} ${ChannelId} [{Topic}]", subscribe.SubscriberId, subscribe.SubscriberActor, subscribe.ChannelId, subscribe.Topic);

            // Allocate incoming channel & create subscriber
            int inChannelId = _runningChannelId++;
            EntitySubscriber newSubscriber = new EntitySubscriber(subscribe.SubscriberId, subscribe.SubscriberActor, subscribe.Topic, inChannelId, subscribe.ChannelId);

            // Mark that we are currently processing a subscription.
            _ongoingNewSubscriber = newSubscriber;

            // Handle new subscriber
            // \note store subscriber after handling it (so won't get sent messages if handler publishes)
            MetaMessage response;
            try
            {
                response = await OnNewSubscriber(newSubscriber, subscribe.Message);
            }
            catch (EntityAskRefusal error) when (error.OriginEntity == EntityId.None)
            {
                // Handler threw a controlled exception, propagate to caller
                error.OriginEntity = _entityId;
                Tell(ask.OwnerShard, EntityShard.EntityAskReply.FailWithError(ask.Id, ask.FromEntityId, _entityId, error));

                // Drop pooled messages
                if (_ongoingNewSubscriberMsgQueue != null)
                {
                    foreach (PubSub.PubSubMessage envelope in _ongoingNewSubscriberMsgQueue)
                        _log.Warning("PubSub Message to {Peer} dropped because message was sent from handler that then rejected the Subscription. Message type: {MessageType}.", subscribe.SubscriberId, MetaSerializationUtil.PeekMessageName(envelope.Payload));
                    _ongoingNewSubscriberMsgQueue = null;
                }
                _ongoingNewSubscriber = null;
                return;
            }
            _subscribers.Add(inChannelId, newSubscriber);

            // Send response
            MetaSerialized<MetaMessage> serializedResponse = MetaSerialization.ToMetaSerialized(response, MetaSerializationFlags.IncludeAll, logicVersion: null);
            ReplyToAsk(ask, new PubSub.SubscribeAck(_self, inChannelId, serializedResponse));

            AfterNewSubscribersInternal();

            // Flush pooled messages
            if (_ongoingNewSubscriberMsgQueue != null)
            {
                foreach (PubSub.PubSubMessage envelope in _ongoingNewSubscriberMsgQueue)
                    Tell(newSubscriber.ActorRef, envelope);
                _ongoingNewSubscriberMsgQueue = null;
            }
            _ongoingNewSubscriber = null;
        }

        Task InternalHandlePubSubUnsubscribeAskAsync(EntityShard.EntityAsk ask, PubSub.Unsubscribe unsubscribe)
        {
            if (_subscribers.Remove(unsubscribe.ChannelId, out EntitySubscriber lostSubscriber))
            {
                if (unsubscribe.SubscriberId != lostSubscriber.EntityId)
                    _log.Warning("Received Unsubscribe from {SubscriberId} for ${ChannelId} [{Topic}] which was created by {EntityId}", unsubscribe.SubscriberId, unsubscribe.ChannelId, lostSubscriber.EntityId, lostSubscriber.Topic);
                else
                    _log.Debug("Entity {SubscriberId} unsubscribed ${ChannelId} [{Topic}]", unsubscribe.SubscriberId, unsubscribe.ChannelId, lostSubscriber.Topic);
                OnSubscriberUnsubscribed(lostSubscriber);
                OnSubscriberLost(lostSubscriber);
                ReplyToAsk(ask, new PubSub.UnsubscribeAck(PubSub.UnsubscribeResult.Success));

                AfterSubscribersLostInternal();
            }
            else
            {
                // \note This can happen if subscriber unsubscribes as it is kicked
                _log.Info("Received Unsubscribe from unknown subscriber ${ChannelId}: {SubscriberId}", unsubscribe.ChannelId, unsubscribe.SubscriberId);
                ReplyToAsk(ask, new PubSub.UnsubscribeAck(PubSub.UnsubscribeResult.UnknownSubscriber));
            }

            return Task.CompletedTask;
        }

        async Task InternalHandlePubSubKickedAsync(PubSub.SubscriberKicked kicked)
        {
            if (_subscriptions.Remove(kicked.ChannelId, out EntitySubscription subscription))
                await OnSubscriptionKicked(subscription, kicked.Message.Deserialize(resolver: null, logicVersion: null));
            else
                _log.Info("Kicked from unknown entity {EntityId} channel ${ChannelId}: {Message}", kicked.FromEntityId, kicked.ChannelId, MetaSerializationUtil.PeekMessageName(kicked.Message));
        }

        void AfterSubscribersLostInternal()
        {
            TryScheduleShutdownAfterSubscriberLoss();
        }

        void AfterNewSubscribersInternal()
        {
            TryCancelShutdownAfterNewSubscriber();
        }

        /// <summary>
        /// Subscribes to an entity. This forms an communication channel between this entity and the target entity, allowing target
        /// to send messages to this, and other subscribers, by using <see cref="PublishMessage(EntityTopic, MetaMessage)"/> on the
        /// provided <paramref name="topic"/>.
        /// <para>
        /// The <paramref name="message"/> is delivered on target entity's <see cref="OnNewSubscriber(EntitySubscriber, MetaMessage)"/>
        /// and the return value from that method is delivered back as the return value of this method. If target entity rejects the
        /// subscription by throwing an <see cref="EntityAskRefusal"/> in the <see cref="OnNewSubscriber(EntitySubscriber, MetaMessage)"/> handler,
        /// the exception is delivered to caller entity and thrown from this method's invocation.
        /// </para>
        /// </summary>
        protected async Task<(EntitySubscription, TResult)> SubscribeToAsync<TResult>(EntityId targetEntityId, EntityTopic topic, MetaMessage message) where TResult : MetaMessage
        {
            // Check for valid target
            if (!targetEntityId.IsValid)
                throw new ArgumentException($"targetEntityId must be a valid entity (got {targetEntityId})", nameof(targetEntityId));

            // Check against duplicate subscriptions to target (\note duplicate subscriptions could be supported, but it's likely a mistake, so checking here)
            if (_subscriptions.Values.Any(sub => sub.EntityId == targetEntityId))
                throw new InvalidOperationException($"Already subscribed to {targetEntityId} [{topic}]");

            // Allocate channelId for incoming messages & subscribe to target
            int inChannelId = _runningChannelId++;
            PubSub.Subscribe subscribe = new PubSub.Subscribe(targetEntityId, _entityId, _self, topic, inChannelId, message);

            // \note: This may throw a Timeout or a Controlled Exception (EntityAskRefusal) or Unhandled Remote Exception (UnexpectedEntityAskError).
            string targetTypeStr = targetEntityId.Kind.Name;
            string metricsLabel;
            if (message != null)
                metricsLabel = $"Subscribe-{targetTypeStr}-{message.GetType().Name}";
            else
                metricsLabel = $"Subscribe-{targetTypeStr}";
            PubSub.SubscribeAck ack = await InternalEntityAskAsync<PubSub.SubscribeAck>(targetEntityId, subscribe, timeout: TimeSpan.FromMilliseconds(10_000), metricsLabel: metricsLabel);

            // Store subscription (using subsciber's channelId)
            EntitySubscription subscription = new EntitySubscription(targetEntityId, ack.TargetActor, topic, inChannelId, ack.ChannelId);
            _subscriptions.Add(inChannelId, subscription);

            c_entitySubscribes.WithLabels(targetTypeStr).Inc();

            // Handle response type
            MetaMessage response = ack.Response.Deserialize(resolver: null, logicVersion: null);
            if (response == null)
                return (subscription, null);
            else if (response is TResult typed)
                return (subscription, typed);
            else
            {
                c_entitySubscribeErrors.WithLabels(targetTypeStr, "invalidResponse").Inc();
                throw new InvalidOperationException($"Invalid response type for SubscribeToAsync<{targetTypeStr}> from {_entityId} to {targetEntityId}: got {response.GetType().ToGenericTypeString()} (expecting {typeof(TResult).ToGenericTypeString()})");
            }
        }

        [Obsolete("Use the statically-typed overload. Note that TResult may be MetaMessage.")]
        protected Task<(EntitySubscription, MetaMessage)> SubscribeToAsync(EntityId targetEntityId, EntityTopic topic, MetaMessage message)
        {
            // \note prefer statically-typed SubscribeToAsync<TResponse> when response value type is known beforehand
            return SubscribeToAsync<MetaMessage>(targetEntityId, topic, message);
        }

        /// <summary>
        /// Unsubscribes from the entity and waits until the target entity acknowledges the delivery of all the messages
        /// this entity has sent to the pubsub channel.
        /// <para>
        /// On success, returns true. If messages cannot be guaranteed to have been delivered, returns false.
        /// If waiting for the response timeouts, throws <see cref="TimeoutException"/>.
        /// </para>
        /// <para>
        /// Message delivery can fail for example if the target entity terminates abnormally, or has already kicked this subscriber.
        /// </para>
        /// </summary>
        protected async Task<bool> UnsubscribeFromAsync(EntitySubscription subscription)
        {
            if (!_subscriptions.Remove(subscription.InChannelId, out EntitySubscription existing) || !ReferenceEquals(subscription, existing))
                throw new InvalidOperationException($"Trying to unsubscribe unknown subscription to {subscription.EntityId} ${subscription.InChannelId}");

            c_entityUnsubscribes.WithLabels(subscription.EntityId.Kind.Name).Inc();

            // Unsubscribe
            PubSub.Unsubscribe unsubscribe = new PubSub.Unsubscribe(subscription.EntityId, _entityId, subscription.OutChannelId);
            try
            {
                PubSub.UnsubscribeAck ack = await InternalEntityAskAsync<PubSub.UnsubscribeAck>(subscription.EntityId, unsubscribe, timeout: TimeSpan.FromMilliseconds(10_000), metricsLabel: $"Unsubscribe-{subscription.EntityId.Kind.Name}");
                if (ack.Result != PubSub.UnsubscribeResult.Success)
                {
                    _log.Info("UnsubscribeFromAsync to {TargetEntityId} returned {UnsubscribeResult}", subscription.EntityId, ack.Result);
                    return false;
                }
                return true;
            }
            catch (EntityAskExceptionBase)
            {
                // peer crashed during or before unsub request.
                return false;
            }
        }

        /// <summary>
        /// Removes the specified subscriber from this entity. This causes <see cref="OnSubscriptionKicked"/>
        /// to be (eventually) called on the kicked entity with the specified message.
        /// </summary>
        /// <param name="subscriber">the subscriber to be removed</param>
        /// <param name="message">kick message delivered to the subscriber</param>
        protected void KickSubscriber(EntitySubscriber subscriber, MetaMessage message)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            // Try to remove subscriber
            bool hadSuchSubscriber = _subscribers.Remove(subscriber.InChannelId);
            if (!hadSuchSubscriber)
                return;

            c_entitySubscriberKicked.WithLabels(subscriber.EntityId.Kind.ToString()).Inc();

            // Send kicked message to subscriber
            // \note EntityShard assumes SubscriberKicked message it delivered with CastMessage()
            MetaSerialized<MetaMessage> serialized = MetaSerialization.ToMetaSerialized(message, MetaSerializationFlags.IncludeAll, logicVersion: null);
            CastMessage(subscriber.EntityId, new PubSub.SubscriberKicked(_entityId, subscriber.OutChannelId, serialized));

            AfterSubscribersLostInternal();

            OnSubscriberKicked(subscriber, message);
        }

        /// <summary>
        /// Direct send message over a subscription channel.
        /// </summary>
        /// <param name="subscription">Subscription over which to send the message</param>
        /// <param name="message">Message to send</param>
        protected void SendMessage(EntitySubscription subscription, MetaMessage message)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            MetaSerialized<MetaMessage> serialized = MetaSerialization.ToMetaSerialized(message, MetaSerializationFlags.IncludeAll, logicVersion: null);
            Tell(subscription.ActorRef, new PubSub.PubSubMessage(_entityId, subscription.OutChannelId, serialized));
        }

        /// <summary>
        /// Direct send message over a subscription channel. Calling this during <see cref="OnNewSubscriber" /> for the
        /// new yet-to-be-added subscriber causes the message to be queued until the OnNewSubscriber returns, and only
        /// then flushed on the added subscriber channel.
        /// </summary>
        /// <param name="subscriber">Subscriber to which to send the message</param>
        /// <param name="message">Message to send</param>
        protected void SendMessage(EntitySubscriber subscriber, MetaMessage message)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            MetaSerialized<MetaMessage> serialized = new MetaSerialized<MetaMessage>(message, MetaSerializationFlags.IncludeAll, logicVersion: null);
            PubSub.PubSubMessage envelope = new PubSub.PubSubMessage(_entityId, subscriber.OutChannelId, serialized);

            if (!ReferenceEquals(subscriber, _ongoingNewSubscriber))
            {
                // existing subscriber channel
                Tell(subscriber.ActorRef, envelope);
            }
            else
            {
                // sending to a channel that is just about to be created. We accepted the subscriber but we haven't
                // replied yet. Buffer the messages until we reply to the request.
                _ongoingNewSubscriberMsgQueue = _ongoingNewSubscriberMsgQueue ?? new List<PubSub.PubSubMessage>();
                _ongoingNewSubscriberMsgQueue.Add(envelope);
            }
        }

        /// <summary>
        /// Publish message to all subscribers on a set of topics.
        /// </summary>
        /// <param name="flags">Bitmask of topics to send message to</param>
        /// <param name="message">Message to send</param>
        protected void PublishMessage(EntityTopicFlags flags, MetaMessage message)
        {
            // Message is serialized lazily (for first actual receiver)
            MetaSerialized<MetaMessage> serialized = new MetaSerialized<MetaMessage>();

            foreach (EntitySubscriber subscriber in _subscribers.Values)
            {
                if (((1u << (int)subscriber.Topic) & (uint)flags) != 0)
                {
                    if (serialized.IsEmpty)
                        serialized = MetaSerialization.ToMetaSerialized(message, MetaSerializationFlags.IncludeAll, logicVersion: null);

                    Tell(subscriber.ActorRef, new PubSub.PubSubMessage(_entityId, subscriber.OutChannelId, serialized));
                }
            }
        }

        /// <summary>
        /// Publish a message to subscribers on a given topic.
        /// </summary>
        /// <param name="topic">Topic on which to publish message on</param>
        /// <param name="message">Message to publish</param>
        protected void PublishMessage(EntityTopic topic, MetaMessage message)
        {
            PublishMessage((EntityTopicFlags)(1 << (int)topic), message);
        }

        /// <summary>
        /// Called just before a new subscriber to this entity is added.
        /// <para>
        /// If this method throws an <see cref="EntityAskRefusal"/>, the subcription is refused instead.
        /// In that case, the subscription is not added and the exception will be delivered to the
        /// caller-entity's call-site <see cref="SubscribeToAsync{TResult}(EntityId, EntityTopic, MetaMessage)"/>,
        /// where it will be thrown.
        /// </para>
        /// <para>
        /// Throwing a refusal does not crash this actor. This actor will remain alive.
        /// </para>
        /// </summary>
        /// <param name="subscriber">the new subscription</param>
        /// <param name="message">the message supplied in <see cref="SubscribeToAsync"/>, if any</param>
        /// <returns>the message supplied to <see cref="SubscribeToAsync"/> return value, if any</returns>
        protected virtual Task<MetaMessage> OnNewSubscriber(EntitySubscriber subscriber, MetaMessage message) => Task.FromResult<MetaMessage>(null);

        /// <summary>
        /// Called after a subscriber to this entity is removed. This is due to the subscriber either terminating
        /// or calling <see cref="UnsubscribeFromAsync"/>.
        /// <para>
        /// See also:
        /// <seealso cref="OnSubscriberTerminated(EntitySubscriber)"/>
        /// <seealso cref="OnSubscriberUnsubscribed(EntitySubscriber)"/>
        /// </para>
        /// </summary>
        /// <param name="subscriber">the terminated subscription</param>
        protected virtual void OnSubscriberLost(EntitySubscriber subscriber) { }

        /// <summary>
        /// Called after a subscriber to this entity is removed due to the subscriber entity terminating.
        /// This method is called just before <see cref="OnSubscriberLost"/>.
        /// <para>
        /// See also:
        /// <seealso cref="OnSubscriberLost(EntitySubscriber)"/>
        /// <seealso cref="OnSubscriberUnsubscribed(EntitySubscriber)"/>
        /// </para>
        /// </summary>
        /// <param name="subscriber">the terminated subscription</param>
        protected virtual void OnSubscriberTerminated(EntitySubscriber subscriber) { }

        /// <summary>
        /// Called after a subscriber to this entity is removed due to the subscriber calling <see cref="UnsubscribeFromAsync"/>.
        /// This method is called just before <see cref="OnSubscriberLost"/>.
        /// <para>
        /// See also:
        /// <seealso cref="OnSubscriberLost(EntitySubscriber)"/>
        /// <seealso cref="OnSubscriberTerminated(EntitySubscriber)"/>
        /// </para>
        /// </summary>
        /// <param name="subscriber">the terminated subscription</param>
        protected virtual void OnSubscriberUnsubscribed(EntitySubscriber subscriber) { }

        /// <summary>
        /// Called after a subscriber is kicked by calling <see cref="KickSubscriber" />. Useful in conjunction with <see cref="OnSubscriberLost(EntitySubscriber)"/>
        /// to track all events where a subscriber is removed from an entity.
        /// </summary>
        /// <remarks>
        /// This method is called synchronously from <see cref="KickSubscriber" />.
        /// </remarks>
        /// <param name="subscriber">the kicked subscriber</param>
        /// <param name="message">the message specified in KickSubscriber, if any</param>
        protected virtual void OnSubscriberKicked(EntitySubscriber subscriber, MetaMessage message) { }

        /// <summary>
        /// Called after a subscription to an entity is terminated due to the entity terminating.
        /// </summary>
        /// <param name="subscription">the terminated subscription</param>
        protected virtual Task OnSubscriptionLost(EntitySubscription subscription)
        {
            _log.Warning("Unhandled lost subscription to {TargetActor} [{Topic}]", subscription.ActorRef, subscription.Topic);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called after a subscription to an entity is terminated due to the entity calling <see cref="KickSubscriber" />.
        /// </summary>
        /// <param name="subscription">the terminated subscription</param>
        /// <param name="message">the message specified in KickSubscriber, if any</param>
        protected virtual Task OnSubscriptionKicked(EntitySubscription subscription, MetaMessage message)
        {
            _log.Debug("Kicked from entity {EntityId} channel ${ChannelId} [{Topic}]: {MessageName}", subscription.EntityId, subscription.InChannelId, subscription.Topic, message?.GetType().Name ?? "<empty>");
            return Task.CompletedTask;
        }
    }
}
