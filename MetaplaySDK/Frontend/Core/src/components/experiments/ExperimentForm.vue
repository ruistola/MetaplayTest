<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MButton(
  @click="modal?.open()"
  permission="api.experiments.edit"
  :disabled="phase === 'Concluded'"
  ) {{ phase === 'Testing' || phase === 'Concluded'  ? 'Configure' : 'Reconfigure'}}

MActionModal(
  ref="modal"
  :title="modalTitle"
  :action="updateConfiguration"
  ok-button-label="Update configuration"
  :ok-button-disabled="!isFormValid"
  @show="resetForm"
  )
  div.mb-4
    meta-alert(v-if="modalText" title='' variant='info') {{ modalText }}

    h6 Rollout Settings
    div.p-3.bg-light.rounded.border
      b-row(align-h="between" no-gutters)
        span.font-weight-bold Rollout Enabled
        MInputSwitch(
          :model-value="rolloutEnabled"
          @update:model-value="rolloutEnabled = $event"
          class="tw-relative tw-top-1 tw-mr-1"
          name="rolloutEnabled"
          size="sm"
          )
      hr(v-show="rolloutEnabled")
      b-row(v-show="rolloutEnabled")
        b-col(md).mb-2
          MInputNumber(
            label="Rollout %"
            :model-value="rolloutPercentage"
            @update:model-value="rolloutPercentage = $event"
            :min="0"
            :max="100"
            )
        b-col(md).mb-2
          MInputNumber(
            label="Capacity Limit"
            :model-value="maxCapacity"
            @update:model-value="maxCapacity = $event"
            :min="0"
            placeholder="Unlimited"
            clearOnZero
            )
      div.small.text-muted.mt-1
        span(v-if="rolloutEnabled") With rollout enabled, a percentage of your player base will be able to join the experiment, up to the optional capacity limit.
        span(v-else) Players will not be able to join an experiment with rollout disabled. You can use this to manually close an experiment to new players.

  div.mb-4
    h6 Audience
    message-audience-form.mb-2(v-model="audience" :isPlayerTargetingSupported="false")
    b-row(align-h="between" no-gutters)
      div.font-weight-bold Account Age
      MInputSingleSelectRadio(
        :model-value="enrollTrigger"
        @update:model-value="enrollTrigger = $event"
        :options="enrollTriggerOptions"
        size="small"
        )
    div.small.text-muted.mb-3(v-if="enrollTrigger !== 'Login'") Players can join the experiment at the time of account creation only.
    div.small.text-muted.mb-3(v-else) Players can join the experiment the next time they login.

  div.mb-4
    h6 Variant Rollout Percentages
    b-container.border.rounded.bg-light.p-3
      b-row(cols="1" cols-md="3")
        b-col(v-for="(value, key) in variantWeights" :key="key").mb-2
          MInputNumber(
            :label="key + ' %'"
            :model-value="value.weight"
            @update:model-value="value.weight = $event; updateVariantWeights(key)"
            :min="0"
            :variant="totalWeights !== 100 ? 'danger' : 'default'"
            )
      div.small.text-danger(v-if="totalWeights !== 100") Variant weights do not add up to 100%. #[b-link(@click="balanceVariantWeights()") Balance automatically]?
      div.small.text-warning(v-if="variantWeights && parseInt(variantWeights.Control.weight) === 0") Empty control group! Validating this experiment's results may not be possible.

</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModal, MButton, MInputSwitch, MInputSingleSelectRadio, MInputNumber } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getSingleExperimentSubscriptionOptions } from '../../subscription_options/experiments'
import MessageAudienceForm from '../mails/MessageAudienceForm.vue'

const props = defineProps<{
  experimentId: string
}>()

// Modal ---------------------------------------------------------------
/**
 * Reference to the modal component
 */
const modal = ref<typeof MActionModal>()

/**
 * The title of the modal.
 */
const modalTitle = computed(() => {
  if (phase.value === 'Testing' || phase.value === 'Concluded') {
    return 'Configure Experiment'
  } else {
    return 'Reconfigure Experiment'
  }
})

/**
 * The text to be shown in the modal.
 */
const modalText = computed(() => {
  if (phase.value === 'Testing' || phase.value === 'Concluded') {
    return undefined
  } else {
    return 'Reconfiguring an experiment after it has been rolled out may not achieve the expected result and is therefore generally discouraged. For example, decreasing rollout % or capacity limit will have no effect if those limits have already been reached. Changing targeting or weights may make it harder to analyse the experiment results.'
  }
})

function open () {
  modal.value?.open()
}

defineExpose({
  open,
})

/**
 * The base data for the current experiment.
 */
const {
  data: singleExperimentData,
  refresh: singleExperimentRefresh,
  error: singleExperimentError
} = useSubscription(getSingleExperimentSubscriptionOptions(props.experimentId || ''))

/**
 * The current lifecycle phase of the experiment.
 */
const phase = computed(() => singleExperimentData.value.state.lifecyclePhase)

/**
 * The data detailing the current state of the experiment.
 */
const {
  data: experimentInfoData,
  refresh: experimentInfoRefresh,
} = useSubscription(getSingleExperimentSubscriptionOptions(props.experimentId))

/**
 * Indicates whether the current experiment is currently active (rolled).
 */
const rolloutEnabled = ref(false)

/**
 * The percentage of players that will be enrolled n the experiment.
 */
const rolloutPercentage = ref<any>(null)

