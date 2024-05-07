// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Akka.Actor;
using Metaplay.Core;
using Metaplay.Core.Config;
using Metaplay.Core.LiveOpsEvent;
using Metaplay.Core.Model;
using Metaplay.Core.Player;
using Metaplay.Core.Schedule;
using Metaplay.Server.LiveOpsEvent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using static Metaplay.Server.AdminApi.Controllers.Exceptions;

namespace Metaplay.Server.AdminApi.Controllers
{
    [LiveOpsEventsEnabledCondition]
    public class LiveOpsEventController : GameAdminApiController
    {
        public LiveOpsEventController(ILogger<LiveOpsEventController> logger, IActorRef adminApi) : base(logger, adminApi)
        {
        }

        // \todo #liveops-event Audit log

        #region Some common types

        public enum LiveOpsEventPhase
        {
            NotYetStarted,
            InPreview,
            Active,
            EndingSoon,
            InReview,
            Ended,
        }

        static bool IsUpcomingPhase(LiveOpsEventPhase phase)
        {
            return phase == LiveOpsEventPhase.NotYetStarted
                || phase == LiveOpsEventPhase.InPreview;
        }

        [MetaSerializable, MetaAllowNoSerializedMembers]
        public class LiveOpsEventScheduleInfo
        {
            public bool IsPlayerLocalTime;
            public MetaCalendarPeriod PreviewDuration;
            /// <summary>
            /// <para>
            /// When this is sent in a request to the api server:<br/>
            /// If <see cref="IsPlayerLocalTime"/> is false, this time is represents a universal point in time;
            /// specifically, UTC time is acquired by subtracting the offset from the datetime.<br/>
            /// If <see cref="IsPlayerLocalTime"/> is true, the offset part in this time is ignored and the
            /// datetime part is taken as the local time.<br/>
            /// See <see cref="CreateMetaSchedule"/>.
            /// </para>
            /// <para>
            /// When this is returned in a response by the api server, the offset part is always 0.<br/>
            /// If <see cref="IsPlayerLocalTime"/> is false, this represents a universal point in time.<br/>
            /// If <see cref="IsPlayerLocalTime"/> is true, the datetime can be understood as the local time;
            /// in other words, the datetimeoffset  represents the point in time when the time occurs for players
            /// with UTC+0 time.<br/>
            /// See <see cref="TryCreateScheduleInfo"/>
            /// </para>
            /// <para>
            /// Background for the above:
            /// The dashboard sends this time always with datetime plus offset notation, the offset according
            /// the browser's local offset. As such, the interpretation of that time must depend on <see cref="IsPlayerLocalTime"/>.
            /// When specifying a UTC (global) schedule, we want to interpret the time (adjusted according to the offset)
            /// as the universal point in time; note that the offset is lost (and is irrelevant) after adjusting to UTC.
            /// When specifying a local schedule, however, we want to interpret the time as a local time using the
            /// datetime part literally without adjustment, and simply ignoring the offset.
            /// </para>
            /// </summary>
            public DateTimeOffset EnabledStartTime;
            public MetaCalendarPeriod EndingSoonDuration;
            /// <summary> Behaves the same way as <see cref="EnabledStartTime"/> regarding <see cref="IsPlayerLocalTime"/>. </summary>
            public DateTimeOffset EnabledEndTime;
            public MetaCalendarPeriod ReviewDuration;
        }

        #endregion

        #region Get list of events

        [HttpGet("liveOpsEvents")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsView)]
        public async Task<ActionResult<GetLiveOpsEventsListApiResult>> GetLiveOpsEventsList([FromQuery] bool includeArchived = true)
        {
            GetLiveOpsEventsResponse events = await AskEntityAsync<GetLiveOpsEventsResponse>(GlobalStateManager.EntityId, new GetLiveOpsEventsRequest(includeArchived: includeArchived));

            OrderedDictionary<MetaGuid, LiveOpsEventSpec> specs = events.Specs.ToOrderedDictionary(spec => spec.SpecId);

            MetaTime currentTime = MetaTime.Now;

            List<LiveOpsEventBriefInfo> infos =
                events.Occurrences
                .Select(occurrence => CreateEventBriefInfo(occurrence, specs[occurrence.DefiningSpecId], currentTime))
                .ToList();

            return new GetLiveOpsEventsListApiResult
            {
                UpcomingEvents       = infos.Where(info => IsUpcomingPhase(info.CurrentPhase)).ToList(),
                OngoingAndPastEvents = infos.Where(info => !IsUpcomingPhase(info.CurrentPhase)).ToList(),
            };
        }

