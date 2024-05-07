// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

import { MetaListFilterSet, MetaListFilterOption } from '@metaplay/meta-ui'
import { type Variant } from '@metaplay/meta-ui-next'

import type { LiveOpsEventBriefInfo, LiveOpsEventPhase } from './liveOpsEventServerTypes'

interface PhaseInfo {
  displayString: string
  badgeVariant: Variant
  tooltip: string
}

export const liveOpsEventPhaseInfos: { [index in LiveOpsEventPhase]: PhaseInfo } = {
  NotYetStarted: {
    displayString: 'Not Yet Started',
    badgeVariant: 'neutral',
    tooltip: 'Not yet available or visible to players.'
  },
  InPreview: {
    displayString: 'In Preview',
    badgeVariant: 'primary',
    tooltip: 'Visible to players but not available yet.'
  },
  Active: {
    displayString: 'Active',
    badgeVariant: 'success',
    tooltip: 'Visible to players and available.'
  },
  EndingSoon: {
    displayString: 'Ending Soon',
    badgeVariant: 'warning',
    tooltip: 'Available and visible to players, but ending soon.'
  },
  InReview: {
    displayString: 'In Review',
    badgeVariant: 'primary',
    tooltip: 'Not available any more, but still visible to players.'
  },
  Ended: {
    displayString: 'Ended',
    badgeVariant: 'neutral',
    tooltip: 'No longer visible to players.'
  }
}

/**
 * Utility function to make filter sets for the live ops event list view.
 * @param eventTypeNames List of all the event type names to include in the filter set.
 * @param phases What phases to include in the filter set.
 * @param includedScheduledTimeMode Whether to include the scheduled time mode filter.
 */
export function makeListViewFilterSets (eventTypeNames: string[], phases: LiveOpsEventPhase[], includedScheduledTimeMode: boolean): MetaListFilterSet[] {
  const timeModeFilterOptions = [
    new MetaListFilterOption('UTC', (event) => (event as LiveOpsEventBriefInfo).schedule?.isPlayerLocalTime === false),
    new MetaListFilterOption('Local time', (event) => (event as LiveOpsEventBriefInfo).schedule?.isPlayerLocalTime === true),
    ...(includedScheduledTimeMode ? [new MetaListFilterOption('Unscheduled', (event) => (event as LiveOpsEventBriefInfo).schedule === null)] : [])
  ]

  return [
    new MetaListFilterSet('phases', phases.map((phaseKey) => new MetaListFilterOption(liveOpsEventPhaseInfos[phaseKey].displayString, (event) => (event as LiveOpsEventBriefInfo).currentPhase === phaseKey))),
    new MetaListFilterSet('eventType', eventTypeNames.map((eventTypeName) => new MetaListFilterOption(eventTypeName, (event) => (event as LiveOpsEventBriefInfo).eventTypeName === eventTypeName))),
    new MetaListFilterSet('timeMode', timeModeFilterOptions),
    new MetaListFilterSet('archived', [
      new MetaListFilterOption('Archived', (event) => (event as LiveOpsEventBriefInfo).isArchived),
      new MetaListFilterOption('Not archived', (event) => !(event as LiveOpsEventBriefInfo).isArchived, true),
    ])
  ]
}