/**
 * The trigger that will enroll players in the experiment.
 * By default players are enrolled when they login.
 */
const enrollTrigger = ref<'Login' | 'NewPlayers'>('Login')

/**
 * The options for when players are enrolled in the experiment.
 */
const enrollTriggerOptions: Array<{ label: string, value: 'Login' | 'NewPlayers' }> = [
  { label: 'Everyone', value: 'Login' },
  { label: 'New players', value: 'NewPlayers' },
]

/**
 * Indicates whether the experiment has a capacity to enroll more players.
 */
const hasCapacityLimit = computed((): boolean => maxCapacity.value !== undefined && maxCapacity.value > 0)

/**
 * The maximum number of players that can be enrolled in the experiment.
 */
const maxCapacity = ref<number | undefined>(10000)

/**
 * The weights of each of the variants in the experiment.
 */
const variantWeights = ref<any>(undefined)

/**
 * The total weight of all the variants.
 */
const totalWeights = ref(0)

/**
 * The audience that will be targeted by the experiment.
 */
const audience = ref<any>(MessageAudienceForm.props.value.default())

/**
 * Indicates whether the form is valid and can be submitted.
 */
const isFormValid = computed(() => {
  if (rolloutPercentage.value === undefined) return false
  if (!variantWeights.value || !Object.keys(variantWeights.value).every(variantId => validateVariantWeight(variantId))) return false
  return true
})

/**
 * Update the weights assigned to each variant in the experiment.
 * @param changedVariantId The id of the variant that was changed.
 */
function updateVariantWeights (changedVariantId: any) {
  if (variantWeights.value[changedVariantId].weight === '' || !variantWeights.value[changedVariantId].weight || parseInt(variantWeights.value[changedVariantId].weight) < 0) {
    variantWeights.value[changedVariantId].weight = 0
  }

  // Round all
  for (const variant of Object.values(variantWeights.value) as any) {
    variant.weight = Math.round(variant.weight)
  }

  totalWeights.value = Object.values(variantWeights.value).reduce((sum: number, variant: any) => sum + parseInt(variant.weight), 0)
}

/**
 * Balance the weights of the variants so that they add up to 100%.
 */
function balanceVariantWeights () {
  // Count
  let totalWeightsTemp = Object.values(variantWeights.value).reduce((sum: number, variant: any) => sum + parseInt(variant.weight), 0)

  // Gracefully handle edge case where all variants have 0 weight
  if (totalWeightsTemp === 0) {
    for (const key in variantWeights.value) {
      variantWeights.value[key].weight = 1
      totalWeightsTemp++
    }
  }

  // Redistribute if needed
  if (totalWeightsTemp !== 100) {
    for (const variant of Object.values(variantWeights.value) as any) {
      variant.weight = Math.round(variant.weight / totalWeightsTemp * 100)
    }

    // Finally adjust control group to fix rounding errors
    totalWeightsTemp = Object.values(variantWeights.value).reduce((sum: number, variant: any) => sum + parseInt(variant.weight), 0)
    if (totalWeightsTemp !== 100) {
      variantWeights.value.Control.weight += (100 - totalWeightsTemp)
    }
  }

  totalWeights.value = Object.values(variantWeights.value).reduce((sum: number, variant: any) => sum + parseInt(variant.weight), 0)
}

/**
 * Validates that the total weight of the variants is 100%.
 * @param variantId The id of the variant to validate.
 */
function validateVariantWeight (variantId: any) {
  const value = parseInt(variantWeights.value[variantId].weight)
  if (isNaN(value)) return false
  if (value < 0 || totalWeights.value !== 100) return false
  return true
}

/**
 * Resets the form to the current experiment configuration.
 */
function resetForm () {
  rolloutEnabled.value = !experimentInfoData.value.state.isRolloutDisabled
  rolloutPercentage.value = experimentInfoData.value.state.rolloutRatioPermille / 10
  enrollTrigger.value = experimentInfoData.value.state.enrollTrigger
  maxCapacity.value = experimentInfoData.value.state.hasCapacityLimit ? experimentInfoData.value.state.maxCapacity : null

  variantWeights.value = {
    Control: { weight: experimentInfoData.value.state.controlWeight }
  }
  Object.entries(experimentInfoData.value.state.variants).forEach(([id, data]) => {
    variantWeights.value[id] = {
      weight: (data as any).weight
    }
  })
  balanceVariantWeights()
  audience.value = MessageAudienceForm.props.value.default()
  audience.value.targetCondition = experimentInfoData.value.state.targetCondition
}

const gameServerApi = useGameServerApi()
const emits = defineEmits(['ok'])

/**
 * Updates the experiment configuration on the server.
 */
async function updateConfiguration () {
  const config = {
    isRolloutDisabled: !rolloutEnabled.value,
    enrollTrigger: enrollTrigger.value,
    hasCapacityLimit: hasCapacityLimit.value,
    maxCapacity: hasCapacityLimit.value ? maxCapacity.value : null,
    rolloutRatioPermille: rolloutPercentage.value * 10,
    variantWeights: Object.fromEntries(Object.keys(variantWeights.value).map(key => [key === 'Control' ? null : key, variantWeights.value[key].weight])),
    variantIsDisabled: null,
    targetCondition: audience.value.targetCondition
  }
  await gameServerApi.post(`/experiments/${props.experimentId}/config`, config)
  showSuccessToast('Configuration set.')
  experimentInfoRefresh()
  emits('ok')
}
</script>