        public class GetLiveOpsEventsListApiResult
        {
            // \todo Error per event, in case an individual event fails to deserialize
            public List<LiveOpsEventBriefInfo> UpcomingEvents;
            public List<LiveOpsEventBriefInfo> OngoingAndPastEvents;
        }

        public class LiveOpsEventBriefInfo
        {
            public MetaGuid EventId;

            public bool IsArchived;
            public bool IsForceDisabled;
            public MetaTime CreatedAt;
            public string EventTypeName;
            public string DisplayName;
            public string Description;
            public int SequenceNumber;
            public List<string> Tags;
            public LiveOpsEventTemplateId TemplateId;
            public LiveOpsEventScheduleInfo Schedule;
            public LiveOpsEventPhase CurrentPhase;
            public LiveOpsEventPhase? NextPhase;
            public MetaTime? NextPhaseTime;
        }

        public static LiveOpsEventBriefInfo CreateEventBriefInfo(LiveOpsEventOccurrence occurrence, LiveOpsEventSpec spec, MetaTime currentTime)
        {
            (LiveOpsEventPhase NextPhase, MetaTime NextPhaseTime)? nextPhaseInfo = TryGetNextPhaseInfo(occurrence.ScheduleTimeMode, occurrence.UtcScheduleOccasionMaybe, currentTime);

            return new LiveOpsEventBriefInfo
            {
                EventId = occurrence.OccurrenceId,

                IsArchived      = occurrence.IsArchived,
                IsForceDisabled = false, // \todo #liveops-event Implement
                CreatedAt       = spec.CreatedAt,
                EventTypeName   = occurrence.EventParams.Content.GetType().Name, // \todo: replace with type name from registry
                DisplayName     = occurrence.EventParams.DisplayName,
                Description     = occurrence.EventParams.Description,
                SequenceNumber  = -1,                 // \todo #liveops-event Implement
                Tags            = new List<string>(), // \todo #liveops-event Implement
                TemplateId      = occurrence.EventParams.TemplateIdMaybe,
                Schedule        = TryCreateScheduleInfo(occurrence.ScheduleTimeMode, occurrence.UtcScheduleOccasionMaybe),
                CurrentPhase    = GetCurrentPhase(occurrence.ScheduleTimeMode, occurrence.UtcScheduleOccasionMaybe, currentTime),
                NextPhase       = nextPhaseInfo?.NextPhase,
                NextPhaseTime   = nextPhaseInfo?.NextPhaseTime,
            };
        }

        static LiveOpsEventScheduleInfo TryCreateScheduleInfo(MetaScheduleTimeMode scheduleTimeMode, LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe)
        {
            if (utcScheduleOccasionMaybe == null)
                return null;

            LiveOpsEventScheduleOccasion occasion = utcScheduleOccasionMaybe;

            MetaTime enabledStartTime   = occasion.GetEnabledStartTime();
            MetaTime enabledEndTime     = occasion.GetEnabledEndTime();

            // \todo Store as periods instead of times in underlying persistent data, instead of converting here?
            //       Consider the implications when dealing with occasions vs specs (relevant for recurring events),
            //       where the the non-constant components (month, year) can only be unambiguously converted when
            //       dealing with a specific occurrence.

            MetaDuration previewDuration;
            if (occasion.PhaseSequence.TryGetValue(Core.LiveOpsEvent.LiveOpsEventPhase.Preview, out MetaTime previewStartTime))
                previewDuration = enabledStartTime - previewStartTime;
            else
                previewDuration = MetaDuration.Zero;

            MetaDuration endingSoonDuration;
            if (occasion.PhaseSequence.TryGetValue(Core.LiveOpsEvent.LiveOpsEventPhase.EndingSoon, out MetaTime endingSoonStartTime))
                endingSoonDuration = enabledEndTime - endingSoonStartTime;
            else
                endingSoonDuration = MetaDuration.Zero;

            MetaDuration reviewDuration;
            if (occasion.PhaseSequence.TryGetValue(Core.LiveOpsEvent.LiveOpsEventPhase.Review, out MetaTime reviewStartTime))
                reviewDuration = occasion.PhaseSequence[Core.LiveOpsEvent.LiveOpsEventPhase.Disappeared] - reviewStartTime;
            else
                reviewDuration = MetaDuration.Zero;

            return new LiveOpsEventScheduleInfo
            {
                IsPlayerLocalTime = scheduleTimeMode == MetaScheduleTimeMode.Local,
                PreviewDuration = DurationToConstantDurationPeriod(previewDuration),
                EnabledStartTime = new DateTimeOffset(enabledStartTime.ToDateTime(), TimeSpan.Zero),
                EndingSoonDuration = DurationToConstantDurationPeriod(endingSoonDuration),
                EnabledEndTime = new DateTimeOffset(enabledEndTime.ToDateTime(), TimeSpan.Zero),
                ReviewDuration = DurationToConstantDurationPeriod(reviewDuration),
            };
        }

