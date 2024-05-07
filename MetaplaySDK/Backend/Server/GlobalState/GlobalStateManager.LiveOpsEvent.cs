// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Cloud.Entity;
using Metaplay.Core;
using Metaplay.Core.LiveOpsEvent;
using Metaplay.Core.Model;
using Metaplay.Core.Player;
using Metaplay.Core.Schedule;
using Metaplay.Core.TypeCodes;
using Metaplay.Server.LiveOpsEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Metaplay.Server
{
    [MetaSerializable]
    public class LiveOpsEventsGlobalState
    {
        // \todo #liveops-event Tolerate individual events being broken and non-deserializable.
        //       Consider what kind of state storage is suitable for this.
        [MetaMember(1)] public OrderedDictionary<MetaGuid, LiveOpsEventSpec> EventSpecs { get; private set; } = new();
        [MetaMember(2)] public OrderedDictionary<MetaGuid, LiveOpsEventOccurrence> EventOccurrences { get; private set; } = new();
    }

    public class ActiveLiveOpsEventSet : IAtomicValue<ActiveLiveOpsEventSet>
    {
        public IReadOnlyList<LiveOpsEventOccurrence> LiveOpsEventOccurrences { get; }

        public ActiveLiveOpsEventSet() { }

        public ActiveLiveOpsEventSet(IEnumerable<LiveOpsEventOccurrence> liveOpsEventOccurrences)
        {
            LiveOpsEventOccurrences = new List<LiveOpsEventOccurrence>(liveOpsEventOccurrences);
        }

        public bool Equals(ActiveLiveOpsEventSet other)
        {
            if (other is null) return false;

            return LiveOpsEventOccurrences.SequenceEqual(other.LiveOpsEventOccurrences);
        }

        public override bool Equals(object obj) => obj is ActiveLiveOpsEventSet other && Equals(other);

        public override int GetHashCode()
        {
            return LiveOpsEventOccurrences != null ? LiveOpsEventOccurrences.GetHashCode() : 0;
        }
    }

    [MetaMessage(MessageCodesCore.CreateLiveOpsEventRequest, MessageDirection.ServerInternal)]
    public class CreateLiveOpsEventRequest : MetaMessage
    {
        public bool ValidateOnly { get; private set; }
        public LiveOpsEventSettings Settings { get; private set; }

        CreateLiveOpsEventRequest() { }
        public CreateLiveOpsEventRequest(bool validateOnly, LiveOpsEventSettings settings)
        {
            ValidateOnly = validateOnly;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
    }
    [MetaMessage(MessageCodesCore.CreateLiveOpsEventResponse, MessageDirection.ServerInternal)]
    public class CreateLiveOpsEventResponse : MetaMessage
    {
        public bool IsValid { get; private set; }
        public List<LiveOpsEventCreationDiagnostic> Diagnostics { get; private set; }
        // \note EventSpecId and InitialEventOccurrenceId are null if IsValid is false, or request's ValidateOnly was true.
        public MetaGuid? EventSpecId { get; private set; }
        public MetaGuid? InitialEventOccurrenceId { get; private set; }
        public List<LiveOpsEventOccurrence> RelatedOccurrences { get; private set; }
        public List<LiveOpsEventSpec> Specs { get; private set; }

        public static CreateLiveOpsEventResponse CreateValid(
            List<LiveOpsEventCreationDiagnostic> diagnostics,
            MetaGuid? eventSpecId,
            MetaGuid? initialEventOccurrenceId,
            List<LiveOpsEventOccurrence> relatedOccurrences,
            List<LiveOpsEventSpec> specs)
            => new CreateLiveOpsEventResponse
            {
                IsValid = true,
                Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)),
                EventSpecId = eventSpecId,
                InitialEventOccurrenceId = initialEventOccurrenceId,
                RelatedOccurrences = relatedOccurrences ?? throw new ArgumentNullException(nameof(relatedOccurrences)),
                Specs = specs ?? throw new ArgumentNullException(nameof(specs)),
            };

        public static CreateLiveOpsEventResponse CreateInvalid(
            List<LiveOpsEventCreationDiagnostic> diagnostics,
            List<LiveOpsEventOccurrence> relatedOccurrences,
            List<LiveOpsEventSpec> specs)
            => new CreateLiveOpsEventResponse
            {
                IsValid = false,
                Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)),
                RelatedOccurrences = relatedOccurrences ?? throw new ArgumentNullException(nameof(relatedOccurrences)),
                Specs = specs ?? throw new ArgumentNullException(nameof(specs)),
            };
    }

    [MetaSerializable]
    public struct LiveOpsEventCreationDiagnostic
    {
        [MetaSerializable]
        public enum DiagnosticLevel
        {
            Warning,
            Error,
        }

        [MetaMember(1)] public DiagnosticLevel Level { get; private set; }
        [MetaMember(2)] public string Message { get; private set; }

        public LiveOpsEventCreationDiagnostic(DiagnosticLevel level, string message)
        {
            Level = level;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }

    [MetaMessage(MessageCodesCore.UpdateLiveOpsEventRequest, MessageDirection.ServerInternal)]
    public class UpdateLiveOpsEventRequest : MetaMessage
    {
        public bool ValidateOnly { get; private set; }
        public MetaGuid OccurrenceId { get; private set; } // \todo Should we use occurrence id or spec id?
        public LiveOpsEventSettings Settings { get; private set; }

        UpdateLiveOpsEventRequest() { }
        public UpdateLiveOpsEventRequest(bool validateOnly, MetaGuid occurrenceId, LiveOpsEventSettings settings)
        {
            ValidateOnly = validateOnly;
            OccurrenceId = occurrenceId;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
    }
    [MetaMessage(MessageCodesCore.UpdateLiveOpsEventResponse, MessageDirection.ServerInternal)]
    public class UpdateLiveOpsEventResponse : MetaMessage
    {
        public bool IsValid { get; private set; }
        // \todo Spec id?
        // \todo Ids of occurrences that were updated due to being defined by the same spec?
        public List<LiveOpsEventCreationDiagnostic> Diagnostics { get; private set; }
        public List<LiveOpsEventOccurrence> RelatedOccurrences { get; private set; }
        public List<LiveOpsEventSpec> Specs { get; private set; }

        UpdateLiveOpsEventResponse() { }
        public UpdateLiveOpsEventResponse(
            bool isValid,
            List<LiveOpsEventCreationDiagnostic> diagnostics,
            List<LiveOpsEventOccurrence> relatedOccurrences,
            List<LiveOpsEventSpec> specs)
        {
            IsValid = isValid;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            RelatedOccurrences = relatedOccurrences ?? throw new ArgumentNullException(nameof(relatedOccurrences));
            Specs = specs ?? throw new ArgumentNullException(nameof(specs));
        }
    }

    [MetaMessage(MessageCodesCore.SetLiveOpsEventArchivedStatusRequest, MessageDirection.ServerInternal)]
    public class SetLiveOpsEventArchivedStatusRequest : MetaMessage
    {
        public MetaGuid OccurrenceId { get; private set; }
        public bool IsArchived { get; private set; }

        SetLiveOpsEventArchivedStatusRequest() { }
        public SetLiveOpsEventArchivedStatusRequest(MetaGuid occurrenceId, bool isArchived)
        {
            OccurrenceId = occurrenceId;
            IsArchived = isArchived;
        }
    }
    [MetaMessage(MessageCodesCore.SetLiveOpsEventArchivedStatusResponse, MessageDirection.ServerInternal)]
    public class SetLiveOpsEventArchivedStatusResponse : MetaMessage
    {
        public bool IsSuccess { get; private set; }
        public string Error { get; private set; }

        public static SetLiveOpsEventArchivedStatusResponse CreateSuccess()
            => new SetLiveOpsEventArchivedStatusResponse { IsSuccess = true, Error = null };

        public static SetLiveOpsEventArchivedStatusResponse CreateFailure(string error)
            => new SetLiveOpsEventArchivedStatusResponse { IsSuccess = false, Error = error ?? throw new ArgumentNullException(nameof(error)) };
    }

    [MetaMessage(MessageCodesCore.GetLiveOpsEventsRequest, MessageDirection.ServerInternal)]
    public class GetLiveOpsEventsRequest : MetaMessage
    {
        public bool IncludeArchived { get; private set; }

        GetLiveOpsEventsRequest() { }
        public GetLiveOpsEventsRequest(bool includeArchived)
        {
            IncludeArchived = includeArchived;
        }
    }
    [MetaMessage(MessageCodesCore.GetLiveOpsEventsResponse, MessageDirection.ServerInternal)]
    public class GetLiveOpsEventsResponse : MetaMessage
    {
        public List<LiveOpsEventOccurrence> Occurrences { get; private set; }
        public List<LiveOpsEventSpec> Specs { get; private set; }

        GetLiveOpsEventsResponse() { }
        public GetLiveOpsEventsResponse(List<LiveOpsEventOccurrence> occurrences, List<LiveOpsEventSpec> specs)
        {
            Occurrences = occurrences ?? throw new ArgumentNullException(nameof(occurrences));
            Specs = specs ?? throw new ArgumentNullException(nameof(specs));
        }
    }

    [MetaMessage(MessageCodesCore.GetLiveOpsEventRequest, MessageDirection.ServerInternal)]
    public class GetLiveOpsEventRequest : MetaMessage
    {
        public MetaGuid OccurrenceId { get; private set; }

        GetLiveOpsEventRequest() { }
        public GetLiveOpsEventRequest(MetaGuid occurrenceId)
        {
            OccurrenceId = occurrenceId;
        }
    }
    [MetaMessage(MessageCodesCore.GetLiveOpsEventResponse, MessageDirection.ServerInternal)]
    public class GetLiveOpsEventResponse : MetaMessage
    {
        public LiveOpsEventOccurrence Occurrence { get; private set; }
        public List<LiveOpsEventOccurrence> RelatedOccurrences { get; private set; }
        public List<LiveOpsEventSpec> Specs { get; private set; }

        GetLiveOpsEventResponse() { }
        public GetLiveOpsEventResponse(LiveOpsEventOccurrence occurrence, List<LiveOpsEventOccurrence> relatedOccurrences, List<LiveOpsEventSpec> specs)
        {
            Occurrence = occurrence ?? throw new ArgumentNullException(nameof(occurrence));
            RelatedOccurrences = relatedOccurrences ?? throw new ArgumentNullException(nameof(relatedOccurrences));
            Specs = specs ?? throw new ArgumentNullException(nameof(specs));
        }
    }

    [MetaMessage(MessageCodesCore.CreateLiveOpsEventMessage, MessageDirection.ServerInternal)]
    public class CreateLiveOpsEventMessage : MetaMessage
    {
        public LiveOpsEventOccurrence Occurrence { get; private set; }
        public LiveOpsEventSpec       Spec { get; private set; }

        CreateLiveOpsEventMessage() { }
        public CreateLiveOpsEventMessage(LiveOpsEventOccurrence occurrence, LiveOpsEventSpec spec)
        {
            Occurrence = occurrence ?? throw new ArgumentNullException(nameof(occurrence));
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }
    }

    [MetaMessage(MessageCodesCore.UpdateLiveOpsEventMessage, MessageDirection.ServerInternal)]
    public class UpdateLiveOpsEventMessage : MetaMessage
    {
        public LiveOpsEventOccurrence Occurrence { get; private set; }
        public LiveOpsEventSpec       Spec { get; private set; }

        UpdateLiveOpsEventMessage() { }
        public UpdateLiveOpsEventMessage(LiveOpsEventOccurrence occurrence, LiveOpsEventSpec spec)
        {
            Occurrence = occurrence ?? throw new ArgumentNullException(nameof(occurrence));
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }
    }

    // \todo #liveops-event Need error tolerance. Limit the blast radius of per-event failures.

    public abstract partial class GlobalStateManagerBase<TGlobalState>
    {
        [EntityAskHandler]
        async Task<CreateLiveOpsEventResponse> HandleCreateLiveOpsEventRequestAsync(CreateLiveOpsEventRequest request)
        {
            LiveOpsEventSettings settings = request.Settings;

            MetaTime currentTime = MetaTime.Now;

            List<LiveOpsEventCreationDiagnostic> diagnostics = ValidateEventSettings(settings, currentTime);

            List<LiveOpsEventOccurrence> relatedOccurrences = GetRelatedOccurrences(settings.EventParams.Content);
            List<LiveOpsEventSpec> specs = GetSpecsForOccurrences(relatedOccurrences);

            // \todo #liveops-event Option/parameter for "treat warnings as errors"?
            if (diagnostics.Any(diag => diag.Level == LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error))
            {
                return CreateLiveOpsEventResponse.CreateInvalid(diagnostics, relatedOccurrences, specs);
            }

            if (request.ValidateOnly)
            {
                return CreateLiveOpsEventResponse.CreateValid(diagnostics, eventSpecId: null, initialEventOccurrenceId: null, relatedOccurrences, specs);
            }

            LiveOpsEventSpec spec = new LiveOpsEventSpec(
                specId: MetaGuid.NewWithTime(currentTime.ToDateTime()),
                editVersion: 0,
                isArchived: false,
                settings,
                createdAt: currentTime);

            LiveOpsEventOccurrence occurrence;
            {
                (MetaScheduleTimeMode timeMode, LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe) = CreateSingleOccasionScheduleAssumeNoRecurrence(settings);

                occurrence = new LiveOpsEventOccurrence(
                    occurrenceId: MetaGuid.NewWithTime(currentTime.ToDateTime()),
                    editVersion: 0,
                    isArchived: false,
                    definingSpecId: spec.SpecId,
                    timeMode,
                    utcScheduleOccasionMaybe,
                    spec.Settings.EventParams);
            }

            _state.LiveOpsEvents.EventSpecs.Add(spec.SpecId, spec);
            _state.LiveOpsEvents.EventOccurrences.Add(occurrence.OccurrenceId, occurrence);
            await PersistStateIntermediate();

            PublishMessage(EntityTopic.Member, new CreateLiveOpsEventMessage(occurrence, spec));

            return CreateLiveOpsEventResponse.CreateValid(
                diagnostics,
                eventSpecId: spec.SpecId,
                initialEventOccurrenceId: occurrence.OccurrenceId,
                relatedOccurrences,
                specs);
        }

        [EntityAskHandler]
        async Task<UpdateLiveOpsEventResponse> HandleUpdateLiveOpsEventRequestAsync(UpdateLiveOpsEventRequest request)
        {
            MetaGuid occurrenceId = request.OccurrenceId;
            LiveOpsEventSettings updatedSettings = request.Settings;

            MetaTime currentTime = MetaTime.Now;

            if (!_state.LiveOpsEvents.EventOccurrences.TryGetValue(occurrenceId, out LiveOpsEventOccurrence existingOccurrence))
                throw new InvalidEntityAsk($"Event occurrence {occurrenceId} not found");

            List<LiveOpsEventCreationDiagnostic> diagnostics = ValidateEventUpdate(existingOccurrence, updatedSettings, currentTime);

            List<LiveOpsEventOccurrence> relatedOccurrences = GetRelatedOccurrences(updatedSettings.EventParams.Content);
            List<LiveOpsEventSpec> specs = GetSpecsForOccurrences(relatedOccurrences);

            // \todo #liveops-event Option/parameter for "treat warnings as errors"?
            if (diagnostics.Any(diag => diag.Level == LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error))
            {
                return new UpdateLiveOpsEventResponse(isValid: false, diagnostics, relatedOccurrences, specs);
            }

            if (request.ValidateOnly)
            {
                return new UpdateLiveOpsEventResponse(isValid: true, diagnostics, relatedOccurrences, specs);
            }

            LiveOpsEventSpec existingSpec = _state.LiveOpsEvents.EventSpecs[existingOccurrence.DefiningSpecId];

            LiveOpsEventSpec updatedSpec = new LiveOpsEventSpec(
                specId: existingSpec.SpecId,
                editVersion: existingSpec.EditVersion + 1,
                isArchived: existingSpec.IsArchived,
                updatedSettings,
                existingSpec.CreatedAt);

            LiveOpsEventOccurrence updatedOccurrence;
            {
                (MetaScheduleTimeMode timeMode, LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe) = CreateSingleOccasionScheduleAssumeNoRecurrence(updatedSettings);

                updatedOccurrence = new LiveOpsEventOccurrence(
                    occurrenceId: existingOccurrence.OccurrenceId,
                    editVersion: existingOccurrence.EditVersion + 1,
                    isArchived: existingOccurrence.IsArchived,
                    definingSpecId: existingOccurrence.DefiningSpecId,
                    timeMode,
                    utcScheduleOccasionMaybe,
                    updatedSpec.Settings.EventParams);
            }
            // \todo #liveops-event Restrictions for updating existing occurrences:
            // \todo #liveops-event Update existing occurrence(s) as appropriate:
            //       - if occurrence is fully in past, don't update
            //       - if occurrence has started, don't update start time, but allow updating other things
            //       - otherwise, allow updating anything
            //       Offer some user controls for whether to update ongoing events or just future?
            _state.LiveOpsEvents.EventSpecs[existingSpec.SpecId] = updatedSpec;
            _state.LiveOpsEvents.EventOccurrences[existingOccurrence.OccurrenceId] = updatedOccurrence;
            await PersistStateIntermediate();

            PublishMessage(EntityTopic.Member, new UpdateLiveOpsEventMessage(updatedOccurrence, updatedSpec));

            return new UpdateLiveOpsEventResponse(isValid: true, diagnostics, relatedOccurrences, specs);
        }

        List<LiveOpsEventCreationDiagnostic> ValidateEventUpdate(LiveOpsEventOccurrence existingOccurrence, LiveOpsEventSettings updatedSettings, MetaTime currentTime)
        {
            List<LiveOpsEventCreationDiagnostic> diagnostics = ValidateEventSettings(updatedSettings, currentTime, ignoreExistingOccurrenceId: existingOccurrence.OccurrenceId);

            if (existingOccurrence.IsArchived)
            {
                // \todo Even for archived events, could allow editing some settings that don't affect anything, like display name and description.
                diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                    LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error,
                    "An event cannot be edited after it has been archived"));
            }

            if (updatedSettings.EventParams.Content != null // \note Null content will have already caused a diagnostic by ValidateEventSettings.
                && updatedSettings.EventParams.Content.GetType() != existingOccurrence.EventParams.Content.GetType())
            {
                diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                    LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error,
                    $"An existing event's type cannot be changed (trying to change {existingOccurrence.EventParams.Content.GetType().ToGenericTypeString()} to {updatedSettings.EventParams.Content.GetType().ToGenericTypeString()})"));
            }

            return diagnostics;
        }

        List<LiveOpsEventCreationDiagnostic> ValidateEventSettings(LiveOpsEventSettings settings, MetaTime currentTime, MetaGuid? ignoreExistingOccurrenceId = null)
        {
            List<LiveOpsEventCreationDiagnostic> diagnostics = new();

            if (settings.ScheduleMaybe != null)
            {
                if (!(settings.ScheduleMaybe is MetaRecurringCalendarSchedule schedule))
                {
                    // \note "Internal error" because the schedule is constructed in the controller, converted based on
                    //       more adminapi-friendly types.
                    diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                        LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error,
                        $"Internal error: got schedule of type {settings.ScheduleMaybe.GetType()}, expected {nameof(MetaRecurringCalendarSchedule)}"));
                }
                else
                {
                    // \note "Internal error" because the schedule is constructed in the controller, converted based on
                    //       more adminapi-friendly types.
                    if (schedule.Recurrence.HasValue)
                    {
                        diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                            LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error,
                            $"Internal error: recurring schedules not yet supported by LiveOps Events"));
                    }
                }
            }

            if (settings.EventParams.Content == null)
            {
                diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                    LiveOpsEventCreationDiagnostic.DiagnosticLevel.Error,
                    "LiveOps Event must have non-null Content"));
            }

            // Warnings about overlapping events (when desired, according to user implementation of ShouldWarnAboutOverlapWith)
            // \todo #liveops-event Think about overlap check more carefully. Didn't think about this too much yet.
            //       - Local vs UTC
            //       - Is it enough to only look at enabled time range, or should we also look at visibility time range?
            //       - What else...

            LiveOpsEventScheduleOccasion scheduleOccasion = CreateSingleOccasionScheduleAssumeNoRecurrence(settings).UtcScheduleOccasionMaybe;
            foreach (LiveOpsEventOccurrence existingOccurrence in _state.LiveOpsEvents.EventOccurrences.Values)
            {
                if (ignoreExistingOccurrenceId.HasValue && existingOccurrence.OccurrenceId == ignoreExistingOccurrenceId.Value)
                    continue;

                if (!existingOccurrence.EventParams.Content.ShouldWarnAboutOverlapWith(settings.EventParams.Content))
                    continue;

                LiveOpsEventScheduleOccasion existingScheduleOccasion = existingOccurrence.UtcScheduleOccasionMaybe;

                if (scheduleOccasion == null && existingScheduleOccasion == null)
                {
                    diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                        LiveOpsEventCreationDiagnostic.DiagnosticLevel.Warning,
                        $"New event overlaps with existing event {existingOccurrence.OccurrenceId} in the same group. Both events are always active (neither has a schedule)."));
                }
                else if (scheduleOccasion == null && existingScheduleOccasion != null)
                {
                    if (currentTime < existingScheduleOccasion.GetEnabledEndTime())
                    {
                        diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                            LiveOpsEventCreationDiagnostic.DiagnosticLevel.Warning,
                            $"New event overlaps with existing event {existingOccurrence.OccurrenceId} in the same group. The new event is always active (does not have a schedule) and the other event does not end until {existingScheduleOccasion.GetEnabledEndTime()}."));
                    }
                }
                else if (scheduleOccasion != null && existingScheduleOccasion == null)
                {
                    if (currentTime < scheduleOccasion.GetEnabledEndTime())
                    {
                        diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                            LiveOpsEventCreationDiagnostic.DiagnosticLevel.Warning,
                            $"New event overlaps with existing event {existingOccurrence.OccurrenceId} in the same group. The existing event is always active (does not have a schedule) and the new event does not end until {scheduleOccasion.GetEnabledEndTime()}."));
                    }
                }
                else
                {
                    MetaTime overlapStart = MetaTime.Max(scheduleOccasion.GetEnabledStartTime(),    existingScheduleOccasion.GetEnabledStartTime());
                    MetaTime overlapEnd   = MetaTime.Min(scheduleOccasion.GetEnabledEndTime(),      existingScheduleOccasion.GetEnabledEndTime());

                    if (overlapStart < overlapEnd)
                    {
                        diagnostics.Add(new LiveOpsEventCreationDiagnostic(
                            LiveOpsEventCreationDiagnostic.DiagnosticLevel.Warning,
                            $"New event overlaps with existing event {existingOccurrence.OccurrenceId} in the same group. The overlap is from {overlapStart} to {overlapEnd}. The existing event is from {existingScheduleOccasion.GetEnabledStartTime()} to {existingScheduleOccasion.GetEnabledEndTime()}."));
                    }
                }
            }

            return diagnostics;
        }

        [EntityAskHandler]
        async Task<SetLiveOpsEventArchivedStatusResponse> HandleSetLiveOpsEventArchivedStatusRequestAsync(SetLiveOpsEventArchivedStatusRequest request)
        {
            // \todo #liveops-event This is a very MVP version of archival. Need to think more about how archival affects event behavior.
            // \note #liveops-event Updating archical status does not currently bump EditVersion, as this MVP version of archival doesn't actually affect how PlayerActors treat archived events.

            MetaTime currentTime = MetaTime.Now;

            MetaGuid occurrenceId = request.OccurrenceId;

            if (!_state.LiveOpsEvents.EventOccurrences.TryGetValue(occurrenceId, out LiveOpsEventOccurrence existingOccurrence))
                throw new InvalidEntityAsk($"Event occurrence {occurrenceId} not found");

            // No change in status -> no-op success
            if (request.IsArchived == existingOccurrence.IsArchived)
                return SetLiveOpsEventArchivedStatusResponse.CreateSuccess();

            if (request.IsArchived)
            {
                LiveOpsEventPhase phase = LiveOpsEventServerUtil.GetCurrentPhase(existingOccurrence.ScheduleTimeMode, existingOccurrence.UtcScheduleOccasionMaybe, currentTime);

                // \note This is a soft check because the time between GSM and players is not completely in sync.
                //       PlayerActor will still need to consider archived events.
                if (phase != LiveOpsEventPhase.Disappeared)
                {
                    return SetLiveOpsEventArchivedStatusResponse.CreateFailure("An event cannot be archived until it has ended.");
                }
            }

            LiveOpsEventSpec existingSpec = _state.LiveOpsEvents.EventSpecs[existingOccurrence.DefiningSpecId];

            LiveOpsEventSpec updatedSpec = new LiveOpsEventSpec(
                specId: existingSpec.SpecId,
                editVersion: existingSpec.EditVersion,
                isArchived: request.IsArchived,
                existingSpec.Settings,
                existingSpec.CreatedAt);

            LiveOpsEventOccurrence updatedOccurrence = new LiveOpsEventOccurrence(
                occurrenceId: existingOccurrence.OccurrenceId,
                editVersion: existingOccurrence.EditVersion,
                isArchived: request.IsArchived,
                definingSpecId: existingOccurrence.DefiningSpecId,
                existingOccurrence.ScheduleTimeMode,
                existingOccurrence.UtcScheduleOccasionMaybe,
                existingOccurrence.EventParams);

            _state.LiveOpsEvents.EventSpecs[existingSpec.SpecId] = updatedSpec;
            _state.LiveOpsEvents.EventOccurrences[existingOccurrence.OccurrenceId] = updatedOccurrence;
            await PersistStateIntermediate();

            PublishMessage(EntityTopic.Member, new UpdateLiveOpsEventMessage(
                updatedOccurrence,
                updatedSpec));

            return SetLiveOpsEventArchivedStatusResponse.CreateSuccess();
        }

        [EntityAskHandler]
        GetLiveOpsEventsResponse HandleGetLiveOpsEventOccurrencesRequest(GetLiveOpsEventsRequest request)
        {
            List<LiveOpsEventOccurrence> occurrences;
            if (request.IncludeArchived)
                occurrences = _state.LiveOpsEvents.EventOccurrences.Values.ToList();
            else
                occurrences = _state.LiveOpsEvents.EventOccurrences.Values.Where(occurrence => !occurrence.IsArchived).ToList();

            List<LiveOpsEventSpec> specs = GetSpecsForOccurrences(occurrences);

            return new GetLiveOpsEventsResponse(
                occurrences,
                specs);
        }

        [EntityAskHandler]
        GetLiveOpsEventResponse HandleGetLiveOpsEventRequest(GetLiveOpsEventRequest request)
        {
            MetaGuid occurrenceId = request.OccurrenceId;

            if (!_state.LiveOpsEvents.EventOccurrences.TryGetValue(occurrenceId, out LiveOpsEventOccurrence requestedOccurrence))
                throw new InvalidEntityAsk($"Event occurrence {occurrenceId} not found");

            List<LiveOpsEventOccurrence> relatedOccurrences = GetRelatedOccurrences(requestedOccurrence.EventParams.Content);
            List<LiveOpsEventSpec> specs = GetSpecsForOccurrences(relatedOccurrences.Prepend(requestedOccurrence));

            return new GetLiveOpsEventResponse(
                requestedOccurrence,
                relatedOccurrences,
                specs);
        }

        List<LiveOpsEventOccurrence> GetRelatedOccurrences(LiveOpsEventContent content)
        {
            return _state.LiveOpsEvents.EventOccurrences.Values
                .Where(occ => occ.EventParams.Content.ShouldWarnAboutOverlapWith(content))
                .ToList();
        }

        List<LiveOpsEventSpec> GetSpecsForOccurrences(IEnumerable<LiveOpsEventOccurrence> occurrences)
        {
            IEnumerable<MetaGuid> specIds =
                occurrences
                .Select(occ => occ.DefiningSpecId)
                .Distinct();

            return specIds
                .Select(specId => _state.LiveOpsEvents.EventSpecs[specId])
                .ToList();
        }

        // \todo #liveops-event Needs to be rethought when recurring liveops events are supported.
        //       This is only good for MVP where each specs correspond 1-to-1 with occurrences.
        //
        //       Currently we're assuming a single-occasion schedule (or no schedule at all).
        //       In this case exactly 1 event occurrence is created from the spec, its schedule
        //       occasion matching the single occasion of the spec's schedule.
        (MetaScheduleTimeMode TimeMode, LiveOpsEventScheduleOccasion UtcScheduleOccasionMaybe) CreateSingleOccasionScheduleAssumeNoRecurrence(LiveOpsEventSettings settings)
        {
            MetaScheduleTimeMode timeMode;
            LiveOpsEventScheduleOccasion utcScheduleOccasionMaybe;
            if (settings.ScheduleMaybe == null)
            {
                timeMode = MetaScheduleTimeMode.Utc;
                utcScheduleOccasionMaybe = null;
            }
            else
            {
                MetaScheduleBase schedule = settings.ScheduleMaybe;

                timeMode = settings.ScheduleMaybe.TimeMode;

                MetaScheduleOccasion? metaOccasionMaybe = schedule.TryGetNextOccasion(new PlayerLocalTime(
                    time: MetaTime.Epoch,
                    utcOffset: MetaDuration.Zero));

                if (!metaOccasionMaybe.HasValue)
                {
                    // Not expected to happen.
                    utcScheduleOccasionMaybe = null;
                }
                else
                {
                    MetaScheduleOccasion metaOccasion = metaOccasionMaybe.Value;

                    OrderedDictionary<LiveOpsEventPhase, MetaTime> phaseSequence = new();

                    if (metaOccasion.VisibleRange.Start != metaOccasion.EnabledRange.Start)
                        phaseSequence.Add(LiveOpsEventPhase.Preview, metaOccasion.VisibleRange.Start);

                    phaseSequence.Add(LiveOpsEventPhase.NormalActive, metaOccasion.EnabledRange.Start);

                    if (metaOccasion.EndingSoonStartsAt != metaOccasion.EnabledRange.End)
                        phaseSequence.Add(LiveOpsEventPhase.EndingSoon, metaOccasion.EndingSoonStartsAt);

                    if (metaOccasion.EnabledRange.End != metaOccasion.VisibleRange.End)
                        phaseSequence.Add(LiveOpsEventPhase.Review, metaOccasion.EnabledRange.End);

                    phaseSequence.Add(LiveOpsEventPhase.Disappeared, metaOccasion.VisibleRange.End);

                    utcScheduleOccasionMaybe = new LiveOpsEventScheduleOccasion(phaseSequence);
                }
            }

            return (timeMode, utcScheduleOccasionMaybe);
        }
    }
}
