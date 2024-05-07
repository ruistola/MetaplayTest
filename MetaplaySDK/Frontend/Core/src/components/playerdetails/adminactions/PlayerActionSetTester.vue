<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MTextButton(
  v-if="playerData"
  @click="setTesterModal?.open()"
  :variant="experiments.isPlayerTester ? 'warning' : undefined"
  permission="api.players.edit_experiment_groups"
  :data-testid="`set-action-tester-${experimentId}`"
  ) {{ experiments.isPlayerTester ? 'Remove as tester' : 'Mark as tester' }}

  //- Set tester modal.
  MActionModal(
    ref="setTesterModal"
    title="Manage Tester status"
    :action="updateTesterStatus"
    ok-button-label="Save Settings"
    :ok-button-disabled="!isStatusChanged"
    :data-testid="`set-action-tester-modal-${experimentId}`"
    )
    div(class="tw-flex tw-justify-between")
      span(class="tw-font-bold") Tester status
      MInputSwitch(
        :model-value="isTester"
        @update:model-value="isTester = $event"
        class="tw-relative tw-top-1 tw-mr-3"
        name="isPlayerTester"
        size="sm"
        data-testid="tester-status-toggle"
        )
    span(class="tw-text-neutral-500 tw-text-sm") As a tester, this player can try out the experiment before it is enabled for everyone. This is a great way to test variants before rolling them out!

</template>

<script lang="ts" setup>
import { computed, ref, onMounted } from 'vue'
import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModal, MInputSwitch, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'

const props = defineProps<{
  /**
   * ID of the player to set as tester.
   */
  playerId: string
  /**
   * ID of the experiment to assign the player as a tester.
   */
  experimentId: string
}>()

const gameServerApi = useGameServerApi()

/** Subscribe to the target player's data. */
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

/**
 * Whether the player is a tester in the experiment.
 */
const isTester = ref(false)

/**
 * Reference to the modal for setting the tester status.
 */
const setTesterModal = ref<typeof MActionModal>()

/**
 * Get the player's experiment data.
 */
const experiments = computed(() => {
  if (playerData) {
    return playerData.value.experiments[props.experimentId]
  }
  return undefined
})

/**
 * Check if the player's tester status has changed.
 */
const isStatusChanged = computed(() => {
  return isTester.value !== experiments.value?.isPlayerTester
})

/**
 * Set the initial tester status.
 */
onMounted(() => {
  isTester.value = experiments.value?.isPlayerTester ?? false
})

const toastMessage = computed(() => {
  if (isTester.value) {
    return `${playerData.value.model.playerName} is now a tester in ${props.experimentId}`
  }
  return `${playerData.value.model.playerName} is no longer a tester in ${props.experimentId}`
})

/**
 * Update the player's tester status.
 */
async function updateTesterStatus () {
  let toastMessage
  if (isTester.value) {
    toastMessage = `${playerData.value.model.playerName} is now a tester in ${props.experimentId}`
  } else toastMessage = `${playerData.value.model.playerName} is no longer a tester in ${props.experimentId}`
  await gameServerApi.post(`/players/${playerData.value.id}/changeExperiment`, { ExperimentId: props.experimentId, VariantId: experiments.value.enrolledVariant, IsTester: isTester.value })
  showSuccessToast(toastMessage)
  playerRefresh()
}

</script>