        /// <summary>
        /// Convert the given duration to a period which only uses constant-duration components.
        /// Days is the biggest possible non-zero unit in the returned period. As a consequence, the number of days may be greater than in any month.
        /// </summary>
        /// <remarks>
        /// Truncates away the sub-second part, as it is currently not supported by <see cref="MetaCalendarPeriod"/>.
        /// </remarks>
        static MetaCalendarPeriod DurationToConstantDurationPeriod(MetaDuration duration)
        {
            TimeSpan timeSpan = duration.ToTimeSpan();
            return new MetaCalendarPeriod
            {
                Years = 0,
                Months = 0,

                Days = timeSpan.Days,
                Hours = timeSpan.Hours,
                Minutes = timeSpan.Minutes,
                Seconds = timeSpan.Seconds,
            };
        }

        static LiveOpsEventPhase GetCurrentPhase(MetaScheduleTimeMode scheduleTimeMode, LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe, MetaTime currentTime)
        {
            Core.LiveOpsEvent.LiveOpsEventPhase phase = LiveOpsEventServerUtil.GetCurrentPhase(scheduleTimeMode, utcScheduleOccasionMaybe, currentTime);
            return ConvertEventPhase(phase);
        }

        static (LiveOpsEventPhase NextPhase, MetaTime NextPhaseTime)? TryGetNextPhaseInfo(MetaScheduleTimeMode scheduleTimeMode, LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe, MetaTime currentTime)
        {
            (Core.LiveOpsEvent.LiveOpsEventPhase Phase, MetaTime NextPhaseTime)? info = LiveOpsEventServerUtil.TryGetNextPhaseAndStartTime(scheduleTimeMode, utcScheduleOccasionMaybe, currentTime);
            if (!info.HasValue)
                return null;

            return (ConvertEventPhase(info.Value.Phase), info.Value.NextPhaseTime);
        }

        static LiveOpsEventPhase ConvertEventPhase(Core.LiveOpsEvent.LiveOpsEventPhase phase)
        {
            if (phase == Core.LiveOpsEvent.LiveOpsEventPhase.NotStartedYet)
                return LiveOpsEventPhase.NotYetStarted;
            else if (phase == Core.LiveOpsEvent.LiveOpsEventPhase.Preview)
                return LiveOpsEventPhase.InPreview;
            else if (phase == Core.LiveOpsEvent.LiveOpsEventPhase.NormalActive)
                return LiveOpsEventPhase.Active;
            else if (phase == Core.LiveOpsEvent.LiveOpsEventPhase.EndingSoon)
                return LiveOpsEventPhase.EndingSoon;
            else if (phase == Core.LiveOpsEvent.LiveOpsEventPhase.Review)
                return LiveOpsEventPhase.InReview;
            else if (phase == Core.LiveOpsEvent.LiveOpsEventPhase.Disappeared)
                return LiveOpsEventPhase.Ended;
            else
                return LiveOpsEventPhase.Ended;
        }

        #endregion

        #region Get event details

