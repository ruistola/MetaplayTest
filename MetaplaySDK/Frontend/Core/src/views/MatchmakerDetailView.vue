<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :is-loading="!singleMatchmakerData"
  :meta-api-error="singleMatchmakerError"
  :alerts="alerts"
  )
  template(#error-card-message)
    p Oh no, something went wrong while trying to access the matchmaker!
  template(#overview)
    //- Overview
    meta-page-header-card(data-testid="matchmaker-overview-card" :id="matchmakerId")
      template(#title) {{ singleMatchmakerData.data.name }}
      template(#subtitle) {{ singleMatchmakerData.data.description }}
      template(#caption) Save file size:&nbsp;
        MTextButton(permission="api.database.inspect_entity" :to="`/entities/${matchmakerId}/dbinfo`" data-testid="model-size-link" )
          meta-abbreviate-number(:value="singleMatchmakerData.data.stateSizeInBytes" unit="byte")

      div.font-weight-bold #[fa-icon(icon="chart-bar")] Overview
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Number of participants
            b-td.text-right {{ singleMatchmakerData.data.playersInBuckets }}
          b-tr
            b-td Current capacity
            b-td.text-right(v-if="singleMatchmakerData?.data?.bucketsOverallFillPercentage > 0") {{ Math.round(singleMatchmakerData.data.bucketsOverallFillPercentage * 10000) / 100 }}%
            b-td.text-right.font-italic.text-muted(v-else) None
          //- b-tr
            b-td Number of matches during the last hour
            b-td.text-right TBD

      div.font-weight-bold #[fa-icon(icon="scale-unbalanced")] Rebalancing
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Last rebalanced
            b-td.text-right: meta-time(:date="singleMatchmakerData.data.lastRebalanceOperationTime")

      p.small.text-muted(v-if="singleMatchmakerData.data.playerScanErrorCount && singleMatchmakerData.data.playerScanErrorCount > 0") The matchmaker encountered (#[span.font-weight-bold {{ scanningErrorRate.toFixed(2) }}])% error rate while scanning for players (#[span.font-weight-bold {{singleMatchmakerData.data.playerScanErrorCount }} ] out of #[span.font-weight-bold {{ singleMatchmakerData.data.scannedPlayersCount }}] players scanned). See the server logs for more information.

      template(#buttons)
        div(class="tw-flex tw-justify-end tw-mt-3 tw-gap-2")
          //- Simulation modal
          MActionModalButton(
            modal-title="Simulate Matchmaking"
            :action="async () => {}"
            trigger-button-label="Simulate"
            only-close
            permission="api.matchmakers.test"
            data-testid="simulate-matchmaking"
            @hide="simulationResult = undefined"
            )
              template(#default)
                div(class="tw-pr-4 tw-border-r-2 tw-border-neutral-200")
                  p You can use this tool to preview the matches this matchmaker would return for a given matchmaking ranking (MMR).
                  meta-generated-form(
                    :typeName="singleMatchmakerData.queryJsonType"
                    v-model="simulationData"
                    @status="isSimulationFormValid = $event"
                    addTypeSpecifier
                    class="tw-mb-3"
                    )
                  MButton(
                    :disabled="!isSimulationFormValid"
                    @click="simulateMatchmaking"
                    data-testid="simulate-matchmaking-ok-button"
                    ) Simulate

              template(#right-panel)
                h5 Matchmaking Results #[span(v-if="simulationResult?.numTries" class="tw-text-xs tw-normal-case") After #[meta-plural-label(:value="simulationResult.numTries" label="iteration")]]

                //- Simulation is running.
                div(v-if="isSimulationRunning" class="tw-text-center tw-pt-3 tw-text-neutral-400 tw-italic")
                  span Simulating...

                //- Error.
                div(v-else-if="simulationResult?.response?.data?.error")
                  MErrorCallout(:error="simulationResult.response.data.error")

                //- Results.
                div(v-else-if="simulationResult?.response?.responseType === 'Success' && previewCandidateData")
                  MList(showBorder class="tw-px-3")
                    MListItem {{ previewCandidateData.model.playerName }}
                      template(#top-right) {{ previewCandidateData.id }}
                      template(#bottom-right): MTextButton(permission="api.players.view" :to="`/players/${simulationResult.response.bestCandidate}`") View player

                //- No results.
                div(v-else-if="simulationResult?.response?.responseType" class="tw-pt-4")
                  p No matches found!

                //- Haven't run the simulation at all yet.
                div(v-else class="tw-text-center tw-pt-3 tw-text-neutral-400 tw-italic")
                  | Simulation not run yet.

          //- Re-balance modal
          MActionModalButton(
            modal-title="Rebalance Matchmaker"
            :action="rebalanceMatchmaker"
            trigger-button-label="Rebalance"
            ok-button-label="Rebalance"
            :ok-button-disabled="!singleMatchmakerData.data.hasEnoughDataForBucketRebalance"
            permission="api.matchmakers.admin"
            data-testid="rebalance-matchmaker"
            )
              p Rebalancing this matchmaker will re-distribute participants to the configured matchmaking buckets.
              p.text-muted.small Matchmakers automatically rebalance themselves over time. Manually triggering the rebalancing is mostly useful for manual testing during development.

              b-alert(:show="!singleMatchmakerData.data.hasEnoughDataForBucketRebalance" variant="danger" data-testid="not-enough-data") This matchmaker does not have enough data to rebalance. Please wait until the matchmaker has been populated with enough data from players.

          //- Reset modal
          MActionModalButton(
            modal-title="Reset Matchmaker"
            :action="resetMatchmaker"
            trigger-button-label="Reset"
            ok-button-label="Reset"
            variant="warning"
            permission="api.matchmakers.admin"
            data-testid="reset-matchmaker"
            )
              p Resetting this matchmaker will immediately re-initialize it.
              p.text-muted.small Resetting is safe to do in a production environment, but might momentarily degrade the matchmaking experience for live players as it takes a few minutes for the matchmaker to re-populate.

  template(#default)
    core-ui-placement(placementId="Matchmakers/Details" :matchmakerId="matchmakerId")

    meta-raw-data(:kvPair="singleMatchmakerData" name="singleMatchmakerData")
</template>

<script lang="ts" setup>
import { Chart as ChartJS, Title, Tooltip, BarElement, CategoryScale, LogarithmicScale } from 'chart.js'
import { ref, computed } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { type MetaPageContainerAlert } from '@metaplay/meta-ui'
import { MActionModalButton, MButton, MErrorCallout, MList, MListItem, MTextButton } from '@metaplay/meta-ui-next'
import { fetchSubscriptionDataOnceOnly, useSubscription } from '@metaplay/subscriptions'

import MetaGeneratedForm from '../components/generatedui/components/MetaGeneratedForm.vue'
import CoreUiPlacement from '../components/system/CoreUiPlacement.vue'

import { getSingleMatchmakerSubscriptionOptions, getTopPlayersOfSingleMatchmakerSubscriptionOptions } from '../subscription_options/matchmaking'
import { getSinglePlayerSubscriptionOptions } from '../subscription_options/players'
import useHeaderbar from '../useHeaderbar'

ChartJS.register(Title, Tooltip, BarElement, CategoryScale, LogarithmicScale)

const props = defineProps<{
  /**
   * ID of the matchmaker to display.
   */
  matchmakerId: string
}>()

/**
 * Subscribe to the data, error and refresh of a single matchmaker based on its id.
 */
const {
  data: singleMatchmakerData,
  error: singleMatchmakerError,
  refresh: singleMatchmakerRefresh
} = useSubscription(getSingleMatchmakerSubscriptionOptions(props.matchmakerId))

// Update the headerbar title dynamically as data changes.
useHeaderbar().setDynamicTitle(singleMatchmakerData, (singleMatchmakerData) => `Manage ${(singleMatchmakerData.value)?.data.name || 'Matchmaker'}`)

// Top players.
const {
  refresh: topPlayersRefresh,
} = useSubscription(getTopPlayersOfSingleMatchmakerSubscriptionOptions(props.matchmakerId))

// Reset modal.
async function resetMatchmaker () {
  await useGameServerApi().post(`matchmakers/${props.matchmakerId}/reset`)
  singleMatchmakerRefresh()
  topPlayersRefresh()
}

// Rebalance modal.
async function rebalanceMatchmaker () {
  await useGameServerApi().post(`matchmakers/${props.matchmakerId}/rebalance`)
  showSuccessToast(`${props.matchmakerId} rebalanced successfully.`)
  singleMatchmakerRefresh()
}

// Simulation modal.
const simulationData = ref(null)
const isSimulationFormValid = ref(false)
const simulationResult = ref<any>(null)
const previewCandidateData = ref()
const isSimulationRunning = ref(false)

async function simulateMatchmaking () {
  try {
    isSimulationRunning.value = true

    // Fetch simulation results.
    const response = await useGameServerApi().post(`matchmakers/${props.matchmakerId}/test`, simulationData.value)
    simulationResult.value = response.data

    if (simulationResult.value.response?.responseType === 'Success') {
      // Fetch data for the best matching player.
      const previewCandidateId = simulationResult.value.response.bestCandidate
      previewCandidateData.value = await fetchSubscriptionDataOnceOnly(getSinglePlayerSubscriptionOptions(previewCandidateId))
    }

    isSimulationRunning.value = false
  } catch (e) {
    simulationResult.value = {
      response: {
        data: {
          error: e
        }
      }
    }
  }
}

const scanningErrorRate = computed((): number => {
  if (singleMatchmakerData.value?.data.scannedPlayersCount && singleMatchmakerData.value?.data.playerScanErrorCount) {
    return singleMatchmakerData.value?.data.playerScanErrorCount / singleMatchmakerData.value?.data.scannedPlayersCount * 100
  } else {
    return 0
  }
})

/**
 * Array of messages to be displayed at the top of the page.
 */
const alerts = computed(() => {
  const allAlerts: MetaPageContainerAlert[] = []
  // Fixed warning threshold of 5%.
  if (scanningErrorRate.value > 5) {
    allAlerts.push({
      title: 'Warning: Errors encountered',
      message: `The matchmaker encountered ${scanningErrorRate.value.toFixed(2)}% error rate while scanning for players (${singleMatchmakerData.value?.data.playerScanErrorCount} out of ${singleMatchmakerData.value?.data.scannedPlayersCount} players scanned). See the server logs for more information.`,
      variant: 'warning'
    })
  }
  return allAlerts
})

</script>
