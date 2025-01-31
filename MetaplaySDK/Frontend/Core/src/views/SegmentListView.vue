<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :is-loading="!playerSegmentsData"
  :meta-api-error="playerSegmentsError"
  :alerts="alerts"
  )

  template(#overview)
    meta-page-header-card(title="View Player Segments")
      p Player segments are dynamic sets of players based on their game state.
      p.small.text-muted Players can enter and leave segments as their play the game and belong to multiple segments at once. You can define your own properties to segment players with and then create the actual segments in your #[MTextButton(to="/gameConfigs") game configs].
      p(v-if="playerSegmentsData.playerScanErrorCount && playerSegmentsData.playerScanErrorCount > 0").small.text-muted The segment size estimator encountered (#[span.font-weight-bold {{ segmentSizeEstimatorErrorRate?.toFixed(2) || 0 }}])% error rate while scanning for players (#[span.font-weight-bold {{playerSegmentsData.playerScanErrorCount || 0 }}] out of #[span.font-weight-bold {{ playerSegmentsData.scannedPlayersCount || 0 }}] players scanned). See the server logs for more information.

  template(#center-column)
    meta-list-card(
      data-testid="all-segments"
      title="All Segments"
      :itemList="allPlayerSegments"
      :searchFields="['info.displayName', 'info.description']"
      :sortOptions="sortOptions"
      :pageSize="20"
      emptyMessage="No player segments. Set them up in your game configs to start using the feature!"
      )
      template(#item-card="{ item: segment }")
        MListItem {{ segment.info.displayName }}
          template(#top-right): meta-audience-size-estimate(:sizeEstimate="segment.sizeEstimate")
          template(#bottom-left) {{ segment.info.description }}
          template(#bottom-right): MTextButton(:to="`/segments/${segment.info.segmentId}`" data-testid="view-segment") View segment

  template(#default)
    meta-raw-data(:kvPair="playerSegmentsData.segments" name="segments")/
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import { MetaListSortDirection, MetaListSortOption } from '@metaplay/meta-ui'
import type { MetaPageContainerAlert } from '@metaplay/meta-ui'
import { MListItem, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import MetaAudienceSizeEstimate from '../components/MetaAudienceSizeEstimate.vue'

import { getPlayerSegmentsSubscriptionOptions } from '../subscription_options/general'

const {
  data: playerSegmentsData,
  error: playerSegmentsError
} = useSubscription(getPlayerSegmentsSubscriptionOptions())

const allPlayerSegments = computed((): any[] | undefined => playerSegmentsData.value?.segments)

const sortOptions = [
  MetaListSortOption.asUnsorted(),
  new MetaListSortOption('Name', 'info.displayName', MetaListSortDirection.Ascending),
  new MetaListSortOption('Name', 'info.displayName', MetaListSortDirection.Descending),
  new MetaListSortOption('Audience Size Estimate', 'sizeEstimate', MetaListSortDirection.Ascending),
  new MetaListSortOption('Audience Size Estimate', 'sizeEstimate', MetaListSortDirection.Descending),
]

/**
 * The segment size estimator error rate.
 */
const segmentSizeEstimatorErrorRate = computed(() => {
  if (playerSegmentsData.value?.playerScanErrorCount && playerSegmentsData.value?.scannedPlayersCount) {
    return playerSegmentsData.value?.playerScanErrorCount / playerSegmentsData.value?.scannedPlayersCount * 100
  }
  return 0
})

/**
 * Alerts to show on the page header.
 */
const alerts = computed(() => {
  const allAlerts: MetaPageContainerAlert[] = []
  // Fixed 5% threshold before showing the segment size estimator error alert.
  if (segmentSizeEstimatorErrorRate.value > 5) {
    allAlerts.push({
      variant: 'warning',
      title: `Warning: ${segmentSizeEstimatorErrorRate.value.toFixed(2)}% errors encountered`,
      message: `The segment size estimator encountered ${segmentSizeEstimatorErrorRate.value.toFixed(2)}% error rate while scanning for players (${playerSegmentsData.value?.playerScanErrorCount} out of ${playerSegmentsData.value?.scannedPlayersCount} players). This can affect the accuracy of the segment size estimates. See the server logs for more information.`
    })
  }
  return allAlerts
})

</script>