        [HttpGet("liveOpsEvent/{liveOpsEventIdStr}")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsView)]
        public async Task<ActionResult<LiveOpsEventDetailsInfo>> GetLiveOpsEventDetails(string liveOpsEventIdStr)
        {
            MetaGuid occurrenceId = MetaGuid.Parse(liveOpsEventIdStr);

            GetLiveOpsEventResponse eventInfo = await AskEntityAsync<GetLiveOpsEventResponse>(GlobalStateManager.EntityId, new GetLiveOpsEventRequest(occurrenceId: occurrenceId));

            LiveOpsEventOccurrence requestedOccurrence = eventInfo.Occurrence;
            List<LiveOpsEventOccurrence> relatedOccurrences = eventInfo.RelatedOccurrences;
            OrderedDictionary<MetaGuid, LiveOpsEventSpec> specs = eventInfo.Specs.ToOrderedDictionary(spec => spec.SpecId);

            MetaTime currentTime = MetaTime.Now;

            (LiveOpsEventPhase NextPhase, MetaTime NextPhaseTime)? nextPhaseInfo = TryGetNextPhaseInfo(requestedOccurrence.ScheduleTimeMode, requestedOccurrence.UtcScheduleOccasionMaybe, currentTime);

            return new LiveOpsEventDetailsInfo
            {
                EventId = requestedOccurrence.OccurrenceId,
                EventParams = EditableParamsFromOccurrence(requestedOccurrence),
                IsArchived = requestedOccurrence.IsArchived,
                IsForceDisabled = false, // \todo #liveops-event Implement
                CreatedAt = specs[requestedOccurrence.DefiningSpecId].CreatedAt,
                SequenceNumber = -1, // \todo #liveops-event Implement
                Tags = new List<string>(), // \todo #liveops-event Implement
                CurrentPhase = GetCurrentPhase(requestedOccurrence.ScheduleTimeMode, requestedOccurrence.UtcScheduleOccasionMaybe, currentTime),
                NextPhase = nextPhaseInfo?.NextPhase,
                NextPhaseTime = nextPhaseInfo?.NextPhaseTime,
                RelatedEvents =
                    relatedOccurrences
                    .Select(occurrence => CreateEventBriefInfo(occurrence, specs[occurrence.DefiningSpecId], currentTime))
                    .ToList(),
            };
        }

        static EditableEventParams EditableParamsFromOccurrence(LiveOpsEventOccurrence occurrence)
        {
            return new EditableEventParams()
            {
                EventType       = occurrence.EventParams.Content.GetType(),
                DisplayName     = occurrence.EventParams.DisplayName,
                Description     = occurrence.EventParams.Description,
                TemplateId      = occurrence.EventParams.TemplateIdMaybe,
                Schedule        = TryCreateScheduleInfo(occurrence.ScheduleTimeMode, occurrence.UtcScheduleOccasionMaybe),
                TargetPlayers   = occurrence.EventParams.TargetPlayersMaybe,
                TargetCondition = occurrence.EventParams.TargetConditionMaybe,
                Content         = occurrence.EventParams.Content,
            };
        }

        public class LiveOpsEventDetailsInfo
        {
            public MetaGuid EventId;
            public EditableEventParams EventParams;

            public bool IsArchived;
            public bool IsForceDisabled;
            public MetaTime CreatedAt; // \todo Separate "originally created at" and "imported at"? List of "updated at" times, or is that a job for the audit log?

            // Sequence number among similar events, starting from 1 (or 0?), so we can say "WeekendHappyHour #1", "WeekendHappyHour #2", etc.
            // This becomes fixed when the event starts, based on how many similar events have happened before it.
            // Before the event has started, this is a "tentative" number and can still change if scheduling is changed.
            public int SequenceNumber;
            // Arbitrary user-defined tags for searching etc.
            public List<string> Tags;
            public LiveOpsEventPhase CurrentPhase;
            public LiveOpsEventPhase? NextPhase;
            public MetaTime? NextPhaseTime;
            public List<LiveOpsEventBriefInfo> RelatedEvents;

            // \todo #liveops-event Show somewhere: Audience estimate, participation counts, etc.
        }

        #endregion

        #region Create new event

        [HttpPost("createLiveOpsEvent")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsEdit)]
        public async Task<ActionResult<CreateLiveOpsEventApiResult>> CreateLiveOpsEvent()
        {
            CreateLiveOpsEventApiBody body = await ParseBodyAsync<CreateLiveOpsEventApiBody>();

            bool settingsOk = TryCreateEventSettings(body.Parameters, out LiveOpsEventSettings eventSettings, out List<LiveOpsEventCreationDiagnostic> diagnostics);
            if (!settingsOk)
            {
                return new CreateLiveOpsEventApiResult
                {
                    IsValid = false,
                    EventId = null,
                    Diagnostics = diagnostics,
                };
            }

            CreateLiveOpsEventResponse response = await AskEntityAsync<CreateLiveOpsEventResponse>(GlobalStateManager.EntityId, new CreateLiveOpsEventRequest(
                validateOnly: body.ValidateOnly,
                eventSettings));

            OrderedDictionary<MetaGuid, LiveOpsEventSpec> specs = response.Specs.ToOrderedDictionary(spec => spec.SpecId);

            MetaTime currentTime = MetaTime.Now;

            return new CreateLiveOpsEventApiResult
            {
                IsValid = response.IsValid,
                EventId = response.InitialEventOccurrenceId,
                RelatedEvents = response.RelatedOccurrences.Select(occurrence => CreateEventBriefInfo(occurrence, specs[occurrence.DefiningSpecId], currentTime)).ToList(),
                Diagnostics = response.Diagnostics.Select(diagnostic => CreateEventCreationDiagnostic(diagnostic)).ToList(),
            };
        }

        public class CreateLiveOpsEventApiBody
        {
            [JsonProperty(Required = Required.Always)] public bool                ValidateOnly;
            public                                            EditableEventParams Parameters;
        }

        public class CreateLiveOpsEventApiResult
        {
            public bool IsValid;
            public MetaGuid? EventId;
            public List<LiveOpsEventBriefInfo> RelatedEvents;
            public List<LiveOpsEventCreationDiagnostic> Diagnostics;
        }

        [MetaSerializable, MetaAllowNoSerializedMembers]
        public struct EditableEventParams
        {
            public string DisplayName;
            public string Description;

            public Type EventType;

            public LiveOpsEventTemplateId TemplateId;
            // \note Despite direct editing of content not being in MVP (instead it just comes from template),
            //       it is still included in the creation params, because we need to support the case where the user duplicates
            //       an existing event which was created using a template that has changed since (or doesn't exist anymore).
            //       In that case we want, by default, the content to be copied as it was when the duplication source event
            //       was created, rather than how it is at the time at duplication.
            public LiveOpsEventContent Content;
            public LiveOpsEventScheduleInfo Schedule;
            public List<EntityId> TargetPlayers;
            public PlayerCondition TargetCondition;
        }

        public struct LiveOpsEventCreationDiagnostic
        {
            public enum DiagnosticLevel
            {
                Warning,
                Error,
            }

            public DiagnosticLevel Level;
            public string Message;

            public LiveOpsEventCreationDiagnostic(DiagnosticLevel level, string message)
            {
                Level = level;
                Message = message ?? "<unknown error>";
            }
        }

        static bool TryCreateEventSettings(EditableEventParams eventParams, out LiveOpsEventSettings eventSettings, out List<LiveOpsEventCreationDiagnostic> diagnostics)
        {
            bool scheduleOk;
            MetaScheduleBase metaSchedule;
            List<LiveOpsEventCreationDiagnostic> scheduleDiagnostics;
            if (eventParams.Schedule != null)
            {
                scheduleOk = TryCreateMetaSchedule(eventParams.Schedule, out metaSchedule, out scheduleDiagnostics);
            }
            else
            {
                scheduleOk = true;
                metaSchedule = null;
                scheduleDiagnostics = new List<LiveOpsEventCreationDiagnostic>();
            }

            if (!scheduleOk)
            {
                eventSettings = null;
                diagnostics = scheduleDiagnostics;
                return false;
            }

            eventSettings = new LiveOpsEventSettings(
                metaSchedule,
                CreateEventParams(eventParams));
            diagnostics = scheduleDiagnostics;
            return true;
        }

        static bool TryCreateMetaSchedule(LiveOpsEventScheduleInfo schedule, out MetaScheduleBase metaSchedule, out List<LiveOpsEventCreationDiagnostic> diagnostics)
        {
            // \todo Do nicer than plainly exception-based

            try
            {
                metaSchedule = CreateMetaSchedule(schedule);
                diagnostics = new List<LiveOpsEventCreationDiagnostic>();
                return true;
            }
            catch (Exception ex)
            {
                metaSchedule = null;
                diagnostics = new List<LiveOpsEventCreationDiagnostic>
                {
                    new LiveOpsEventCreationDiagnostic(
                        LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error,
                        $"Invalid schedule: {ex.Message}"),
                };
                return false;
            }
        }

        static MetaScheduleBase CreateMetaSchedule(LiveOpsEventScheduleInfo schedule)
        {
            DateTime utcEnabledStartTime;
            DateTime utcEnabledEndTime;

            if (schedule.IsPlayerLocalTime)
            {
                utcEnabledStartTime = DateTime.SpecifyKind(schedule.EnabledStartTime.DateTime, DateTimeKind.Utc);
                utcEnabledEndTime = DateTime.SpecifyKind(schedule.EnabledEndTime.DateTime, DateTimeKind.Utc);
            }
            else
            {
                utcEnabledStartTime = schedule.EnabledStartTime.UtcDateTime;
                utcEnabledEndTime = schedule.EnabledEndTime.UtcDateTime;
            }

            return new MetaRecurringCalendarSchedule(
                timeMode: schedule.IsPlayerLocalTime
                          ? MetaScheduleTimeMode.Local
                          : MetaScheduleTimeMode.Utc,
                start: MetaCalendarDateTime.FromDateTime(utcEnabledStartTime),
                duration: DurationToConstantDurationPeriod(MetaDuration.FromTimeSpan(utcEnabledEndTime - utcEnabledStartTime)),
                endingSoon: schedule.EndingSoonDuration,
                preview: schedule.PreviewDuration,
                review: schedule.ReviewDuration,
                recurrence: null,
                numRepeats: null);
        }

        static LiveOpsEventParams CreateEventParams(EditableEventParams eventParams)
        {
            return new LiveOpsEventParams(
                displayName: eventParams.DisplayName,
                description: eventParams.Description,
                eventParams.TargetPlayers,
                eventParams.TargetCondition,
                eventParams.TemplateId,
                eventParams.Content);
        }

        static LiveOpsEventCreationDiagnostic CreateEventCreationDiagnostic(Server.LiveOpsEventCreationDiagnostic diagnostic)
        {
            return new LiveOpsEventCreationDiagnostic
            {
                Level = ConvertEventCreationDiagnosticLevel(diagnostic.Level),
                Message = diagnostic.Message,
            };
        }

        static LiveOpsEventCreationDiagnostic.DiagnosticLevel ConvertEventCreationDiagnosticLevel(Server.LiveOpsEventCreationDiagnostic.DiagnosticLevel level)
        {
            switch (level)
            {
                case Server.LiveOpsEventCreationDiagnostic.DiagnosticLevel.Warning: return LiveOpsEventCreationDiagnostic.DiagnosticLevel.Warning;
                case Server.LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error: return LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error;
                default:
                    throw new InvalidEnumArgumentException(nameof(level), (int)level, typeof(Server.LiveOpsEventCreationDiagnostic.DiagnosticLevel));
            }
        }

        #endregion

        #region Update event

        [HttpPost("updateLiveOpsEvent")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsEdit)]
        public async Task<ActionResult<UpdateLiveOpsEventApiResult>> UpdateLiveOpsEvent()
        {
            UpdateLiveOpsEventApiBody body = await ParseBodyAsync<UpdateLiveOpsEventApiBody>();

            bool settingsOk = TryCreateEventSettings(body.Parameters, out LiveOpsEventSettings eventSettings, out List<LiveOpsEventCreationDiagnostic> diagnostics);
            if (!settingsOk)
            {
                return new UpdateLiveOpsEventApiResult
                {
                    IsValid = false,
                    Diagnostics = diagnostics,
                };
            }

            UpdateLiveOpsEventResponse response = await AskEntityAsync<UpdateLiveOpsEventResponse>(GlobalStateManager.EntityId, new UpdateLiveOpsEventRequest(
                validateOnly: body.ValidateOnly,
                occurrenceId: body.OccurrenceId,
                eventSettings));

            OrderedDictionary<MetaGuid, LiveOpsEventSpec> specs = response.Specs.ToOrderedDictionary(spec => spec.SpecId);

            MetaTime currentTime = MetaTime.Now;

            return new UpdateLiveOpsEventApiResult
            {
                IsValid = response.IsValid,
                RelatedEvents = response.RelatedOccurrences.Select(occurrence => CreateEventBriefInfo(occurrence, specs[occurrence.DefiningSpecId], currentTime)).ToList(),
                Diagnostics = response.Diagnostics.Select(diagnostic => CreateEventCreationDiagnostic(diagnostic)).ToList(),
            };
        }

        public class UpdateLiveOpsEventApiBody
        {
            [JsonProperty(Required = Required.Always)] public bool ValidateOnly;
            public MetaGuid OccurrenceId;
            public EditableEventParams Parameters;
        }

        public class UpdateLiveOpsEventApiResult
        {
            public bool IsValid;
            public List<LiveOpsEventBriefInfo> RelatedEvents;
            public List<LiveOpsEventCreationDiagnostic> Diagnostics;
        }

        #endregion

        #region Force end/disable an event

        [HttpPost("forceDisableLiveOpsEvent/{liveOpsEventIdStr}")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsEdit)]
        public async Task<ActionResult<SetLiveOpsEventForceDisabledStatusApiResult>> SetLiveOpsEventForceDisabledStatus(string liveOpsEventIdStr)
        {
            // \todo #liveops-event Implement
            _ = await ParseBodyAsync<SetLiveOpsEventForceDisabledStatusApiBody>();
            throw new NotImplementedException();
        }

        public class SetLiveOpsEventForceDisabledStatusApiBody
        {
            public bool IsForceDisabled;
        }

        public class SetLiveOpsEventForceDisabledStatusApiResult
        {
        }

        #endregion

        #region Set event's archival status

        [HttpPost("setLiveOpsEventArchivedStatus/{liveOpsEventIdStr}")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsEdit)]
        public async Task<ActionResult<SetLiveOpsEventArchivedStatusApiResult>> SetLiveOpsEventArchivedStatus(string liveOpsEventIdStr)
        {
            MetaGuid occurrenceId = MetaGuid.Parse(liveOpsEventIdStr);

            SetLiveOpsEventArchivedStatusApiBody body = await ParseBodyAsync<SetLiveOpsEventArchivedStatusApiBody>();

            SetLiveOpsEventArchivedStatusResponse response = await AskEntityAsync<SetLiveOpsEventArchivedStatusResponse>(GlobalStateManager.EntityId, new SetLiveOpsEventArchivedStatusRequest(
                occurrenceId: occurrenceId,
                isArchived: body.IsArchived));

            if (!response.IsSuccess)
                throw new MetaplayHttpException(400, "Failed to update archival status", response.Error);

            return new SetLiveOpsEventArchivedStatusApiResult();
        }

        public class SetLiveOpsEventArchivedStatusApiBody
        {
            public bool IsArchived;
        }

        public class SetLiveOpsEventArchivedStatusApiResult
        {
        }

        #endregion

        #region Get event types

        [HttpGet("liveOpsEventTypes")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsView)]
        public ActionResult GetLiveOpsEventTypes()
        {
            FullGameConfig fullGameConfig = GlobalStateProxyActor.ActiveGameConfig.Get().BaselineGameConfig;
            return Ok(LiveOpsEventTypeRegistry.EventTypes.Select(eventType =>
                new LiveOpsEventTypeInfo
                {
                    ContentClass = eventType.ContentClass,
                    EventTypeName = eventType.EventTypeName,
                    Templates = GetEventTemplatesForEventType(eventType, fullGameConfig).ToDictionary(x => x.TemplateId, x => x.ContentBase),
                }));
        }

        public struct LiveOpsEventTypeInfo
        {
            public Type ContentClass;
            public string EventTypeName;
            public Dictionary<LiveOpsEventTemplateId, LiveOpsEventContent> Templates;
            // public bool   CanBeScheduled;
            // public bool   RequiresTemplate;
        }

        static IEnumerable<ILiveOpsEventTemplate> GetEventTemplatesForEventType(EventTypeStaticInfo eventType, FullGameConfig fullGameConfig)
        {
            if (eventType.ConfigTemplateLibraryGetter == null)
                return Enumerable.Empty<ILiveOpsEventTemplate>();
            IGameConfigLibrary templateLibrary = (IGameConfigLibrary)eventType.ConfigTemplateLibraryGetter(fullGameConfig);
            return templateLibrary.EnumerateAll().Select(x => (ILiveOpsEventTemplate)x.Value);
        }

        #endregion

        #region Export a single event

        [HttpGet("exportLiveOpsEvent/{liveOpsEventIdStr}")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsView)]
        public ActionResult<ExportLiveOpsEventApiResult> ExportLiveOpsEvent(string liveOpsEventIdStr)
        {
            // \todo #liveops-event Implement
            throw new NotImplementedException();
        }

        public class ExportLiveOpsEventApiResult
        {
            public LiveOpsEventsExportImportPackage Package;
        }

        public class LiveOpsEventsExportImportPackage
        {
            // \todo #liveops-event Figure out: are both occurrences and specs included in export-import? Or just either?
            public int SchemaVersion;
            public List<LiveOpsEventExportPackage> Events;
        }

        public class LiveOpsEventExportPackage
        {
            public int SchemaVersion;
            public MetaGuid EventId;
            public string Payload;
        }

        #endregion

        #region Import events from an exported package

        [HttpPost("importLiveOpsEvents")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsEdit)]
        public async Task<ActionResult<ImportLiveOpsEventsApiResult>> ImportLiveOpsEvents()
        {
            // \todo #liveops-event Implement
            _ = await ParseBodyAsync<ImportLiveOpsEventsApiBody>();
            throw new NotImplementedException();
        }

        public class ImportLiveOpsEventsApiBody
        {
            public LiveOpsEventImportParams Parameters;
        }

        public class ImportLiveOpsEventsApiResult
        {
            public List<EventImportResult> EventResults;
        }

        public class LiveOpsEventImportParams
        {
            public ImportConflictPolicy OverwritePolicy;
            public LiveOpsEventsExportImportPackage Package;
        }

        public enum ImportConflictPolicy
        {
            Error,
            Overwrite,
            KeepOld,
            CreateNew,
        }

        public class EventImportResult
        {
            public MetaGuid EventIdInPackage;
            public bool IsSuccess;
            public bool EventIdAlreadyExists;
            public LiveOpsEventBriefInfo EventInfo; // Should it be exactly the same? Some parts, such as id, may be just "speculated" and may differ when the import is actually done.

            public string ResultDescription;
        }

        #endregion

        #region Import events from an exported package: preview

        [HttpPost("previewImportLiveOpsEvents")]
        [RequirePermission(MetaplayPermissions.ApiLiveOpsEventsEdit)]
        public async Task<ActionResult<PreviewImportLiveOpsEventsApiResult>> PreviewImportLiveOpsEvents()
        {
            // \todo #liveops-event Implement
            _ = await ParseBodyAsync<PreviewImportLiveOpsEventsApiBody>();
            throw new NotImplementedException();
        }

        public class PreviewImportLiveOpsEventsApiBody
        {
            public LiveOpsEventImportParams Parameters;
        }

        public class PreviewImportLiveOpsEventsApiResult
        {
            // \todo #liveops-event Preview event result should not be exactly the same as the actual import result?
            //       Some parts, such as resulting event id, may be just "speculated" and may differ when the import is actually done.
            public List<EventImportResult> EventResults;
        }

        #endregion

        #region Player-specific list of events

        [HttpGet("players/{playerIdStr}/liveOpsEvents")]
        [RequirePermission(MetaplayPermissions.ApiPlayersView)]
        public ActionResult<GetPlayerLiveOpsEventsApiResult> GetPlayerLiveOpsEvents(string playerIdStr)
        {
            // \todo #liveops-event Implement
            throw new NotImplementedException();
        }

        public class GetPlayerLiveOpsEventsApiResult
        {
            public List<LiveOpsEventPerPlayerInfo> Events;
        }

        public class LiveOpsEventPerPlayerInfo
        {
            public MetaGuid EventId;

            public bool IsArchived;
            public bool IsForceDisabled;
            public MetaTime CreatedAt;
            public Type EventType;
            public string DisplayName;
            public string Description;
            public int SequenceNumber;
            public List<string> Tags;
            public LiveOpsEventTemplateId TemplateId;
            // Unlike with the non-player-specific endpoints, here the schedule times are adjusted for player's local time zone, if it's a local-time event.
            public LiveOpsEventScheduleInfo Schedule;
            public LiveOpsEventPhase CurrentPhase;
            public LiveOpsEventPhase? NextPhase;
            public MetaTime? NextPhaseTime;

            // \todo #liveops-event Separate eligibility bool like this, or report "NotEligible" in CurrentPhase?
            public bool IsEligible;
            public PlayerLiveOpsEventModel PlayerState;
        }

        #endregion
    }
}
