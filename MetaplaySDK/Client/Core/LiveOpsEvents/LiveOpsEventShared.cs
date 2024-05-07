// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core.Config;
using Metaplay.Core.Json;
using Metaplay.Core.Model;
using Metaplay.Core.Player;
using Metaplay.Core.Schedule;
using Metaplay.Core.Serialization;
using Metaplay.Core.TypeCodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Metaplay.Core.LiveOpsEvent
{
    [MetaSerializable]
    [MetaReservedMembers(100, 200)]
    public abstract class LiveOpsEventContent : IMetaIntegration<LiveOpsEventContent>
    {
        public virtual bool GetActivationIsSticky() => false;
        public virtual bool ShouldWarnAboutOverlapWith(LiveOpsEventContent otherContent) => false;

        public abstract PlayerLiveOpsEventModel CreateModel(PlayerLiveOpsEventInfo info);
    }

    public abstract class LiveOpsEventContent<TEventContent, TEventModel> : LiveOpsEventContent
        where TEventContent : LiveOpsEventContent<TEventContent, TEventModel>
        where TEventModel : PlayerLiveOpsEventModel<TEventContent>
    {
        public abstract TEventModel CreateModel(PlayerLiveOpsEventInfo<TEventContent> info);
        public sealed override PlayerLiveOpsEventModel CreateModel(PlayerLiveOpsEventInfo info)
        {
            return CreateModel(new PlayerLiveOpsEventInfo<TEventContent>(info));
        }
    }

    public interface ILiveOpsEventTemplate
    {
        LiveOpsEventTemplateId TemplateId  { get; }
        LiveOpsEventContent    ContentBase { get; }
    }

    [MetaSerializable]
    public class LiveOpsEventTemplateConfigData<TContentClass> : IGameConfigData<LiveOpsEventTemplateId>, ILiveOpsEventTemplate where TContentClass : LiveOpsEventContent
    {
        [MetaMember(1)]
        public LiveOpsEventTemplateId TemplateId { get; protected set; }
        [MetaMember(2)]
        public TContentClass          Content    { get; protected set; }
        [IgnoreDataMember]
        public LiveOpsEventContent    ContentBase => Content;
        public LiveOpsEventTemplateId ConfigKey => TemplateId;
    }

    public interface IPlayerLiveOpsEventInfo
    {
        MetaGuid Id { get; }
        LiveOpsEventScheduleOccasion ScheduleMaybe { get; }
        LiveOpsEventContent Content { get; }
        LiveOpsEventPhase Phase { get; }
    }

    public class PlayerLiveOpsEventInfo : IPlayerLiveOpsEventInfo
    {
        public MetaGuid Id { get; }
        public LiveOpsEventScheduleOccasion ScheduleMaybe { get; }
        public LiveOpsEventContent Content { get; }
        public LiveOpsEventPhase Phase { get; }

        public PlayerLiveOpsEventInfo(MetaGuid id, LiveOpsEventScheduleOccasion scheduleMaybe, LiveOpsEventContent content, LiveOpsEventPhase phase)
        {
            Id = id;
            ScheduleMaybe = scheduleMaybe;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Phase = phase ?? throw new ArgumentNullException(nameof(phase));
        }
    }

    public class PlayerLiveOpsEventInfo<TEventContent> : IPlayerLiveOpsEventInfo
        where TEventContent : LiveOpsEventContent
    {
        PlayerLiveOpsEventInfo _info;

        public MetaGuid Id => _info.Id;
        public LiveOpsEventScheduleOccasion ScheduleMaybe => _info.ScheduleMaybe;
        public TEventContent Content => (TEventContent)_info.Content;
        public LiveOpsEventPhase Phase => _info.Phase;

        LiveOpsEventContent IPlayerLiveOpsEventInfo.Content => _info.Content;

        public PlayerLiveOpsEventInfo(PlayerLiveOpsEventInfo info)
        {
            if (!(info.Content is TEventContent))
                throw new ArgumentException($"Invalid content type {info.Content.GetType().ToGenericTypeString()} given to {GetType().ToGenericTypeString()}");

            _info = info;
        }
    }

    [MetaSerializable]
    public class LiveOpsEventScheduleOccasion
    {
        [MetaMember(1)] public OrderedDictionary<LiveOpsEventPhase, MetaTime> PhaseSequence { get; private set; }

        LiveOpsEventScheduleOccasion() { }
        public LiveOpsEventScheduleOccasion(OrderedDictionary<LiveOpsEventPhase, MetaTime> phaseSequence)
        {
            PhaseSequence = phaseSequence ?? throw new ArgumentNullException(nameof(phaseSequence));
        }

        /// <summary>
        /// Called on an occasion which represents the times of the occasion
        /// when it occurs in an UTC+0 zone, this returns an occasion which represents the times
        /// when it occurs for a player who has UTC offset <paramref name="playerUtcOffset"/>,
        /// with <paramref name="timeMode"/> controlling whether the occasion is the same for
        /// all players (<see cref="MetaScheduleTimeMode.Utc"/>) or depends on the player's
        /// local UTC offset (<see cref="MetaScheduleTimeMode.Local"/>).
        /// <para>
        /// When <paramref name="timeMode"/> is <see cref="MetaScheduleTimeMode.Utc"/>,
        /// this returns the same occasion unchanged.
        /// </para>
        /// <para>
        /// When <paramref name="timeMode"/> is <see cref="MetaScheduleTimeMode.Local"/>,
        /// the offset is subtracted from (<em>not</em> added to) the occasion's times
        /// Please see the example below.
        /// </para>
        /// <example>
        /// For example, if <c>utcOccasion.VisibilityStartTime</c> is <c>2024-02-20 12:00:00.000 Z</c>, then
        /// <c>GetUtcOccasionForPlayer(utcOccasion, MetaScheduleTimeMode.Local, MetaDuration.FromHours(2))</c>
        /// returns an occasion whose <c>VisibilityStartTime</c> is <c>2024-02-20 10:00:00.000 Z</c>
        /// (and the other time members are also similarly offset).
        /// This is because the UTC offset is 2 hours, meaning the player's local time is 2 hours
        /// ahead of UTC, meaning the occasion occurs 2 hours earlier than it does in UTC+0 zones.
        /// </example>
        /// </summary>
        public LiveOpsEventScheduleOccasion GetUtcOccasionAdjustedForPlayer(MetaScheduleTimeMode timeMode, MetaDuration playerUtcOffset)
        {
            if (timeMode == MetaScheduleTimeMode.Local)
            {
                // Note the negation of the offset.
                return CreateWithAddedOffset(-playerUtcOffset);
            }
            else
                return this;
        }

        LiveOpsEventScheduleOccasion CreateWithAddedOffset(MetaDuration offset)
        {
            OrderedDictionary<LiveOpsEventPhase, MetaTime> offsetPhaseSequence = new OrderedDictionary<LiveOpsEventPhase, MetaTime>(capacity: PhaseSequence.Count);

            foreach ((LiveOpsEventPhase phase, MetaTime time) in PhaseSequence)
                offsetPhaseSequence.Add(phase, time + offset);

            return new LiveOpsEventScheduleOccasion(offsetPhaseSequence);
        }

        public MetaTime GetEnabledStartTime()
        {
            return PhaseSequence[LiveOpsEventPhase.NormalActive];
        }

        public MetaTime GetEnabledEndTime()
        {
            if (PhaseSequence.TryGetValue(LiveOpsEventPhase.Review, out MetaTime review))
                return review;
            else
                return PhaseSequence[LiveOpsEventPhase.Disappeared];
        }

        public LiveOpsEventPhase GetPhaseAtTime(MetaTime currentTime)
        {
            LiveOpsEventPhase latestStartedPhase = LiveOpsEventPhase.NotStartedYet;
            foreach ((LiveOpsEventPhase phase, MetaTime phaseStartTime) in PhaseSequence)
            {
                if (currentTime < phaseStartTime)
                    break;

                latestStartedPhase = phase;
            }

            return latestStartedPhase;
        }

        public IEnumerable<LiveOpsEventPhase> GetPhasesBetween(LiveOpsEventPhase startPhaseExclusive, LiveOpsEventPhase endPhaseExclusive)
        {
            return
                LiveOpsEventPhase.GetPhasesBetween(startPhaseExclusive: startPhaseExclusive, endPhaseExclusive: endPhaseExclusive)
                .Where(phase => PhaseSequence.ContainsKey(phase));
        }
    }

    [MetaSerializable]
    [MetaReservedMembers(100, 200)]
    public abstract class PlayerLiveOpsEventModel
    {
        [MetaMember(100)] public MetaGuid Id { get; private set; }
        [MetaMember(101)] public LiveOpsEventScheduleOccasion ScheduleMaybe { get; set; }
        [MetaMember(102)] LiveOpsEventContent _content;
        [MetaMember(103)] public LiveOpsEventPhase Phase { get; set; }

        public LiveOpsEventContent Content { get => _content; set { _content = value; } }

        public PlayerLiveOpsEventInfo GetEventInfo() => new PlayerLiveOpsEventInfo(Id, ScheduleMaybe, Content, Phase);

        protected PlayerLiveOpsEventModel() { }
        protected PlayerLiveOpsEventModel(IPlayerLiveOpsEventInfo info)
        {
            Id = info.Id;
            ScheduleMaybe = info.ScheduleMaybe;
            Content = info.Content;
            Phase = info.Phase;
        }

        public virtual void OnPhaseChanged(IPlayerModelBase player, LiveOpsEventPhase oldPhase, LiveOpsEventPhase[] fastForwardedPhases, LiveOpsEventPhase newPhase) { }
        public virtual void OnParamsUpdated(IPlayerModelBase player, PlayerLiveOpsEventInfo oldInfo, PlayerLiveOpsEventInfo newInfo) { }
        public virtual void OnForceSetInfo(IPlayerModelBase player, PlayerLiveOpsEventInfo oldInfo, PlayerLiveOpsEventInfo newInfo) { }
        public virtual void OnAbruptlyRemoved(IPlayerModelBase player) { }
    }

    public abstract class PlayerLiveOpsEventModel<TEventContent> : PlayerLiveOpsEventModel
        where TEventContent : LiveOpsEventContent
    {
        public new TEventContent Content => (TEventContent)base.Content;

        protected PlayerLiveOpsEventModel() { }
        protected PlayerLiveOpsEventModel(PlayerLiveOpsEventInfo<TEventContent> info) : base(info) { }
    }

    public abstract class PlayerLiveOpsEventModel<TPlayerModel, TEventContent> : PlayerLiveOpsEventModel<TEventContent>
        where TPlayerModel : IPlayerModelBase
        where TEventContent : LiveOpsEventContent
    {
        protected PlayerLiveOpsEventModel() { }
        protected PlayerLiveOpsEventModel(PlayerLiveOpsEventInfo<TEventContent> info) : base(info) { }

        protected virtual void OnPhaseChanged(TPlayerModel player, LiveOpsEventPhase oldPhase, LiveOpsEventPhase[] fastForwardedPhases, LiveOpsEventPhase newPhase) { }
        public override sealed void OnPhaseChanged(IPlayerModelBase player, LiveOpsEventPhase oldPhase, LiveOpsEventPhase[] fastForwardedPhases, LiveOpsEventPhase newPhase)
        {
            OnPhaseChanged((TPlayerModel)player, oldPhase, fastForwardedPhases, newPhase);
        }

        protected virtual void OnParamsUpdated(TPlayerModel player, PlayerLiveOpsEventInfo<TEventContent> oldInfo, PlayerLiveOpsEventInfo<TEventContent> newInfo) { }
        public override sealed void OnParamsUpdated(IPlayerModelBase player, PlayerLiveOpsEventInfo oldInfo, PlayerLiveOpsEventInfo newInfo)
        {
            OnParamsUpdated((TPlayerModel)player, new PlayerLiveOpsEventInfo<TEventContent>(oldInfo), new PlayerLiveOpsEventInfo<TEventContent>(newInfo));
        }

        protected virtual void OnForceSetInfo(TPlayerModel player, PlayerLiveOpsEventInfo<TEventContent> oldInfo, PlayerLiveOpsEventInfo<TEventContent> newInfo) { }
        public override sealed void OnForceSetInfo(IPlayerModelBase player, PlayerLiveOpsEventInfo oldInfo, PlayerLiveOpsEventInfo newInfo)
        {
            OnForceSetInfo((TPlayerModel)player, new PlayerLiveOpsEventInfo<TEventContent>(oldInfo), new PlayerLiveOpsEventInfo<TEventContent>(newInfo));
        }

        protected virtual void OnAbruptlyRemoved(TPlayerModel player) {}
        public override sealed void OnAbruptlyRemoved(IPlayerModelBase player)
        {
            OnAbruptlyRemoved((TPlayerModel)player);
        }
    }

    [MetaSerializable]
    public class LiveOpsEventPhase : DynamicEnum<LiveOpsEventPhase>
    {
        public static readonly LiveOpsEventPhase NotStartedYet  = new LiveOpsEventPhase(1, nameof(NotStartedYet));
        public static readonly LiveOpsEventPhase Preview        = new LiveOpsEventPhase(2, nameof(Preview));
        public static readonly LiveOpsEventPhase NormalActive   = new LiveOpsEventPhase(3, nameof(NormalActive));
        public static readonly LiveOpsEventPhase EndingSoon     = new LiveOpsEventPhase(4, nameof(EndingSoon));
        public static readonly LiveOpsEventPhase Review         = new LiveOpsEventPhase(5, nameof(Review));
        public static readonly LiveOpsEventPhase Disappeared    = new LiveOpsEventPhase(6, nameof(Disappeared));

        public LiveOpsEventPhase(int id, string name) : base(id, name, isValid: true) { }

        public bool IsActivePhase()
        {
            return this == NormalActive
                || this == EndingSoon;
        }

        /// <summary>
        /// Gets the sequence of all the possible phases, in the order they occur.
        /// This is the "full" sequence, and a specific schedule might contain only
        /// a subset of these phases, e.g. might not have a Preview phase.
        ///
        /// The distinction between a schedule not having a phase, and having a phase
        /// with 0 duration, is that when a phase is not present, it won't be reported
        /// via <see cref="PlayerLiveOpsEventModel.OnPhaseChanged"/>, whereas if a 0-duration
        /// phase was present, it would be reported with that method when the time advances
        /// over that phase.
        /// </summary>
        public LiveOpsEventPhase[] GetFullPhaseSequence() => s_fullPhaseSequence;

        static readonly LiveOpsEventPhase[] s_fullPhaseSequence = new LiveOpsEventPhase[]
        {
            NotStartedYet,
            Preview,
            NormalActive,
            EndingSoon,
            Review,
            Disappeared,
        };

        public static IEnumerable<LiveOpsEventPhase> GetPhasesBetween(LiveOpsEventPhase startPhaseExclusive, LiveOpsEventPhase endPhaseExclusive)
        {
            bool foundStart = startPhaseExclusive == LiveOpsEventPhase.NotStartedYet;

            foreach (LiveOpsEventPhase phase in s_fullPhaseSequence)
            {
                if (phase == endPhaseExclusive)
                    break;

                if (!foundStart)
                {
                    if (phase == startPhaseExclusive)
                        foundStart = true;

                    continue;
                }

                yield return phase;
            }
        }

        /// <summary>
        /// Whether phase <paramref name="first"/> comes before phase <paramref name="second"/>
        /// in the full phase sequence (<see cref="GetFullPhaseSequence"/>).
        /// </summary>
        public static bool PhasePrecedes(LiveOpsEventPhase first, LiveOpsEventPhase second)
        {
            return GetPhaseIndexInFullSequence(first) < GetPhaseIndexInFullSequence(second);
        }

        static int GetPhaseIndexInFullSequence(LiveOpsEventPhase phase)
        {
            int ndx = Array.IndexOf(s_fullPhaseSequence, phase);
            if (ndx < 0)
                throw new ArgumentException($"Phase '{phase}' is not present in the full phase sequence", nameof(phase));
            return ndx;
        }
    }

    [MetaSerializable]
    public class PlayerLiveOpsEventsModel
    {
        [MetaMember(1)] public OrderedDictionary<MetaGuid, PlayerLiveOpsEventModel> EventModels { get; private set; } = new OrderedDictionary<MetaGuid, PlayerLiveOpsEventModel>();
        [MetaMember(3), ServerOnly] public PlayerLiveOpsEventsServerOnlyModel ServerOnly { get; private set; } = new PlayerLiveOpsEventsServerOnlyModel();

        [MetaMember(2)] public OrderedDictionary<MetaGuid, PlayerLiveOpsEventUpdate> LatestUnacknowledgedUpdatePerEvent { get; private set; } = new OrderedDictionary<MetaGuid, PlayerLiveOpsEventUpdate>();

        public void RecordUpdate(IPlayerModelBase player, PlayerLiveOpsEventUpdate update)
        {
            // \note Mutation of input parameter
            update.Timestamp = player.CurrentTime;

            LatestUnacknowledgedUpdatePerEvent[update.EventId] = update;

            player.ClientListenerCore.GotLiveOpsEventUpdate(update);
        }

        public PlayerLiveOpsEventUpdate TryGetEarliestUpdate(Func<PlayerLiveOpsEventUpdate, bool> filter = null)
        {
            PlayerLiveOpsEventUpdate earliestUpdate = null;
            foreach (PlayerLiveOpsEventUpdate update in LatestUnacknowledgedUpdatePerEvent.Values)
            {
                if (filter != null && !filter(update))
                    continue;

                if (earliestUpdate == null || update.Timestamp < earliestUpdate.Timestamp)
                    earliestUpdate = update;
            }

            return earliestUpdate;
        }

        public PlayerLiveOpsEventUpdate TryGetEarliestUpdate<TEventContent>()
            where TEventContent : LiveOpsEventContent
        {
            return TryGetEarliestUpdate(update => update.EventModel.Content is TEventContent);
        }
    }

    [MetaSerializable]
    public class PlayerLiveOpsEventsServerOnlyModel
    {
        [MetaMember(1)] public OrderedDictionary<MetaGuid, PlayerLiveOpsEventServerOnlyModel> EventModels { get; private set; } = new OrderedDictionary<MetaGuid, PlayerLiveOpsEventServerOnlyModel>();
    }

    [MetaSerializable]
    public class PlayerLiveOpsEventServerOnlyModel
    {
        [MetaMember(1)] public MetaGuid EventId { get; private set; }
        [MetaMember(2)] public LiveOpsEventPhase LatestAssignedPhase { get; set; }
        [MetaMember(3)] public int EditVersion { get; set; }

        PlayerLiveOpsEventServerOnlyModel() { }
        public PlayerLiveOpsEventServerOnlyModel(MetaGuid eventId, LiveOpsEventPhase latestAssignedPhase, int editVersion)
        {
            EventId = eventId;
            LatestAssignedPhase = latestAssignedPhase;
            EditVersion = editVersion;
        }
    }

    [MetaSerializable]
    public class PlayerLiveOpsEventUpdate
    {
        [MetaMember(5)] public PlayerLiveOpsEventUpdateType Type;
        [MetaMember(1)] public MetaTime Timestamp;
        [MetaMember(2)] public MetaGuid EventId;
        [MetaMember(3)] public PlayerLiveOpsEventModel EventModel;
        [MetaMember(4)] public bool IsNewEvent;
        [MetaMember(6)] public LiveOpsEventPhase[] FastForwardedPhases;
    }

    [MetaSerializable]
    public enum PlayerLiveOpsEventUpdateType
    {
        PhaseChanged = 0,
        ForceUpdated = 1,
        AbruptlyRemoved = 2,
        ParamsUpdated = 3,
    }

    [ModelAction(ActionCodesCore.PlayerAddLiveOpsEvent)]
    public class PlayerAddLiveOpsEvent : PlayerSynchronizedServerActionCore<IPlayerModelBase>
    {
        [MetaMember(1)] public MetaGuid EventId { get; private set; }
        [MetaMember(2)] public LiveOpsEventScheduleOccasion ScheduleMaybe { get; private set; }
        [MetaMember(3)] public LiveOpsEventContent Content { get; private set; }

        PlayerAddLiveOpsEvent() { }
        public PlayerAddLiveOpsEvent(MetaGuid eventId, LiveOpsEventScheduleOccasion scheduleMaybe, LiveOpsEventContent content)
        {
            EventId = eventId;
            ScheduleMaybe = scheduleMaybe;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public override MetaActionResult Execute(IPlayerModelBase player, bool commit)
        {
            if (player.LiveOpsEvents.EventModels.ContainsKey(EventId))
                return MetaActionResult.AlreadyHasEvent;

            if (commit)
            {
                PlayerLiveOpsEventInfo eventInfo = new PlayerLiveOpsEventInfo(
                    EventId,
                    scheduleMaybe: MetaSerialization.CloneTagged(ScheduleMaybe, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver()),
                    content: MetaSerialization.CloneTagged(Content, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver()),
                    phase: LiveOpsEventPhase.NotStartedYet);

                PlayerLiveOpsEventModel eventModel = eventInfo.Content.CreateModel(eventInfo);
                player.LiveOpsEvents.EventModels.Add(EventId, eventModel);
            }

            return MetaActionResult.Success;
        }
    }

    [ModelAction(ActionCodesCore.PlayerRunLiveOpsPhaseSequence)]
    public class PlayerRunLiveOpsPhaseSequence : PlayerSynchronizedServerActionCore<IPlayerModelBase>
    {
        [MetaMember(1)] public MetaGuid EventId { get; private set; }
        [MetaMember(2)] public List<LiveOpsEventPhase> FastForwardedPhases { get; private set; }
        [MetaMember(3)] public LiveOpsEventPhase NewPhase { get; private set; }

        PlayerRunLiveOpsPhaseSequence() { }
        public PlayerRunLiveOpsPhaseSequence(MetaGuid eventId, List<LiveOpsEventPhase> fastForwardedPhases, LiveOpsEventPhase newPhase)
        {
            EventId = eventId;
            FastForwardedPhases = fastForwardedPhases ?? throw new ArgumentNullException(nameof(fastForwardedPhases));
            NewPhase = newPhase ?? throw new ArgumentNullException(nameof(newPhase));
        }

        public override MetaActionResult Execute(IPlayerModelBase player, bool commit)
        {
            if (!player.LiveOpsEvents.EventModels.ContainsKey(EventId))
                return MetaActionResult.NoSuchEvent;

            if (commit)
            {
                PlayerLiveOpsEventModel eventModel = player.LiveOpsEvents.EventModels[EventId];
                LiveOpsEventPhase[] fastForwardedPhasesCopy = FastForwardedPhases.ToArray();

                LiveOpsEventPhase oldPhase = eventModel.Phase;
                eventModel.Phase = NewPhase;

                eventModel.OnPhaseChanged(
                    player,
                    oldPhase: oldPhase,
                    fastForwardedPhases: fastForwardedPhasesCopy,
                    newPhase: NewPhase);

                player.LiveOpsEvents.RecordUpdate(player, new PlayerLiveOpsEventUpdate
                {
                    Type = PlayerLiveOpsEventUpdateType.PhaseChanged,
                    EventId = EventId,
                    EventModel = MetaSerialization.CloneTagged(eventModel, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver()),
                    IsNewEvent = oldPhase == LiveOpsEventPhase.NotStartedYet,
                    FastForwardedPhases = fastForwardedPhasesCopy,
                });
            }

            return MetaActionResult.Success;
        }
    }

    [ModelAction(ActionCodesCore.PlayerUpdateEventLiveOpsEventParams)]
    public class PlayerUpdateEventLiveOpsEventParams : PlayerSynchronizedServerActionCore<IPlayerModelBase>
    {
        [MetaMember(1)] public MetaGuid EventId { get; private set; }
        [MetaMember(2)] public LiveOpsEventScheduleOccasion ScheduleMaybe { get; private set; }
        [MetaMember(3)] public LiveOpsEventContent Content { get; private set; }

        PlayerUpdateEventLiveOpsEventParams() { }
        public PlayerUpdateEventLiveOpsEventParams(MetaGuid eventId, LiveOpsEventScheduleOccasion scheduleMaybe, LiveOpsEventContent content)
        {
            EventId = eventId;
            ScheduleMaybe = scheduleMaybe;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public override MetaActionResult Execute(IPlayerModelBase player, bool commit)
        {
            if (!player.LiveOpsEvents.EventModels.ContainsKey(EventId))
                return MetaActionResult.NoSuchEvent;

            if (commit)
            {
                PlayerLiveOpsEventModel eventModel = player.LiveOpsEvents.EventModels[EventId];

                PlayerLiveOpsEventInfo oldInfo = eventModel.GetEventInfo();
                eventModel.ScheduleMaybe = MetaSerialization.CloneTagged(ScheduleMaybe, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver());
                eventModel.Content = MetaSerialization.CloneTagged(Content, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver());
                PlayerLiveOpsEventInfo newInfo = eventModel.GetEventInfo();

                eventModel.OnParamsUpdated(player, oldInfo: oldInfo, newInfo: newInfo);

                player.LiveOpsEvents.RecordUpdate(player, new PlayerLiveOpsEventUpdate
                {
                    Type = PlayerLiveOpsEventUpdateType.ParamsUpdated,
                    EventId = EventId,
                    EventModel = MetaSerialization.CloneTagged(eventModel, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver()),
                });
            }

            return MetaActionResult.Success;
        }
    }

    [ModelAction(ActionCodesCore.PlayerForceSetLiveOpsEventInfo)]
    public class PlayerForceSetLiveOpsEventInfo : PlayerSynchronizedServerActionCore<IPlayerModelBase>
    {
        [MetaMember(1)] public MetaGuid EventId { get; private set; }
        [MetaMember(2)] public LiveOpsEventScheduleOccasion NewScheduleMaybe { get; private set; }
        [MetaMember(3)] public LiveOpsEventContent NewContent { get; private set; }
        [MetaMember(4)] public LiveOpsEventPhase NewPhase { get; private set; }

        PlayerForceSetLiveOpsEventInfo() { }
        public PlayerForceSetLiveOpsEventInfo(MetaGuid eventId, LiveOpsEventScheduleOccasion newScheduleMaybe, LiveOpsEventContent newContent, LiveOpsEventPhase newPhase)
        {
            EventId = eventId;
            NewScheduleMaybe = newScheduleMaybe;
            NewContent = newContent ?? throw new ArgumentNullException(nameof(newContent));
            NewPhase = newPhase ?? throw new ArgumentNullException(nameof(newPhase));
        }

        public override MetaActionResult Execute(IPlayerModelBase player, bool commit)
        {
            if (!player.LiveOpsEvents.EventModels.ContainsKey(EventId))
                return MetaActionResult.NoSuchEvent;

            if (commit)
            {
                PlayerLiveOpsEventModel eventModel = player.LiveOpsEvents.EventModels[EventId];

                PlayerLiveOpsEventInfo oldInfo = eventModel.GetEventInfo();
                eventModel.ScheduleMaybe = MetaSerialization.CloneTagged(NewScheduleMaybe, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver());
                eventModel.Content = MetaSerialization.CloneTagged(NewContent, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver());
                eventModel.Phase = NewPhase;
                PlayerLiveOpsEventInfo newInfo = eventModel.GetEventInfo();

                eventModel.OnForceSetInfo(player, oldInfo: oldInfo, newInfo: newInfo);

                player.LiveOpsEvents.RecordUpdate(player, new PlayerLiveOpsEventUpdate
                {
                    Type = PlayerLiveOpsEventUpdateType.ForceUpdated,
                    EventId = EventId,
                    EventModel = MetaSerialization.CloneTagged(eventModel, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver()),
                });
            }

            return MetaActionResult.Success;
        }
    }

    [ModelAction(ActionCodesCore.PlayerRemoveDisappearedLiveOpsEvent)]
    public class PlayerRemoveDisappearedLiveOpsEvent : PlayerSynchronizedServerActionCore<IPlayerModelBase>
    {
        [MetaMember(1)] public MetaGuid EventId { get; private set; }

        PlayerRemoveDisappearedLiveOpsEvent() { }
        public PlayerRemoveDisappearedLiveOpsEvent(MetaGuid eventId)
        {
            EventId = eventId;
        }

        public override MetaActionResult Execute(IPlayerModelBase player, bool commit)
        {
            if (!player.LiveOpsEvents.EventModels.ContainsKey(EventId))
                return MetaActionResult.NoSuchEvent;
            if (player.LiveOpsEvents.EventModels[EventId].Phase != LiveOpsEventPhase.Disappeared)
                return MetaActionResult.EventNotDisappeared;

            if (commit)
            {
                player.LiveOpsEvents.EventModels.Remove(EventId);
            }

            return MetaActionResult.Success;
        }
    }

    [ModelAction(ActionCodesCore.PlayerAbruptlyRemoveLiveOpsEvent)]
    public class PlayerAbruptlyRemoveLiveOpsEvent : PlayerActionCore<IPlayerModelBase>
    {
        [MetaMember(1)] public MetaGuid EventId { get; private set; }

        PlayerAbruptlyRemoveLiveOpsEvent() { }
        public PlayerAbruptlyRemoveLiveOpsEvent(MetaGuid eventId)
        {
            EventId = eventId;
        }

        public override MetaActionResult Execute(IPlayerModelBase player, bool commit)
        {
            if (!player.LiveOpsEvents.EventModels.ContainsKey(EventId))
                return MetaActionResult.NoSuchEvent;

            if (commit)
            {
                PlayerLiveOpsEventModel eventModel = player.LiveOpsEvents.EventModels[EventId];

                player.LiveOpsEvents.EventModels.Remove(EventId);

                eventModel.OnAbruptlyRemoved(player);

                player.LiveOpsEvents.RecordUpdate(player, new PlayerLiveOpsEventUpdate
                {
                    Type = PlayerLiveOpsEventUpdateType.AbruptlyRemoved,
                    EventId = EventId,
                    EventModel = MetaSerialization.CloneTagged(eventModel, MetaSerializationFlags.IncludeAll, logicVersion: player.LogicVersion, resolver: player.GetDataResolver()),
                });
            }

            return MetaActionResult.Success;
        }
    }

    [ModelAction(ActionCodesCore.PlayerClearLiveOpsEventUpdates)]
    public class PlayerClearLiveOpsEventUpdates : PlayerActionCore<IPlayerModelBase>
    {
        [MetaMember(1)] public List<MetaGuid> UpdatesToClear { get; private set; }

        PlayerClearLiveOpsEventUpdates() { }
        public PlayerClearLiveOpsEventUpdates(List<MetaGuid> updatesToClear)
        {
            UpdatesToClear = updatesToClear ?? throw new ArgumentNullException(nameof(updatesToClear));
        }

        public override MetaActionResult Execute(IPlayerModelBase player, bool commit)
        {
            if (commit)
            {
                foreach (MetaGuid eventId in UpdatesToClear)
                    player.LiveOpsEvents.LatestUnacknowledgedUpdatePerEvent.Remove(eventId);
            }

            return MetaActionResult.Success;
        }
    }

    public struct EventTypeStaticInfo
    {
        public Type                         ContentClass { get; }
        public string                       EventTypeName { get; }
        public Func<FullGameConfig, object> ConfigTemplateLibraryGetter { get; }

        public EventTypeStaticInfo(Type contentClass, string eventTypeName, Func<FullGameConfig, object> configTemplateLibraryGetter)
        {
            ContentClass = contentClass ?? throw new ArgumentNullException(nameof(contentClass));
            EventTypeName = eventTypeName ?? throw new ArgumentNullException(nameof(eventTypeName));
            ConfigTemplateLibraryGetter = configTemplateLibraryGetter ?? throw new ArgumentNullException(nameof(configTemplateLibraryGetter));
        }
    }

    public class LiveOpsEventTypeRegistry
    {
        readonly EventTypeStaticInfo[] _types;

        static EventTypeStaticInfo ResolveStaticInfo(Type contentType, Func<FullGameConfig, object> templateAccessorMaybe)
        {
            return new EventTypeStaticInfo(
                contentClass:                   contentType,
                eventTypeName:                  contentType.Name, // \TODO support override from attribute
                configTemplateLibraryGetter:    templateAccessorMaybe);
        }

        IEnumerable<(Type, Func<FullGameConfig, object>)> EnumerateTemplateAccessors(Type configType, Func<FullGameConfig, IGameConfig> configTypeAccessor)
        {
            foreach (MemberInfo configEntryMember in GameConfigRepository.Instance.GetGameConfigTypeInfo(configType).Entries.Values.Select(entry => entry.MemberInfo))
            {
                Type configEntryType = configEntryMember.GetDataMemberType();
                if (!configEntryType.IsGameConfigLibrary())
                    continue;
                Type configDataType = configEntryType.GenericTypeArguments[1];
                if (configDataType.HasGenericAncestor(typeof(LiveOpsEventTemplateConfigData<>)))
                {
                    Type                 eventTypeForConfigEntry = configDataType.GetGenericAncestorTypeArguments(typeof(LiveOpsEventTemplateConfigData<>))[0];
                    Func<object, object> libraryGetter           = configEntryMember.GetDataMemberGetValueOnDeclaringType();
                    yield return (eventTypeForConfigEntry, x => libraryGetter(configTypeAccessor(x)));
                }
            }
        }

        LiveOpsEventTypeRegistry()
        {
            Dictionary<Type, Func<FullGameConfig, object>> templateAccessors =
                EnumerateTemplateAccessors(GameConfigRepository.Instance.ServerGameConfigType, x => x.ServerConfig)
                    .Concat(EnumerateTemplateAccessors(GameConfigRepository.Instance.SharedGameConfigType, x => x.SharedConfig))
                    .ToDictionary(x => x.Item1, x => x.Item2);

            _types = IntegrationRegistry.GetIntegrationClasses(typeof(LiveOpsEventContent))
                .Select(x => ResolveStaticInfo(x, templateAccessors.TryGetValue(x, out Func<FullGameConfig, object> accessor) ? accessor : null)).ToArray();
        }

        static LiveOpsEventTypeRegistry _instance;
        public static void Initialize()
        {
            _instance = new LiveOpsEventTypeRegistry();
        }
        public static IEnumerable<EventTypeStaticInfo> EventTypes => _instance._types;
    }

    #region Id types that should fundamentally only really be needed on the server, but are currently in shared code if only because ServerGameConfig is.

    [MetaSerializable]
    public class LiveOpsEventTemplateId : StringId<LiveOpsEventTemplateId> { }

    #endregion
}
