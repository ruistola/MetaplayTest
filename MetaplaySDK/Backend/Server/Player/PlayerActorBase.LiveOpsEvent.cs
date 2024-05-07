// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Cloud.Entity;
using Metaplay.Core;
using Metaplay.Core.LiveOpsEvent;
using Metaplay.Server.LiveOpsEvent;
using System.Collections.Generic;
using System.Linq;

namespace Metaplay.Server
{
    public abstract partial class PlayerActorBase<TModel, TPersisted>
    {
        protected AtomicValueSubscriber<ActiveLiveOpsEventSet> _activeLiveOpsEventStateSubscriber = GlobalStateProxyActor.ActiveLiveOpsEventState.Subscribe();

        class RefreshLiveOpsEventsTrigger { public static readonly RefreshLiveOpsEventsTrigger Instance = new RefreshLiveOpsEventsTrigger(); }

        [CommandHandler]
        void HandleRefreshLiveOpsEventsTrigger(RefreshLiveOpsEventsTrigger _)
        {
            RefreshLiveOpsEvents();
        }

        void RefreshLiveOpsEvents()
        {
            _activeLiveOpsEventStateSubscriber.Update((_, _) => { });

            IReadOnlyList<LiveOpsEventOccurrence> liveOpsEvents = _activeLiveOpsEventStateSubscriber.Current.LiveOpsEventOccurrences;

            // \todo #liveops-event Handle removal of an event from ActiveLiveOpsEventSet.

            foreach (LiveOpsEventOccurrence liveOpsEvent in liveOpsEvents)
            {
                MetaGuid eventId = liveOpsEvent.OccurrenceId;

                bool playerIsInTargetAudience = Model.PassesFilter(liveOpsEvent.EventParams.PlayerFilter, out bool _);
                PlayerLiveOpsEventServerOnlyModel serverState = Model.LiveOpsEvents.ServerOnly.EventModels.GetValueOrDefault(eventId);

                if (!playerIsInTargetAudience)
                {
                    // If player isn't in target audience and doesn't already have the event, do nothing.
                    if (serverState == null)
                        continue;

                    // If player isn't in target audience, but does already have the event, _and_ the event is not "sticky", then abruptly remove the event.
                    // In contrast, existing "sticky" events stick around even if the player moves out of the target audience.
                    if (!liveOpsEvent.EventParams.Content.GetActivationIsSticky())
                    {
                        Model.LiveOpsEvents.ServerOnly.EventModels.Remove(eventId);
                        EnqueueServerAction(new PlayerAbruptlyRemoveLiveOpsEvent(eventId));
                        continue;
                    }
                }

                // Adjust schedule according to time mode and player's UTC offset
                // \todo #liveops-event For local-time events, fix the UTC offset when the event starts, so player can't tweak time offset to extend the event.
                LiveOpsEventScheduleOccasion scheduleForPlayerMaybe = liveOpsEvent.UtcScheduleOccasionMaybe?.GetUtcOccasionAdjustedForPlayer(liveOpsEvent.ScheduleTimeMode, Model.TimeZoneInfo.CurrentUtcOffset);

                // If event params have been edited, update them to the player
                if (serverState != null && serverState.EditVersion != liveOpsEvent.EditVersion)
                {
                    serverState.EditVersion = liveOpsEvent.EditVersion;
                    EnqueueServerAction(new PlayerUpdateEventLiveOpsEventParams(eventId, scheduleForPlayerMaybe, liveOpsEvent.EventParams.Content));
                }

                // Calculate current phase according to schedule (if there is no schedule, it behaves like a schedule that is always active).
                LiveOpsEventPhase currentPhase = scheduleForPlayerMaybe?.GetPhaseAtTime(Model.CurrentTime) ?? LiveOpsEventPhase.NormalActive;

                // Update the player's event state, depending on currentPhase and on whether the player already has the event.

                // \todo #liveops-event User-definable hooks for controlling what kinds of phase transitions are permitted:
                //       - Support stopping activation of an event, based on custom criteria.
                //       - Support controlling what happens in atypical cases where phase isn't being simply advanced. For the following scenarios (granularity
                //         between the different scenarios is TBD), the user should be able to define whether the event stays in the phase that it was previously
                //         for the player, or whether it gets "reversed" or otherwise coerced to the current schedule-dictated phase. Also need proper API hooks
                //         for this "reversing" (possibly separate from the current OnPhaseChanged (which could be renamed to OnPhaseAdvanced or something)).
                //         - When the schedule has been edited such that the schedule shouldn't have started yet but it was previously started for the player.
                //         - When the schedule has been edited such that the schedule's current phase *precedes* the phase that was last updated for the player.
                //         - When the schedule has been edited such that the schedule no longer contains the phase that was last updated for the player.
                //       #lifecycle-anomaly-controls

                if (serverState == null)
                {
                    // Player doesn't yet have the event.

                    // If event is not in a visible phase, don't add it for the player.
                    if (currentPhase == LiveOpsEventPhase.NotStartedYet
                     || currentPhase == LiveOpsEventPhase.Disappeared)
                    {
                        continue;
                    }

                    // Add the event for the player (initially in the NotYetStarted phase, but will be updated right after).
                    serverState = new PlayerLiveOpsEventServerOnlyModel(
                        eventId: eventId,
                        latestAssignedPhase: LiveOpsEventPhase.NotStartedYet,
                        editVersion: liveOpsEvent.EditVersion);
                    Model.LiveOpsEvents.ServerOnly.EventModels.Add(eventId, serverState);
                    EnqueueServerAction(new PlayerAddLiveOpsEvent(eventId, scheduleForPlayerMaybe, liveOpsEvent.EventParams.Content));

                    // Advance the event to currentPhase.
                    IEnumerable<LiveOpsEventPhase> fastForwardedPhases = scheduleForPlayerMaybe?.GetPhasesBetween(startPhaseExclusive: LiveOpsEventPhase.NotStartedYet, endPhaseExclusive: currentPhase) ?? Enumerable.Empty<LiveOpsEventPhase>();
                    serverState.LatestAssignedPhase = currentPhase;
                    EnqueueServerAction(new PlayerRunLiveOpsPhaseSequence(eventId, fastForwardedPhases.ToList(), currentPhase));
                }
                else
                {
                    // Player already has the event.

                    // If phase has not changed since last update, do nothing.
                    if (currentPhase == serverState.LatestAssignedPhase)
                        continue;

                    // If the current scheduled phase *precedes* the phase that was previously known to the player, do nothing.
                    // This ensures the phases only ever advance forwards.
                    // \todo #liveops-event See comment a bit above, #lifecycle-anomaly-controls
                    if (LiveOpsEventPhase.PhasePrecedes(currentPhase, serverState.LatestAssignedPhase))
                        continue;

                    // Advance the event to currentPhase.
                    IEnumerable<LiveOpsEventPhase> fastForwardedPhases = scheduleForPlayerMaybe?.GetPhasesBetween(startPhaseExclusive: serverState.LatestAssignedPhase, endPhaseExclusive: currentPhase) ?? Enumerable.Empty<LiveOpsEventPhase>();
                    serverState.LatestAssignedPhase = currentPhase;
                    EnqueueServerAction(new PlayerRunLiveOpsPhaseSequence(eventId, fastForwardedPhases.ToList(), currentPhase));

                    // If the event has ended (reached the final phase, Disappeared), remove it.
                    if (currentPhase == LiveOpsEventPhase.Disappeared)
                    {
                        Model.LiveOpsEvents.ServerOnly.EventModels.Remove(eventId);
                        EnqueueServerAction(new PlayerRemoveDisappearedLiveOpsEvent(eventId));
                    }
                }
            }
        }
    }
}
