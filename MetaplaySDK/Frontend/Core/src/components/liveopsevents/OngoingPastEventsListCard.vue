<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-list-card(
  title="Ongoing and Past Events"
  icon="calendar-days"
  tooltip="Events that are active or have ended."
  :itemList="liveOpsEventsData?.ongoingAndPastEvents"
  :searchFields="['displayName', 'description']"
  :filterSets="ongoingAndPastLiveOpsEventFilterSets"
  :sortOptions="sortOptions"
  emptyMessage="No ongoing events right now, please create new ones."
  data-testid="ongoing-and-past-events"
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
        div(v-if="event.schedule !== null")
          span Started #[meta-time(:date="DateTime.fromISO(event.schedule.enabledStartTime ?? '')")]
          span(v-if="DateTime.fromISO(event.schedule.enabledEndTime ?? '') > DateTime.now()") &nbsp;and will end in #[meta-duration(:duration="DateTime.fromISO(event.schedule.enabledEndTime ?? '').diffNow()" showAs="exactDuration" hideMilliseconds)].
          span(v-else) &nbsp;and ran for #[meta-duration(:duration="DateTime.fromISO(event.schedule.enabledEndTime ?? '').diff(DateTime.fromISO(event.schedule.enabledStartTime ?? ''))" showAs="exactDuration" hideMilliseconds)].
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

import { DateTime } from 'luxon'

import { MetaListSortDirection, MetaListSortOption } from '@metaplay/meta-ui'
import { MListItem, MBadge, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

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
const ongoingAndPastLiveOpsEventFilterSets = computed(() => {
  if (liveOpsEventsData.value) {
    return makeListViewFilterSets(eventTypeNames.value, [LiveOpsEventPhase.Active, LiveOpsEventPhase.EndingSoon, LiveOpsEventPhase.InReview, LiveOpsEventPhase.Ended], true)
  } else {
    return []
  }
})

const sortOptions = [
  new MetaListSortOption('Creation time', 'createdAt', MetaListSortDirection.Descending),
  new MetaListSortOption('Creation time', 'createdAt', MetaListSortDirection.Ascending),
]
</script>
