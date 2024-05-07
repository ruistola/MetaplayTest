<template lang="pug">
MActionModalButton(
  :modal-title="`${buttonText} ${itemName}`"
  :action="createOrUpdateBroadcast"
  :trigger-button-label="buttonText"
  :trigger-button-disabled="disabled"
  :ok-button-label="editBroadcast ? 'Update Broadcast' : 'Start Broadcast'"
  :action-button-text="buttonText"
  :ok-button-disabled="!isFormValid"
  modal-size="large"
  permission="api.broadcasts.edit"
  :data-testid="dataTestid"
  @show="resetNewBroadcast"
  )
  //- Trigger button icon
  template(#trigger-button-icon)
    fa-icon(v-if="buttonIcon" :icon="buttonIcon" class="tw-h-3.5 tw-w-4 tw-mb-[0.05rem] tw-space-x-2")

  //- Modal default content
  div(class="tw-@container")
    MInputText(
      label="Name"
      :model-value="broadcastFormInfo.name"
      @update:model-value="broadcastFormInfo.name = $event"
      :variant="isNameValid ? 'success' : 'danger'"
      :placeholder="`${itemName} name here...`"
      class="mb-3"
      )

    MInputStartDateTimeAndDuration(
      :startDateTime="broadcastFormInfo.startAt"
      :duration="broadcastFormInfo.duration"
      @update:startDateTime="broadcastFormInfo.startAt = $event"
      @update:duration="broadcastFormInfo.duration = $event"
      class="mb-3"
      )

    div.mb-3
      h6 Audience
      message-audience-form(
        :value="broadcastFormInfo.audience"
        @input="broadcastFormInfo.audience = $event"
        )

    div(v-if="triggeringSupported").mb-3
      h6 Triggering
      trigger-condition-form(
        :value="broadcastFormInfo.triggerCondition"
        @input="broadcastFormInfo.triggerCondition = $event"
        )

    meta-generated-form(
      :typeName="broadcastType"
      :value="broadcastFormInfo.contents"
      @input="broadcastFormInfo.contents = $event"
      @status="contentsValid = $event"
      :page="'BroadcastForm'"
      class="tw-relative tw-z-0"
      )
</template>

<script lang="ts" setup>
import { DateTime, Duration } from 'luxon'
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'

import MessageAudienceForm from './MessageAudienceForm.vue'
import TriggerConditionForm from './TriggerConditionForm.vue'
import MetaGeneratedForm from '../generatedui/components/MetaGeneratedForm.vue'

import { useRouter } from 'vue-router'

import type { BroadcastInfo, TargetingOptions, TriggerInfo } from './mailUtils'
import { MActionModalButton, MInputStartDateTimeAndDuration, MInputText } from '@metaplay/meta-ui-next'

const props = withDefaults(defineProps<{
  /**
   * Text for the modal's action button. Should be kept as short as possible.
   * @example 'New campaign'
   */
  buttonText: string
  /**
   *  Optional: A Font-Awesome icon to place in the action button.
   * @example 'paper-plane'
   */
  buttonIcon?: string
  /**
   * Optional: Existing broadcast content which is to be edited or duplicated.
   */
  prefillData?: BroadcastInfo
  /**
   * Optional: If true then edit an existing broadcast.
   */
  editBroadcast?: boolean
  /**
   * Optional: C# type name of the class
   */
  broadcastType?: string
  /**
   * Optional: Display name to be rendered on this modal.
   */
  itemName?: string
  /**
   * Optional:
   */
  triggeringSupported?: boolean
  /**
   * Optional: Whether to disable the action button.
   */
  disabled?: boolean
  /**
   * Optional: Cypress label for automated testing.
   */
  dataTestid?: string
}>(), {
  prefillData: undefined,
  buttonIcon: undefined,
  broadcastType: 'Metaplay.Server.BroadcastMessageContents',
  itemName: 'Broadcast',
  dataTestid: undefined,
})

const gameServerApi = useGameServerApi()
const router = useRouter()

const emits = defineEmits(['refresh'])

// Form data and validation -------------------------------------------------------------------------------------------

/**
 * Broadcast data collected on this form.
 */
interface BroadcastFormInfo {
  name: string
  audience: TargetingOptions
  startAt: DateTime
  duration: Duration
  contents: object
  triggerCondition?: TriggerInfo
}

/**
 * Broadcast details to be collected using this form.
 */
const broadcastFormInfo = ref(getNewBroadcastFormInfo())

/**
 * Return initial data for a new broadcast.
 */
function getNewBroadcastFormInfo (): BroadcastFormInfo {
  return {
    name: '',
    audience: MessageAudienceForm.props.value.default(),
    startAt: DateTime.now(),
    duration: Duration.fromObject({ days: 1 }),
    // endAt: getDefaultEndDate(DateTime.now()),
    contents: {},
    triggerCondition: undefined
  }
}

// /**
//  * Calculates the default end date based on the given start date and time.
//  * By default, this is set 1 day after the start date.
//  * @param startDate The start date for the broadcast.
//  */
// function getDefaultEndDate (startDate: DateTime): DateTime {
//   return startDate.plus(Duration.fromObject({ days: 1 }))
// }

/**
 * Checks that the name field is not empty.
 */
const isNameValid = computed(() => {
  // Name should not be empty.
  if (!broadcastFormInfo.value.name || broadcastFormInfo.value.name.length === 0) {
    return false
  } else {
    return true
  }
})

/**
 * Checks that the form input fields have valid values.
 */
const isFormValid = computed(() => {
  if (!isNameValid.value) return false
  if (broadcastFormInfo.value.duration.toMillis() === 0) return false
  if (!broadcastFormInfo.value.audience.valid) return false
  if (!contentsValid.value) return false
  return true
})

// Modify/create broadcast-----------------------------------------------------------------------------------------

/**
 * Send the updated or new broadcast payload to the server.
 */
async function createOrUpdateBroadcast () {
  const payload: BroadcastInfo = {
    name: broadcastFormInfo.value.name,
    startAt: broadcastFormInfo.value.startAt.toISO(),
    endAt: broadcastFormInfo.value.startAt.plus(broadcastFormInfo.value.duration).toISO(),
    targetPlayers: broadcastFormInfo.value.audience.targetPlayers,
    targetCondition: broadcastFormInfo.value.audience.targetCondition,
    contents: broadcastFormInfo.value.contents,
    triggerCondition: broadcastFormInfo.value.triggerCondition
  }

  // If updating an existing broadcast...
  if (props.prefillData && props.editBroadcast) {
    // ...update.
    payload.id = props.prefillData.id
    await gameServerApi.put(`/broadcasts/${payload.id}`, payload)
    const message = `${props.itemName} with id ${props.prefillData.id} updated.`
    showSuccessToast(message)
    emits('refresh')

    // TODO: force update the subscription.
  } else {
    // ...or else create a new one.
    const created = (await gameServerApi.post('/broadcasts', payload)).data
    const message = `New ${props.itemName} created with id ${created.id}.`
    showSuccessToast(message)
    emits('refresh')

    // Navigate back to broadcast list if we just created a duplicate (and thus are in the details page)
    // TODO: should emit an event instead and let the parent page react to this event instead of guessing the right contextual behavior here!
    if (props.prefillData) {
      await router.push('/broadcasts')
    }

    // TODO: force update the broadcast list subscription.
  }
}

/**
 * Check if the contents of broadcast message fields are valid.
 */
const contentsValid = ref(false)

/** Reset the modal */
function resetNewBroadcast () {
  // Initialize with default values if an existing broadcast was given.
  if (props.prefillData) {
    broadcastFormInfo.value.name = props.editBroadcast ? props.prefillData.name : props.prefillData.name + ' (copy)'
    broadcastFormInfo.value.audience.targetPlayers = props.prefillData.targetPlayers
    broadcastFormInfo.value.audience.targetCondition = props.prefillData.targetCondition
    broadcastFormInfo.value.startAt = props.editBroadcast && props.prefillData.startAt !== null ? DateTime.fromISO(props.prefillData.startAt) : DateTime.now()
    // broadcastFormInfo.value.endAt = props.editBroadcast && props.prefillData.endAt !== null ? DateTime.fromISO(props.prefillData.endAt) : getDefaultEndDate(DateTime.now())
    broadcastFormInfo.value.duration = props.editBroadcast && props.prefillData.endAt && props.prefillData.startAt !== null ? DateTime.fromISO(props.prefillData.endAt).diff(DateTime.fromISO(props.prefillData.startAt), ['days', 'minutes', 'hours']) : Duration.fromObject({ days: 1 })
    broadcastFormInfo.value.contents = props.prefillData.contents
    broadcastFormInfo.value.triggerCondition = props.prefillData.triggerCondition
    contentsValid.value = true
  } else {
    broadcastFormInfo.value = getNewBroadcastFormInfo()
  }
}

</script>
