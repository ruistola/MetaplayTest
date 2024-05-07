<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MActionModalButton(
  modal-title="Join an Experiment"
  :action="joinOrUpdateExperiment"
  trigger-button-label="Join Experiment"
  trigger-button-full-width
  variant="warning"
  :ok-button-label="okButtonText"
  :ok-button-disabled="!okButtonEnabled"
  @show="resetModal"
  permission="api.players.edit_experiment_groups"
  data-testid="action-join-experiment"
  )
  template(#default)
    //- Button to display the modal
    meta-alert(
      v-if="!experimentsAvailable"
      title="No Experiments Available"
      )
      | There are no active experiments available in this environment. First, set them up in your game configs and then configure them from the #[MTextButton(to="/experiments") experiments page].

    div(v-else)
      div(class="tw-mb-3") You can manually enroll #[MBadge {{ playerData.model.playerName }}] in an active experiment, or change their variant in an experiment they are already in.
      div(class="tw-text-neutral-500 tw-text-xs+ tw-mb-3") Note: Players can never leave experiments once enrolled, but you can always change their variant. Moving a player to the control group has the same effect as removing a player from an experiment.

      div(class="tw-mb-3")
        div(class="tw-font-semibold tw-mb-1") Experiment
        meta-input-select(
          :value="experimentFormInfo.experimentId ?? 'none'"
          @input="updateExperimentSelection"
          :options="experimentOptions"
          no-clear
          ).mb-3

      div(class="tw-mb-3")
        div(class="tw-font-semibold") Variant
        meta-input-select(
          :value="experimentFormInfo.variantId ?? 'none'"
          @input="experimentFormInfo.variantId = $event"
          :options="variantOptions"
          :disabled="!experimentFormInfo.experimentId"
          no-clear
          )
        div(class="tw-text-neutral-500 tw-text-xs+ tw-mt-3" v-if="experimentMessage" title="" variant="neutral") {{ experimentMessage }}

      div(class="tw-flex tw-justify-between")
        div(class="tw-font-semibold") Tester
        MInputSwitch(
          :model-value="experimentFormInfo.isTester"
          @update:model-value="experimentFormInfo.isTester = $event"
          :disabled="!experimentFormInfo.experimentId"
          name="isPlayerTester"
          size="sm"
          )
      div(class="tw-text-neutral-500 tw-text-xs+ tw-mb-3") As a tester, this player can try out the experiment before it is enabled for everyone. This is a great way to test variants before rolling them out!

      meta-no-seatbelts(
        v-if="okButtonEnabled"
        message="Enrolling a player to an experiment or modifying the variant will force the player to reconnect!"
        )
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast, type MetaInputSelectOption } from '@metaplay/meta-ui'
import { MBadge, MInputSwitch, MActionModalButton, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getAllExperimentsSubscriptionOptions } from '../../../subscription_options/experiments'
import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'

const props = defineProps<{
  /**
   * Id of the player to target the change action at.
   */
  playerId: string
}>()

/** Access to the pre-configured HTTP client. */
const gameServerApi = useGameServerApi()

