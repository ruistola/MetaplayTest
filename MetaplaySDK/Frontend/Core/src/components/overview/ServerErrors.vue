<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MCard(
  v-if="hasPermission"
  title="Server Errors"
  :is-loading="!errorCountsData"
  :error="errorCountsError"
  )
  div(v-for="details in recentErrorDetails" :key="details.label").tw-flex.tw-justify-between
    span {{ toTitleCase(`Recent ${details.label}s`) }}
    MTooltip(:content="details.tooltip" :class="details.style")
      span {{ details.text }}
  template(#buttons)
    MButton(to="serverErrorLog") View Errors
  template(#subtitle)
    span Server errors {{ collectorRunTimespanHumanized(errorCountsData) }}
    span(v-if="errorCountsData?.collectorRestartedWithinMaxAge") &nbsp;
      meta-time(:date="errorCountsData?.collectorRestartTime" showAs="timeago")
    span .
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import { useGameServerApiStore } from '@metaplay/game-server-api'
import { maybePlural, toTitleCase } from '@metaplay/meta-ui'
import { MButton, MCard, MTooltip } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { collectorRunTimespanHumanized } from '../../coreUtils'
import { getServerErrorsSubscriptionOptions } from '../../subscription_options/general'
import type { ErrorCountResponse } from '../../subscription_options/generalTypes'

const gameServerApiStore = useGameServerApiStore()

const {
  data: errorCountsData,
  error: errorCountsError,
} = useSubscription<ErrorCountResponse>(getServerErrorsSubscriptionOptions())

/**
 * Does user have permission to see error logs?
 */
const hasPermission = computed(() => {
  return gameServerApiStore.doesHavePermission('api.system.view_error_logs')
})

/**
 * Note: This is a little over-engineered (RecentLogDetails and makeRecentLogDetails) because we used to have to deal
 * with both errors and warnings here, so it made sense to abstract the details into a helper function. Now we only
 * have to deal with errors we _could_ remove the abstraction, but it's not hurting anything.
 */

/**
 * Structure to hold details for the recent errors and warnings displays.
 */
interface RecentLogDetails {
  /**
   * Label name for the log level (Error, Warning, etc).
   */
  label: string
  /**
   * Numeric value to display for this log level.
   */
  text: string
  /**
   * Optional: Link to view more details for this log level.
   */
  viewLink?: string
  /**
   * Optional: Style to apply to the text.
   */
  style?: string
  /**
   * Optional: Tooltip to display for this log level.
   */
  tooltip?: string
}

/**
 * Helper function to fill out a RecentLogDetails object.
 * @param label Label name for the log level (error, warning, etc).
 * @param viewLink Optional link to view more details for this log level.
 * @param count Number of log entries.
 * @param overMaxCount True is the number of log entries has been capped.
 * @param textStyle Text style to use when rendering the count if it is non-zero.
 */
function makeRecentLogDetails (label: string, viewLink: string | undefined, count: number | undefined, overMaxCount: boolean | undefined, textStyle: string): RecentLogDetails {
  if (!errorCountsData.value || count === undefined || overMaxCount === undefined) {
    // Data not fetched yet.
    return {
      label,
      text: '...',
    }
  } else {
    if (count === 0) {
      return {
        label,
        text: '0',
        viewLink,
      }
    } else if (overMaxCount) {
      return {
        label,
        text: `${count}+`,
        viewLink,
        style: textStyle,
        tooltip: `More than ${maybePlural(count, label)} ${collectorRunTimespanHumanized(errorCountsData.value)}.`,
      }
    } else {
      return {
        label,
        text: `${count}`,
        viewLink,
        style: textStyle,
      }
    }
  }
}

/**
 * Details for the recent errors display.
 */
const recentErrorDetails = computed((): RecentLogDetails[] => {
  return [
    makeRecentLogDetails('error', '/serverErrorLog', errorCountsData.value?.errorCount, errorCountsData.value?.overMaxErrorCount, 'text-danger'),
  ]
})

/**
 * Subtitle for the card.
 */
const subtitleText = computed(() => {
  if (!errorCountsData.value) {
    return undefined
  } else {
    return `Server errors ${collectorRunTimespanHumanized(errorCountsData.value)}.`
  }
})
</script>
