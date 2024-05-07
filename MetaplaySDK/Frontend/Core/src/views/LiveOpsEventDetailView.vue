<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :is-loading="!singleLiveOpsEventData"
  :meta-api-error="singleLiveOpsEventError"
  :alerts="alerts"
  permission="api.liveops_events.view"
  )
  template(#overview)
    meta-page-header-card(data-testid="overview" :id="eventId" )
      template(#title) {{ singleLiveOpsEventData?.eventParams.displayName }}
      template(#subtitle) {{ singleLiveOpsEventData?.eventParams.description || 'No description available.'}}

      div(class="tw-overflow-hidden")
        //- Overview.
        ul(role="list" class="tw-divide-y tw-divide-neutral-200 tw-m-0 tw-mb-4")
          div(class="tw-font-semibold tw-mb-1") #[fa-icon(icon='bar-chart')] Overview
          li(class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span Type
              span {{ eventTypeDisplayName }}
          li(class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span Template
              span {{ singleLiveOpsEventData?.eventParams.templateId }}
          li(class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span Archive Status
              MBadge(v-if="singleLiveOpsEventData?.isArchived" variant="neutral") Archived
              MBadge(v-else-if="!singleLiveOpsEventData?.isArchived" variant="success") Not Archived

        //- Schedule.
        ul(role="list" class="tw-divide-y tw-divide-neutral-200 tw-m-0")
          div(class="tw-font-semibold tw-mb-1") #[fa-icon(icon='calendar-alt')] Scheduling
          li(class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span Start Time
              span(v-if="!schedule" class="tw-italic tw-text-neutral-500") None
              meta-time(v-else :date="schedule.enabledStartTime ?? ''" show-as="datetime")
          li(class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span End Time
              span(v-if="!schedule" class="tw-italic tw-text-neutral-500") None
              meta-time(v-else :date="schedule.enabledEndTime ?? ''" show-as="datetime")
          li(class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span Preview Duration
              span(v-if="!schedule || Duration.fromISO(schedule.previewDuration).toMillis() === 0" class="tw-italic tw-text-neutral-500") None
              meta-duration(v-else :duration="schedule.previewDuration" showAs="exactDuration" hideMilliseconds)
          li(class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span Ending Soon Duration
              span(v-if="!schedule || Duration.fromISO(schedule.endingSoonDuration).toMillis() === 0" class="tw-italic tw-text-neutral-500") None
              meta-duration(v-else :duration="schedule.endingSoonDuration" showAs="exactDuration" hideMilliseconds)
          li( class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span Review Duration
              span(v-if="!schedule || Duration.fromISO(schedule.reviewDuration).toMillis() === 0" class="tw-italic tw-text-neutral-500") None
              meta-duration(v-else :duration="schedule?.reviewDuration" showAs="exactDuration" hideMilliseconds)
          li(class="tw-block tw-py-1")
            span(class="tw-flex tw-justify-between")
              span Total Duration
              span(v-if="!schedule" class="tw-italic tw-text-neutral-500") None
              meta-duration(v-else :duration="totalDurationInSeconds" showAs="exactDuration" hideMilliseconds)

      //- Only show timeline if event is not already ended.
      span(v-if="singleLiveOpsEventData?.currentPhase !== 'Ended'")
        //- Time mode.
        div(class="tw-my-5")
          ul(v-if="schedule" role="list" class="tw-divide-y tw-divide-neutral-200 tw-m-0")
            div(class="tw-flex tw-justify-between")
              span Time Mode: #[MBadge {{ schedule?.isPlayerLocalTime ? 'Local' : 'UTC' }}]
              span Current Phase:&nbsp;
                MBadge(
                  v-if="singleLiveOpsEventData?.currentPhase"
                  :variant="liveOpsEventPhaseInfos[singleLiveOpsEventData.currentPhase].badgeVariant"
                  :tooltip="liveOpsEventPhaseInfos[singleLiveOpsEventData.currentPhase].tooltip"
                  ) {{ liveOpsEventPhaseInfos[singleLiveOpsEventData.currentPhase].displayString }}
              span Next Phase:&nbsp;
                MBadge(
                  v-if="singleLiveOpsEventData?.nextPhase"
                  :variant="liveOpsEventPhaseInfos[singleLiveOpsEventData.nextPhase].badgeVariant"
                  :tooltip="liveOpsEventPhaseInfos[singleLiveOpsEventData.nextPhase].tooltip"
                  ) {{ liveOpsEventPhaseInfos[singleLiveOpsEventData.nextPhase].displayString }}
                span &nbsp;#[meta-time(v-if="singleLiveOpsEventData?.nextPhaseTime" :date="singleLiveOpsEventData.nextPhaseTime")]

        //- Progress bar.
        div(:style="`margin-left: ${durationToMilliseconds(schedule?.previewDuration ?? '0') / totalDurationInSeconds * 100}%; position: relative; left: -1px`").pb-3.pl-2.border-left.border-dark
          div.small.font-weight-bold Start
          div.small
            span(v-if="schedule === null") No schedule
            meta-time(v-else :date="schedule?.enabledStartTime ?? ''")
        // Schedule timeline
        b-progress(v-if="schedule" :max="totalDurationInSeconds" height="3rem").font-weight-bold
          b-progress-bar(
            :value="durationToMilliseconds(schedule?.previewDuration ?? '0')"
            variant="info"
          ) Preview
          b-progress-bar(
            :value="schedule.endingSoonDuration ? enabledDurationInMilliseconds - durationToMilliseconds(schedule.endingSoonDuration) : enabledDurationInMilliseconds"
            variant="success"
          ) Active
          b-progress-bar(
            :value="durationToMilliseconds(schedule.endingSoonDuration ?? '0')"
            variant="warning"
          ) Ending soon
          b-progress-bar(
            :value="durationToMilliseconds(schedule.reviewDuration ?? '0')"
            variant="info"
          ) Review
        b-progress(v-else :max="1" height="3rem").font-weight-bold
          b-progress-bar(
              :value="1"
              variant="success"
            ) Active
        div(:style="`margin-right: ${durationToMilliseconds(schedule?.reviewDuration ?? '0') / totalDurationInSeconds * 100}%; position: relative; right: -1px`").pt-3.pr-2.pb-1.border-right.border-dark.text-right
          div.small.font-weight-bold End
          div.small
            span(v-if="schedule === null") No schedule
            meta-time(v-else :date="schedule?.enabledEndTime ?? ''")

      template(#buttons)
        live-ops-event-action-archive(:eventId="eventId")
        live-ops-event-form-modal-button(
          form-mode="edit"
          :current-phase="singleLiveOpsEventData?.currentPhase"
          :prefill-data="singleLiveOpsEventData?.eventParams"
          :event-id="singleLiveOpsEventData?.eventId"
          @refresh="singleLiveOpsEventRefresh"
          )
        live-ops-event-form-modal-button(
          form-mode="duplicate"
          :prefill-data="singleLiveOpsEventData?.eventParams"
          @refresh="singleLiveOpsEventRefresh"
          )
        MButton(disabled-tooltip="Export feature is currently under construction.") Export
  template(#default)
    b-container
      b-row(no-gutters align-v="center").mt-3.mb-2
        h3 Configuration

      b-row(align-h="center")
        b-col(md="6").mb-3
          live-ops-event-related-events-card(:eventId="eventId")

        b-col(md="6").mb-3
          MCard(title="Event Configuration" data-testid="event-configuration")
            meta-generated-content(:value="singleLiveOpsEventData?.eventParams.content")

        b-col(md="6").mb-3
          targeting-card(
            :targetCondition="singleLiveOpsEventData?.eventParams.targetCondition ?? null"
            ownerTitle="This experiment"
            )

    meta-raw-data(:kvPair="singleLiveOpsEventData" name="singleLiveOpsEventData")
</template>
<script lang="ts" setup>
import { BProgress, BProgressBar } from 'bootstrap-vue'
import { DateTime, Duration } from 'luxon'
import { computed } from 'vue'
import { useRoute } from 'vue-router'

import { type MetaPageContainerAlert } from '@metaplay/meta-ui'
import { MBadge, MButton, MCard } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import MetaGeneratedContent from '../components/generatedui/components/MetaGeneratedContent.vue'
import LiveOpsEventActionArchive from '../components/liveopsevents/LiveOpsEventActionArchive.vue'
import LiveOpsEventFormModalButton from '../components/liveopsevents/LiveOpsEventFormModalButton.vue'
import LiveOpsEventRelatedEventsCard from '../components/liveopsevents/LiveOpsEventRelatedEventsCard.vue'
import TargetingCard from '../components/mails/TargetingCard.vue'
import { getLiveOpsEventTypesSubscriptionOptions, getSingleLiveOpsEventsSubscriptionOptions } from '../subscription_options/liveOpsEvents'
import { routeParamToSingleValue, durationToMilliseconds } from '../coreUtils'
import type { LiveOpsEventDetailsInfo, LiveOpsEventTypeInfo } from '../liveOpsEventServerTypes'
import { liveOpsEventPhaseInfos } from '../liveOpsEventUtils'

const route = useRoute()
const eventId = routeParamToSingleValue(route.params.id)

/**
 * Information about the event.
 */
const {
  data: singleLiveOpsEventData,
  error: singleLiveOpsEventError,
  refresh: singleLiveOpsEventRefresh
} = useSubscription<LiveOpsEventDetailsInfo>(getSingleLiveOpsEventsSubscriptionOptions(eventId))

/**
 * All template details.
 */
const {
  data: liveOpsEventTypesData
} = useSubscription<LiveOpsEventTypeInfo[]>(getLiveOpsEventTypesSubscriptionOptions())

/**
 * Shortcut access to the schedule.
 */
const schedule = computed(() => {
  return singleLiveOpsEventData.value?.eventParams.schedule ?? null
})

/**
 * Enabled duration for the event, ie: between scheduled start and end time, in seconds.
 */
const enabledDurationInMilliseconds = computed(() => {
  if (singleLiveOpsEventData.value?.eventParams.schedule) {
    const duration = DateTime.fromISO(singleLiveOpsEventData.value.eventParams.schedule.enabledEndTime ?? '')
      .diff(DateTime.fromISO(singleLiveOpsEventData.value.eventParams.schedule.enabledStartTime ?? ''))
      .toISO() ?? 'PT0S'
    return durationToMilliseconds(duration)
  } else {
    return 0
  }
})

/**
 * Total duration of the event, including preview and review, in seconds.
 */
const totalDurationInSeconds = computed(() => {
  if (singleLiveOpsEventData.value?.eventParams.schedule) {
    return durationToMilliseconds(singleLiveOpsEventData.value.eventParams.schedule.previewDuration ?? 'PT0S') +
      enabledDurationInMilliseconds.value +
      durationToMilliseconds(singleLiveOpsEventData.value?.eventParams.schedule.reviewDuration ?? 'PT0S')
  } else {
    return 0
  }
})

/**
 * Return the display name of the event type. Fallback to the full type name if it cannot be found.
 */
const eventTypeDisplayName = computed(() => {
  const eventContentClass = singleLiveOpsEventData.value?.eventParams.content.$type
  const eventType = (liveOpsEventTypesData.value ?? []).find((type) => {
    const typeContentClass = type.contentClass.split(',')[0]
    return eventContentClass === typeContentClass
  })
  return eventType?.eventTypeName ?? eventContentClass
})

/**
 * Array of messages to be displayed at the top of the page.
 */
const alerts = computed(() => {
  const allAlerts: MetaPageContainerAlert[] = []
  if (singleLiveOpsEventData.value?.currentPhase === 'Ended') {
    allAlerts.push({
      title: 'Past Event',
      message: 'You are currently viewing an event that has already ended.',
      variant: 'secondary',
    })
  }
  return allAlerts
})
</script>
