<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :is-loading="!errorCountsData"
  :meta-api-error="errorCountsError"
  :alerts="pageAlerts"
  )
  template(#overview)
    meta-page-header-card()
      template(#title) Server Error Log
      template(#subtitle)
        p These are the most recent errors that have been logged by the server. Errors are often indicative of a problem with the server, and should be investigated.
      template(#default)
        span(v-if="errorCountsData")
          p.small.text-muted
            span Errors listed on this page are only stored for #[meta-duration(:duration="errorCountsData?.maxAge" showAs="humanized")] and are not retained across server restarts.
            span(v-if="grafanaEnabled")  A more complete history of errors and warnings is available in&nbsp;
              MTextButton(
                :to="grafanaMoreCompleteHistory"
                permission="dashboard.grafana.view"
                ) Grafana
              span .
            span(v-else)  When Grafana is configured, a more complete history of errors and warnings can be found there.
          p.small.text-muted
            span The logging collector was restarted #[meta-time(:date="errorCountsData?.collectorRestartTime" showAs="timeago")].
        span(v-else)

  template(#center-column)
    meta-event-stream-card(
      title="Server Errors"
      :event-stream="eventStream"
      search-is-filter
      empty-message="No errors logged."
      no-results-message="No errors found. Try a different search."
      allow-pausing
      )
      template(#event-details="{ event }")
        MErrorCallout(:error="eventToDisplayError(event)")
          template(#buttons)
            MButton(
              :to="getGrafanaLogsUrl(event)"
              :disabled-tooltip="grafanaEnabled ? undefined : 'Grafana has not been configured for this environment.'"
              permission="dashboard.grafana.view"
              ) View in Grafana

  meta-raw-data(:kvPair="errorCountsData" name="errorCountsData")
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import { EventStreamItemEvent, MetaEventStreamCard } from '@metaplay/event-stream'
import type { MetaPageContainerAlert } from '@metaplay/meta-ui'
import { DisplayError, MButton, MErrorCallout, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getServerErrorsSubscriptionOptions } from '../subscription_options/general'
import type { ErrorCountResponse, LogEventInfo } from '../subscription_options/generalTypes'
import { useCoreStore } from '../coreStore'
import { collectorRunTimespanHumanized } from '../coreUtils'
import { makeGrafanaQueryExpression, makeGrafanaUri } from '../grafanaUtils'

const coreStore = useCoreStore()

const {
  data: errorCountsData,
  error: errorCountsError,
} = useSubscription<ErrorCountResponse>(getServerErrorsSubscriptionOptions())

/**
 * Whether Grafana is enabled for this environment.
 */
const grafanaEnabled = computed(() => coreStore.hello.grafanaUri)

/**
 * Link to more complete history of errors and warnings in Grafana.
 */
const grafanaMoreCompleteHistory = computed(() => {
  return makeGrafanaUri(
    [makeGrafanaQueryExpression('loglevel', '=~', 'ERR|WRN')],
    'now-1h',
    'now'
  )
})

/**
 * Returns a URL to the Grafana logs for the given event.
 */
function getGrafanaLogsUrl (event: EventStreamItemEvent) {
  if (grafanaEnabled.value) {
    const timestamp = new Date(event.time)
    return makeGrafanaUri(
      [],
      new Date(+timestamp - 30_000),
      new Date(+timestamp + 1_000))
  } else {
    return undefined
  }
}

/**
 * Event stream data, generated from the fetched data.
 */
const eventStream = computed(() => {
  if (errorCountsData.value) {
    // Regex to trim any characters after the first newline.
    const trimRegexp: RegExp = /\n.*/g

    // Create an event stream.
    const eventStream = errorCountsData.value.errors.map((entry) => {
      return new EventStreamItemEvent(
        entry.timestamp,
        entry.sourceType,
        entry.message.replace(trimRegexp, ''),
        entry.id,
        entry,
        '',
        '',
        ''
      )
    })

    return eventStream
  } else {
    return []
  }
})

/**
 * Convert an event to a displayable error.
 */
function eventToDisplayError (event: EventStreamItemEvent): DisplayError {
  const sourceEvent: LogEventInfo = event.typeData.sourceData
  const displayError = new DisplayError(sourceEvent.source, sourceEvent.message)
  if (sourceEvent.exception) displayError.addDetail('Exception', sourceEvent.exception)
  if (sourceEvent.stackTrace) displayError.addDetail('Stack', sourceEvent.stackTrace)
  return displayError
}

/**
 * Show alerts based on the error counts.
 */
const pageAlerts = computed((): MetaPageContainerAlert[] => {
  if (errorCountsData.value?.overMaxErrorCount) {
    const maxErrorCount = errorCountsData.value.errorCount
    const restartString = collectorRunTimespanHumanized(errorCountsData.value)
    return [{
      title: 'Too Many Errors',
      message: `The server has generated over ${maxErrorCount} errors ${restartString}. This may indicate a problem with the server.`,
      variant: 'danger',
    }]
  } else {
    return []
  }
})
</script>
