<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-list-card(
  title="Upcoming Events"
  icon="calendar-days"
  tooltip="Events that are scheduled but have not started yet."
  :itemList="liveOpsEventsData?.upcomingEvents"
  :searchFields="['displayName', 'description']"
  :filterSets="upcomingLiveOpsEventFilterSets"
  :sortOptions="sortOptions"
  emptyMessage="No upcoming events right now, please create new ones."
  data-testid="upcoming-events"
  )
  template(#item-card="{ item: event }")
    MListItem {{ event.displayName}}
      template(#badge)
        MBadge(variant="primary") {{ event.eventTypeName }}
        MBadge(v-if="event.schedule?.isPlayerLocalTime === true") Local
        MBadge(v-else-if="event.schedule?.isPlayerLocalTime === false") UTC
        MBadge(v-else) No schedule

      template(#top-right)
        MBadge(:variant="liveOpsEventPhaseInfos[event.currentPhase].badgeVariant" :tooltip="liveOpsEventPhaseInfos[event.currentPhase].tooltip") {{ liveOpsEventPhaseInfos[event.currentPhase].displayString }}

      template(#bottom-left)
        span Starting #[meta-time(:date="DateTime.fromISO(event.schedule?.enabledStartTime ?? '')")]
        span(v-if="event.schedule?.enabledEndTime") &nbsp;and will run for #[meta-duration(:duration="DateTime.fromISO(event.schedule?.enabledEndTime).diffNow()" showAs="exactDuration" hideMilliseconds)].
        span(v-else)  and will run indefinitely.
        div(v-if="event.description") {{ event.description }}
        div(v-else class="tw-italic") No description.

      template(#bottom-right)
        div(v-if="event.nextPhase") {{ liveOpsEventPhaseInfos[event.nextPhase].displayString }} in #[meta-duration(:duration="DateTime.fromISO(event.nextPhaseTime).diffNow()" showAs="exactDuration" hideMilliseconds)].
        MTextButton(
          :to="`/liveOpsEvents/${event.eventId}`"
          ) View event
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import { MetaListSortDirection, MetaListSortOption } from '@metaplay/meta-ui'
import { MListItem, MBadge, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { DateTime } from 'luxon'

import { getAllLiveOpsEventsSubscriptionOptions, getLiveOpsEventTypesSubscriptionOptions } from '../../subscription_options/liveOpsEvents'
import { type GetLiveOpsEventsListApiResult, LiveOpsEventPhase, type LiveOpsEventTypeInfo } from '../../liveOpsEventServerTypes'

import { liveOpsEventPhaseInfos, makeListViewFilterSets } from '../../liveOpsEventUtils'

const {
  data: liveOpsEventsData
} = useSubscription<GetLiveOpsEventsListApiResult>(getAllLiveOpsEventsSubscriptionOptions())

const {
  data: liveOpsEventTypesData
} = useSubscription<LiveOpsEventTypeInfo[]>(getLiveOpsEventTypesSubscriptionOptions())

const eventTypeNames = computed(() => {
  return liveOpsEventTypesData.value?.map((type) => type.eventTypeName) ?? []
})

/**
 * Filtering options passed to the MetaListCard component.
 */
const upcomingLiveOpsEventFilterSets = computed(() => {
  if (liveOpsEventsData.value) {
    return makeListViewFilterSets(eventTypeNames.value, [LiveOpsEventPhase.NotYetStarted, LiveOpsEventPhase.InPreview], false)
  } else {
    return []
  }
})

const sortOptions = [
  new MetaListSortOption('Start time', 'schedule.enabledStartTime', MetaListSortDirection.Ascending),
  new MetaListSortOption('Start time', 'schedule.enabledStartTime', MetaListSortDirection.Descending),
]
</script>
