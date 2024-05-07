// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core;
using Metaplay.Core.Config;
using Metaplay.Core.LiveOpsEvent;
using Metaplay.Core.Model;
using Metaplay.Core.Player;
using Metaplay.Core.Schedule;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metaplay.Server.LiveOpsEvent
{
    [MetaSerializable]
    public class LiveOpsEventSettings
    {
        [MetaMember(1)] public MetaScheduleBase   ScheduleMaybe { get; private set; }
        [MetaMember(2)] public LiveOpsEventParams EventParams { get; private set; }

        LiveOpsEventSettings() { }
        public LiveOpsEventSettings(MetaScheduleBase scheduleMaybe, LiveOpsEventParams eventParams)
        {
            ScheduleMaybe = scheduleMaybe;
            EventParams = eventParams ?? throw new ArgumentNullException(nameof(eventParams));
        }
    }

    [MetaSerializable]
    public class LiveOpsEventParams : IPlayerFilter
    {
        [MetaMember(6)] public string DisplayName { get; private set; }
        [MetaMember(7)] public string Description { get; private set; }

        [MetaMember(2)] public List<EntityId>  TargetPlayersMaybe { get; private set; }
        [MetaMember(3)] public PlayerCondition TargetConditionMaybe { get; private set; }

        [MetaMember(4)] public LiveOpsEventTemplateId TemplateIdMaybe { get; private set; }
        [MetaMember(5)] public LiveOpsEventContent Content { get; private set; }

        [JsonIgnore] public PlayerFilterCriteria PlayerFilter => new PlayerFilterCriteria(TargetPlayersMaybe, TargetConditionMaybe);

        LiveOpsEventParams() { }
        public LiveOpsEventParams(string displayName, string description, List<EntityId> targetPlayersMaybe, PlayerCondition targetConditionMaybe, LiveOpsEventTemplateId templateIdMaybe, LiveOpsEventContent content)
        {
            DisplayName = displayName;
            Description = description;
            TargetPlayersMaybe = targetPlayersMaybe;
            TargetConditionMaybe = targetConditionMaybe;
            TemplateIdMaybe = templateIdMaybe;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }

    [MetaSerializable]
    public class LiveOpsEventSpec
    {
        [MetaMember(1)] public MetaGuid SpecId { get; private set; }

        [MetaMember(4)] public int EditVersion { get; private set; }

        // \todo #liveops-event Implement archival differently, by moving to a separate storage?
        // \todo #liveops-event Archive spec, occurrence, or both? Currently both.
        [MetaMember(5)] public bool     IsArchived     { get; private set; }

        [MetaMember(2)] public LiveOpsEventSettings Settings  { get; private set; }
        [MetaMember(3)] public MetaTime             CreatedAt { get; private set; }

        LiveOpsEventSpec() { }

        public LiveOpsEventSpec(MetaGuid specId, int editVersion, bool isArchived, LiveOpsEventSettings settings, MetaTime createdAt)
        {
            SpecId = specId;
            EditVersion = editVersion;
            IsArchived = isArchived;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            CreatedAt = createdAt;
        }
    }

    [MetaSerializable]
    public class LiveOpsEventOccurrence
    {
        [MetaMember(1)] public MetaGuid OccurrenceId { get; private set; }

        // \todo #liveops-event Keep edit version only in LiveOpsEventSpec? Currently in both spec and occurrence.
        // \todo #liveops-event Separate edit version for client-visible parts. For example if only audience targeting
        //       has been edited, then no update needs to be sent to the client. (The audience targeting change
        //       may affect the player, but the effect for the client will be other than an "event params update".)
        [MetaMember(7)] public int EditVersion { get; private set; }

        // \todo #liveops-event Implement archival differently, by moving to a separate storage?
        // \todo #liveops-event Archive spec, occurrence, or both? Currently both.
        [MetaMember(6)] public bool     IsArchived     { get; private set; }
        [MetaMember(2)] public MetaGuid DefiningSpecId { get; private set; }
        /// <summary>
        /// Determines whether the schedule is the same for all players (<see cref="MetaScheduleTimeMode.Utc"/>)
        /// or whether it depends on the player's local time (<see cref="MetaScheduleTimeMode.Local"/>).
        /// See <see cref="UtcScheduleOccasionMaybe"/>.
        /// </summary>
        [MetaMember(3)]
        public MetaScheduleTimeMode ScheduleTimeMode { get; private set; }
        /// <summary>
        /// Optional schedule for the occurrence, in UTC.
        /// <para>
        /// If not null: <br/>
        /// If <see cref="ScheduleTimeMode"/> is <see cref="MetaScheduleTimeMode.Utc"/>, this
        /// is the schedule that is used regardless of player's UTC offset. <br/>
        /// If <see cref="ScheduleTimeMode"/> is <see cref="MetaScheduleTimeMode.Local"/>,
        /// the schedule that is used for the player is acquired by subtracting the player's UTC offset from this.
        /// In other words, this is the schedule that is used when the UTC offset is 0, and
        /// for other offsets, the schedule is adjusted such that the resulting local datetime
        /// is the same as the datetime in UTC when using the unadjusted schedule.
        /// See <see cref="LiveOpsEventScheduleOccasion.GetUtcOccasionAdjustedForPlayer"/>.
        /// </para>
        /// </summary>
        [MetaMember(4)]
        public LiveOpsEventScheduleOccasion UtcScheduleOccasionMaybe { get; private set; }
        [MetaMember(5)] public LiveOpsEventParams EventParams { get; private set; }

        LiveOpsEventOccurrence() { }

        public LiveOpsEventOccurrence(
            MetaGuid occurrenceId,
            int editVersion,
            bool isArchived,
            MetaGuid definingSpecId,
            MetaScheduleTimeMode scheduleTimeMode,
            LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe,
            LiveOpsEventParams eventParams)
        {
            OccurrenceId             = occurrenceId;
            EditVersion              = editVersion;
            IsArchived               = isArchived;
            DefiningSpecId           = definingSpecId;
            ScheduleTimeMode         = scheduleTimeMode;
            UtcScheduleOccasionMaybe = utcScheduleOccasionMaybe;
            EventParams              = eventParams ?? throw new ArgumentNullException(nameof(eventParams));
        }
    }

    public static class LiveOpsEventServerUtil
    {
        /// <remark>
        /// See <see cref="GetOccasionWithPhasesStretchedByLocalOffsets"/> for a description of how this behaves when <paramref name="scheduleTimeMode"/> is <see cref="MetaScheduleTimeMode.Local"/>.
        /// </remark>
        public static LiveOpsEventPhase GetCurrentPhase(MetaScheduleTimeMode scheduleTimeMode, LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe, MetaTime currentTime)
        {
            if (utcScheduleOccasionMaybe == null)
                return LiveOpsEventPhase.NormalActive;

            LiveOpsEventScheduleOccasion effectiveOccasion;
            if (scheduleTimeMode == MetaScheduleTimeMode.Local)
                effectiveOccasion = GetOccasionWithPhasesStretchedByLocalOffsets(utcScheduleOccasionMaybe);
            else
                effectiveOccasion = utcScheduleOccasionMaybe;

            return effectiveOccasion.GetPhaseAtTime(currentTime);
        }

        /// <remark>
        /// See <see cref="GetOccasionWithPhasesStretchedByLocalOffsets"/> for a description of how this behaves when <paramref name="scheduleTimeMode"/> is <see cref="MetaScheduleTimeMode.Local"/>.
        /// </remark>
        public static (LiveOpsEventPhase NextPhase, MetaTime NextPhaseTime)? TryGetNextPhaseAndStartTime(MetaScheduleTimeMode scheduleTimeMode, LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe, MetaTime currentTime)
        {
            if (utcScheduleOccasionMaybe == null)
                return null;

            LiveOpsEventScheduleOccasion effectiveOccasion;
            if (scheduleTimeMode == MetaScheduleTimeMode.Local)
                effectiveOccasion = GetOccasionWithPhasesStretchedByLocalOffsets(utcScheduleOccasionMaybe);
            else
                effectiveOccasion = utcScheduleOccasionMaybe;

            return TryGetNextPhaseAndStartTime(effectiveOccasion, currentTime);
        }

        static (LiveOpsEventPhase NextPhase, MetaTime NextPhaseTime)? TryGetNextPhaseAndStartTime(LiveOpsEventScheduleOccasion occasion, MetaTime currentTime)
        {
            LiveOpsEventPhase currentPhase = occasion.GetPhaseAtTime(currentTime);

            LiveOpsEventPhase nextPhase;
            if (currentPhase == LiveOpsEventPhase.NotStartedYet)
                nextPhase = occasion.PhaseSequence.First().Key;
            else
            {
                IEnumerator<LiveOpsEventPhase> enumerator = occasion.PhaseSequence.Keys.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current == currentPhase)
                        break;
                }
                bool hasNextPhase = enumerator.MoveNext();
                if (!hasNextPhase)
                    return null;
                nextPhase = enumerator.Current;
            }

            return (nextPhase, occasion.PhaseSequence[nextPhase]);
        }

        /// <summary>
        /// Create a modified schedule by stretching the active phase of the given schedule,
        /// for the purposes of best-effort reporting the "phase" of a local-time event,
        /// in contexts where there is no specific local time.
        /// <para>
        /// For local-time events, it doesn't make complete sense to report just one phase,
        /// because the event can be at different phases for different players with different times;
        /// however, for now we make do with some best-effort semantics: <br/>
        /// - If the event is active (LiveOpsEventPhase.NormalActive) in any possible offset, then report it as active. <br/>
        /// - If the event hasn't yet become active anywhere, then report the most-advanced phase. <br/>
        /// - If the event is no longer active anywhere, then report the least-advanced phase. <br/>
        /// We achieve this by modifying the UTC-zone schedule by shifting the pre-active phase transitions
        /// according to the maximum utc offset (most advanced time), and the post-active phase transitions
        /// according to the minimum utc offset (least advanced time).
        /// We thus "stretch" the active phase by `MaximumUtcOffset - MinimumUtcOffset`.
        /// Then using this stretched schedule, we calculate the phase as if it was a normal global-time schedule.
        /// </para>
        /// </summary>
        public static LiveOpsEventScheduleOccasion GetOccasionWithPhasesStretchedByLocalOffsets(LiveOpsEventScheduleOccasion utcScheduleOccasion)
        {
            OrderedDictionary<LiveOpsEventPhase, MetaTime> stretchedOccasionPhaseSequence = new OrderedDictionary<LiveOpsEventPhase, MetaTime>(capacity: utcScheduleOccasion.PhaseSequence.Count);
            foreach ((LiveOpsEventPhase phase, MetaTime phaseStartTime) in utcScheduleOccasion.PhaseSequence)
            {
                MetaTime shiftedStartTime;

                if (LiveOpsEventPhase.PhasePrecedes(phase, LiveOpsEventPhase.NormalActive) || phase == LiveOpsEventPhase.NormalActive)
                    shiftedStartTime = phaseStartTime - PlayerTimeZoneInfo.MaximumUtcOffset;
                else
                    shiftedStartTime = phaseStartTime - PlayerTimeZoneInfo.MinimumUtcOffset;

                stretchedOccasionPhaseSequence.Add(phase, shiftedStartTime);
            }

            return new LiveOpsEventScheduleOccasion(stretchedOccasionPhaseSequence);
        }
    }

    public class LiveOpsEventsEnabledCondition : MetaplayFeatureEnabledConditionAttribute
    {
        public override bool IsEnabled => LiveOpsEventTypeRegistry.EventTypes.Any();
    }
}