/** Subscribe to target player's data. */
const {
  data: playerData,
  refresh: playerRefresh
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

/** Subscribe to experiment data. */
const {
  data: experimentsData
} = useSubscription(getAllExperimentsSubscriptionOptions())

/**
 * Type definition for the information collected on this form.
 */
interface ExperimentFormInfo {
  experimentId: string | null
  variantId: string | null
  isTester: boolean
}

/**
 * Experiment details collected using this form.
 */
const experimentFormInfo = ref(getNewExperimentFormInfo())

/**
 * Data needed to initialize the form.
 */
function getNewExperimentFormInfo (): ExperimentFormInfo {
  return {
    experimentId: null,
    variantId: null,
    isTester: false,
  }
}

/**
 * Checks that the experiments data has active experiments for the target player to join.
 */
const experimentsAvailable = computed((): boolean => {
  return experimentsData?.value.experiments.length
})

/**
 * All experiments that the target player is enrolled in.
 */
const playerExperiments = computed(() => {
  return playerData.value.experiments
})

/**
 * Experiment options that are to be selected from the dropdown.
 */
const experimentOptions = computed((): Array<MetaInputSelectOption<string>> => {
  // Find experiments that are in a phase where the player is able to join.
  const experiments = Object.values(experimentsData.value.experiments)
    .filter((experiment: any) => ['Inactive', 'Testing', 'Ongoing', 'Paused'].includes(experiment.phase) && experiment.whyInvalid === null)

  // Create a list for the dropdown, including a "blank" option for when nothing is selected.
  const options: Array<MetaInputSelectOption<string>> = [
    { value: 'none', id: 'Select an experiment', disabled: true },
    ...experiments.map((experiment: any) => {
      return { value: experiment.experimentId, id: experiment.displayName }
    })
  ]

  return options
})

/**
 * Check if target player is enrolled in an experiment.
 */
const alreadyInSelectedExperiment = computed(() => {
  if (experimentFormInfo.value.experimentId === null) {
    return false
  }
  return Object.keys(playerData.value.model.experiments.experimentGroupAssignment).includes(experimentFormInfo.value.experimentId)
})

/**
 * Check if target player is already enrolled in an experiment variant.
 */
const alreadyInSelectedVariant = computed(() => {
  if (experimentFormInfo.value.variantId === null || experimentFormInfo.value.experimentId === null) {
    return false
  }
  const newVariantId = experimentFormInfo.value.variantId === 'Control group' ? null : experimentFormInfo.value.variantId
  return playerData.value.model.experiments.experimentGroupAssignment[experimentFormInfo.value.experimentId]?.variantId === newVariantId
})

/**
 * Text displayed if the target player is already a member of a selected experiment.
 */
const experimentMessage = computed(() => {
  if (alreadyInSelectedExperiment.value) {
    return `${playerData.value.model.playerName} is already a member of this experiment and is in the ${experimentFormInfo.value?.variantId} variant.`
  } else {
    return undefined
  }
})

/**
 * Text to be displayed on the 'Ok' button.
 */
const okButtonText = computed((): string => {
  if (alreadyInSelectedVariant.value) {
    return 'Modify variant and tester status'
  } else if (experimentFormInfo.value.experimentId && !alreadyInSelectedVariant.value) {
    return 'Set variant'
  } else {
    return 'Enroll in experiment'
  }
})

/**
 * Enables the 'Ok' button and the 'noSeatBelts' warning when a valid experiment variant is selected.
 */
const okButtonEnabled = computed((): boolean => {
  if (!experimentFormInfo.value.experimentId || !experimentFormInfo.value.variantId) {
    return false
  } else if (alreadyInSelectedExperiment.value && alreadyInSelectedVariant.value) {
    return playerExperiments.value[experimentFormInfo.value.experimentId]?.isPlayerTester !== experimentFormInfo.value.isTester
  } else return true
})

/**
 All available variants to be selected on the form dropdown.
 */
const variantOptions = ref<Array<MetaInputSelectOption<string>>>([])

/**
 * Update the selected experiment and/or variant option(s).
 */
async function updateExperimentSelection (newSelection: string) {
  experimentFormInfo.value.experimentId = newSelection

  const isTesterExperiments = Object.entries(playerExperiments.value).filter(([key, value]: any) => value.isPlayerTester)
  experimentFormInfo.value.isTester = isTesterExperiments.find(([key, value]) => key === experimentFormInfo.value.experimentId) !== undefined

  // Prefill variantId if player is already in selected experiment.
  if (alreadyInSelectedExperiment.value) {
    experimentFormInfo.value.variantId = playerData.value.model.experiments.experimentGroupAssignment[experimentFormInfo.value.experimentId]?.variantId || 'Control group'
  } else {
    experimentFormInfo.value.variantId = null
  }

  // Fetch the experiment details so that we can get the list of variants.
  variantOptions.value = []
  if (experimentFormInfo.value.experimentId === 'none' || !experimentFormInfo.value.experimentId) return

  const response = await gameServerApi.get(`/experiments/${experimentFormInfo.value.experimentId}`)
  const options = Object.keys(response.data.state.variants).map((item) => ({ value: item, id: item }))

  variantOptions.value = [
    { value: 'none', id: 'Select an experiment variant', disabled: true },
    { value: 'Control group', id: 'Control group' },
    ...options
  ]
}

/**
 * Join or update a selected experiment.
 */
async function joinOrUpdateExperiment () {
  const message = alreadyInSelectedExperiment.value ? `${playerData.value.model.playerName} changed experiment variant.` : `${playerData.value.id} enrolled into the experiment.`
  const newVariantId = experimentFormInfo.value.variantId === 'Control group' ? null : experimentFormInfo.value.variantId
  await gameServerApi.post(`/players/${playerData.value.id}/changeExperiment`, { ExperimentId: experimentFormInfo.value.experimentId, VariantId: newVariantId, IsTester: experimentFormInfo.value.isTester })
  showSuccessToast(message)
  playerRefresh()
}

/**
 * Reset the modal.
 */
function resetModal () {
  experimentFormInfo.value = getNewExperimentFormInfo()
  variantOptions.value = [{ value: 'none', id: 'Select an experiment' }]
}
</script>
