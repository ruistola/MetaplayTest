<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MActionModalButton(
  :modal-title="populateFormMode().modalTitle"
  :action="async () => { await populateFormMode().action(false); return }"
  :trigger-button-label="populateFormMode().triggerButtonLabel"
  :ok-button-label="populateFormMode().okButtonLabel"
  :ok-button-disabled="!formValid"
  @show="reset()"
  permission="api.liveops_events.edit"
  :data-testid="populateFormMode().dataTestid"
  )
  template(#default)
    div(class="tw-mb-3")
      div(class="tw-font-semibold tw-mb-1") High Level Type
      MInputSingleSelectDropdown(
        :model-value="params.eventType"
        @update:model-value="params.eventType = $event; params.templateId = ''"
        :options="liveOpsEventTypesData?.map((key) => ({ label: key.eventTypeName, value: key.contentClass })) || []"
        placeholder="Select Event Type"
        :variant="formMode === 'edit' ? 'default' : params.eventType ? 'success' : 'danger'"
        :disabled="formMode === 'edit'"
        )

    div(class="tw-mb-3")
      div(class="tw-font-semibold") Specific Template
      MInputSingleSelectDropdown(
        :model-value="params.templateId"
        @update:model-value="params.templateId = $event; updateContentFromTemplate()"
        :options="templateOptions"
        :disabled="!params.eventType"
        :variant="params.templateId ? 'success' : 'danger'"
        )
    MCollapseCard(title="Event Configuration")
      div(class="tw-mx-3 tw-mt-2")
        meta-generated-content(v-if="params.templateId" :value="params.content")
        div(v-else class="tw-text-red-500 tw-ml-1 tw-mb-3") No template selected. Please select a specific template to view its content.

    LiveOpsEventsModalRelatedCard(
      :relatedEvents="relatedEvents"
      :eventId="eventId"
      class="tw-mt-3"
      )

  template(#center-panel)
    MInputText(
      label="Event Name"
      :model-value="params.displayName"
      @update:model-value="params.displayName = $event"
      :variant="params.displayName.length > 0 ? 'success' : 'danger'"
      placeholder="For example: spring template event"
      class="tw-mb-2"
      )
    MInputTextArea(
      label="Event Description"
      :model-value="params.description"
      @update:model-value="params.description = $event"
      placeholder="For example: This event is for players who have completed the tutorial."
      )
    MessageAudienceForm(
      :value="targetingOptions"
      @input="targetingOptions = $event; params.targetPlayers = targetingOptions.targetPlayers; params.targetCondition = targetingOptions.targetCondition"
      class="tw-mt-3"
      )
  template(#right-panel)
    div(class="tw-flex")
      MInputSegmentedSwitch(
        label="Time Format"
        :model-value="schedule.isPlayerLocalTime ? 'local' : 'utc'"
        @update:model-value="$event === 'utc' ? schedule.isPlayerLocalTime = false : schedule.isPlayerLocalTime = true"
        :options="[{label: 'UTC', value: 'utc'}, {label: 'Local Time', value: 'local'}]"
        :disabled="timeFormatHint.inputDisabled"
        :hint-message="timeFormatHint.messageText"
        )
      span(class="tw-relative tw-ml-10")
        span(class="tw-mr-3 tw-font-semibold") Use Schedule
        MInputSwitch(
          :model-value="!!params.schedule"
          @update:model-value="params.schedule = $event ? schedule : null"
          size="sm"
          class="tw-relative tw-top-0.5"
          :disabled="useScheduleHint.inputDisabled"
          )
    div(class="tw-mt-1")
      MInputDateTime(
        label="Start Time"
        :model-value="DateTime.fromISO(schedule.enabledStartTime)"
        @update:model-value="schedule.enabledStartTime = $event.toISO() ?? ''"
        :disabled="startTimeHint.inputDisabled"
        :hint-message="startTimeHint.messageText"
        )
      MInputDateTime(
        label="End Time"
        :model-value="DateTime.fromISO(schedule.enabledEndTime)"
        @update:model-value="schedule.enabledEndTime = $event.toISO() ?? ''"
        :disabled="endTimeHint.inputDisabled"
        :hint-message="endTimeHint.messageText"
        )
      div(class="tw-mt-1 tw-text-xs+ tw-text-red-500") {{ startEndTimeError }}
    div(class="tw-mt-2")
      MInputDuration(
        label="Preview Duration"
        :model-value="Duration.fromISO(schedule.previewDuration ?? '')"
        @update:model-value="schedule.previewDuration = $event?.toISO() ?? 'P0Y0M0DT0H0M0S'"
        :disabled="previewHint.inputDisabled"
        :hint-message="previewHint.messageText"
        allowZeroDuration
        )
      MInputDuration(
        label="Ending Soon Duration"
        :model-value="Duration.fromISO(schedule.endingSoonDuration ?? '')"
        @update:model-value="schedule.endingSoonDuration = $event?.toISO() ?? 'P0Y0M0DT0H0M0S'"
        :disabled="endingSoonHint.inputDisabled"
        :hint-message="endingSoonHint.messageText"
        allowZeroDuration
        )
      MInputDuration(
        label="Review Duration"
        :model-value="Duration.fromISO(schedule.reviewDuration ?? '')"
        @update:model-value="schedule.reviewDuration = $event?.toISO() ?? 'P0Y0M0DT0H0M0S'"
        :disabled="reviewHint.inputDisabled"
        :hint-message="reviewHint.messageText"
        allowZeroDuration
        )
  template(#bottom-panel)
    ul
      li(v-for="diagnostic in validationDiagnostics" :class="['tw-py-2', {'tw-text-orange-500': diagnostic.level === 'Warning', 'tw-text-red-500': diagnostic.level === 'Error'}]")  {{ diagnostic.message }}
</template>

<script lang="ts" setup>
import { DateTime, Duration } from 'luxon'
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast, showErrorToast } from '@metaplay/meta-ui'
import { MActionModalButton, MInputSingleSelectDropdown, MInputText, MInputTextArea, MInputDateTime, MInputSegmentedSwitch, MInputSwitch, MCallout, MInputDuration, MCollapseCard } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { LiveOpsEventPhase } from '../../liveOpsEventServerTypes'
import { getLiveOpsEventTypesSubscriptionOptions } from '../../subscription_options/liveOpsEvents'
import type { LiveOpsEventParams, LiveOpsEventTypeInfo, LiveOpsEventScheduleInfo, CreateLiveOpsEventRequest, CreateLiveOpsEventResponse, UpdateLiveOpsEventRequest, UpdateLiveOpsEventResponse, LiveOpsEventBriefInfo, LiveOpsEventCreationDiagnostic } from '../../liveOpsEventServerTypes'
import MetaGeneratedContent from '../generatedui/components/MetaGeneratedContent.vue'
import { type TargetingOptions } from '../mails/mailUtils'
import MessageAudienceForm from '../mails/MessageAudienceForm.vue'

import LiveOpsEventsModalRelatedCard from './LiveOpsEventModalRelatedCard.vue'

const gameServerApi = useGameServerApi()
const emits = defineEmits(['refresh'])
const router = useRouter()

const props = withDefaults(defineProps<{
  /**
  * Optional: The mode in which the form is to be displayed. Defaults to 'create'.
   */
  formMode: 'create' | 'edit' | 'duplicate'
  /**
   * Optional: Existing LiveOps content which is to be edited or duplicated. Only used in modes 'edit' and 'duplicate'.
   */
  prefillData?: LiveOpsEventParams
  /**
   * Optional: EventId of the event we are editing. Only used in mode 'edit'.
   */
  eventId?: string
  currentPhase?: LiveOpsEventPhase
}>(), {
  formMode: 'create',
  prefillData: undefined,
  eventId: undefined,
  currentPhase: undefined
})

const {
  data: liveOpsEventTypesData
} = useSubscription<LiveOpsEventTypeInfo[]>(getLiveOpsEventTypesSubscriptionOptions())

const schedule = ref<LiveOpsEventScheduleInfo>(NewLiveOpsScheduleInfo())
const params = ref<LiveOpsEventParams>(NewLiveOpsEventCreationParams())
const targetingOptions = ref<TargetingOptions>({ targetPlayers: [], targetCondition: null, valid: true })
const formValid = ref<boolean>(false)

function NewLiveOpsEventCreationParams (): LiveOpsEventParams {
  return {
    displayName: '',
    description: '',
    eventType: '',
    templateId: '',
    content: {},
    schedule: NewLiveOpsScheduleInfo(),
    targetPlayers: [],
    targetCondition: null
  }
}

function NewLiveOpsScheduleInfo (): LiveOpsEventScheduleInfo {
  return {
    isPlayerLocalTime: false,
    enabledStartTime: '',
    enabledEndTime: '',
    previewDuration: 'P0Y0M0DT0H0M0S',
    endingSoonDuration: 'P0Y0M0DT0H0M0S',
    reviewDuration: 'P0Y0M0DT0H0M0S'
  }
}

// Form handling -------------------------------------------------------------------------

function populateFormMode () {
  if (props.formMode === 'edit') {
    return {
      modalTitle: 'Edit Live Ops Event',
      action: updateLiveOpsEvent,
      triggerButtonLabel: 'Edit',
      okButtonLabel: 'Save Changes',
      dataTestid: 'edit-event-form',
    }
  } else if (props.formMode === 'duplicate') {
    return {
      modalTitle: 'Duplicate Live Ops Event',
      action: createLiveOpsEvent,
      triggerButtonLabel: 'Duplicate',
      okButtonLabel: 'Create Duplicate',
      dataTestid: 'duplicate-event-form',
    }
  } else {
    return {
      modalTitle: 'Create New Live Ops Event',
      action: createLiveOpsEvent,
      triggerButtonLabel: 'New LiveOps Event',
      okButtonLabel: 'Create Event',
      dataTestid: 'create-event-form',
    }
  }
}

const selectedEventType = computed(() => {
  return liveOpsEventTypesData.value?.find((x) => x.contentClass === params.value.eventType)
})
const templateOptions = computed(() => {
  return Object.keys(selectedEventType.value?.templates ?? []).map((key) => ({ label: key, value: key }))
})

function updateContentFromTemplate () {
  if (selectedEventType.value) {
    params.value.content = selectedEventType.value.templates[params.value.templateId]
  }
}

/**
 * Dashboard client side validation mirroring some of the server side validation.
 * @param eventParams Form data to be validated.
 */
function eventParamsIsMissingRequiredData (eventParams: LiveOpsEventParams) {
  if (!eventParams.eventType || !eventParams.templateId || !eventParams.displayName || !targetingOptions.value.valid) {
    return true
  } else if (eventParams.schedule && (!eventParams.schedule.enabledStartTime || !eventParams.schedule.enabledEndTime)) {
    return true
  } else if (startEndTimeError.value) {
    return true
  } else {
    return false
  }
}

function reset () {
  if (props.prefillData) {
    // Take a deep clone of the input data so that we don't accidentally edit the wrong thing.
    params.value = JSON.parse(JSON.stringify(props.prefillData))
  } else {
    params.value = NewLiveOpsEventCreationParams()
  }
  schedule.value = params.value.schedule ?? NewLiveOpsScheduleInfo()
  targetingOptions.value = { targetPlayers: params.value.targetPlayers, targetCondition: params.value.targetCondition, valid: true }

  // mode specific initialization
  if (props.formMode === 'duplicate') {
    params.value.displayName = params.value.displayName + ' (copy)'
  }
}

/**
 * Watcher for the form validity. This watcher is responsible for triggering the server side validation call.
 */
watch(() => params.value, async (newParams, oldParams) => {
  formValid.value = false
  // client-side validity checks, before doing server validation request
  if (eventParamsIsMissingRequiredData(newParams)) { return }
  // server-side validation call
  formValid.value = await populateFormMode().action(true)
}, { deep: true })

// Server calls and related data initialization -------------------------------------------------------------------------

/**
 * Data for the related events card from validate only response.
 */
const relatedEvents = ref<LiveOpsEventBriefInfo[]>([])

/**
 * Error and warning messages from the validate only response.
 */
const validationDiagnostics = ref<LiveOpsEventCreationDiagnostic[]>([])

/**
 * Server call to create or duplicate an existing event. If validateOnly is true, the server will only validate the request and not create/update the event.
 */
async function createLiveOpsEvent (validateOnly: boolean) {
  const payload: CreateLiveOpsEventRequest = {
    validateOnly,
    parameters: params.value,
  }
  const created = (await gameServerApi.post<CreateLiveOpsEventResponse>('/createLiveOpsEvent', payload)).data
  if (!validateOnly) {
    if (created.isValid) {
      showSuccessToast(`Event "${params.value.displayName}" created with id ${created.eventId}.`)
    } else {
      showErrorToast('Event create failed!')
      // \TODO: show reason from created.diagnostics
    }
    emits('refresh')

    if (props.formMode === 'duplicate') {
      await router.push(`/liveOpsEvents/${created.eventId}`)
    }
  }
  relatedEvents.value = created.relatedEvents ?? []
  validationDiagnostics.value = created.diagnostics
  return created.isValid
}

/**
 * Server call to update an existing event. If validateOnly is true, the server will only validate the request and not update the event.
 */
async function updateLiveOpsEvent (validateOnly: boolean) {
  const payload: UpdateLiveOpsEventRequest = {
    validateOnly,
    occurrenceId: props.eventId ?? '',
    parameters: params.value,
  }
  const updated = (await gameServerApi.post<UpdateLiveOpsEventResponse>('/updateLiveOpsEvent', payload)).data
  if (!validateOnly) {
    if (updated.isValid) {
      showSuccessToast(`Event "${params.value.displayName}" updated.`)
    } else {
      showErrorToast('Event update failed!')
      // \TODO: show reason from updated.diagnostics
    }
    emits('refresh')
  }
  relatedEvents.value = updated.relatedEvents ?? []
  validationDiagnostics.value = updated.diagnostics
  return updated.isValid
}

// List of Hint Messages for editing an existing event -------------------------------------------------------------------------

interface HintMessage {
  messageText?: string
  inputDisabled: boolean
}

const timeFormatHint = computed((): HintMessage => {
  if (params.value.schedule === null) {
    return { inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.Active || props.currentPhase === LiveOpsEventPhase.EndingSoon)) {
    return { messageText: 'Modifying the time mode of an ongoing event is not allowed.', inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.InReview || props.currentPhase === LiveOpsEventPhase.Ended)) {
    return { messageText: 'Modifying the time mode of a past event is not allowed.', inputDisabled: true }
  } else {
    return { inputDisabled: false }
  }
})

const useScheduleHint = computed((): HintMessage => {
  if (props.formMode === 'edit' && props.currentPhase === LiveOpsEventPhase.Ended) {
    return { inputDisabled: true }
  } else {
    return { inputDisabled: false }
  }
})

const startTimeHint = computed((): HintMessage => {
  if (params.value.schedule === null) {
    return { inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.Active || props.currentPhase === LiveOpsEventPhase.EndingSoon)) {
    return { messageText: 'Modifying the start time of an ongoing event is not allowed.', inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.InReview || props.currentPhase === LiveOpsEventPhase.Ended)) {
    return { messageText: 'Modifying the start time of a past event is not allowed.', inputDisabled: true }
  } else {
    return { inputDisabled: false }
  }
})

const endTimeHint = computed((): HintMessage => {
  if (params.value.schedule === null) {
    return { inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.InReview || props.currentPhase === LiveOpsEventPhase.Ended)) {
    return { messageText: 'Modifying the end time of a past event is not allowed.', inputDisabled: true }
  } else {
    return { inputDisabled: false }
  }
})

// - Edge case scenarios still need to be fleshed out.
const startEndTimeError = computed(() => {
  if (params.value.schedule === null) {
    return undefined
  } else if (schedule.value.enabledStartTime === '' && props.currentPhase !== LiveOpsEventPhase.Active) {
    return 'Start time is required.'
  } else if (DateTime.fromISO(schedule.value.enabledStartTime) < DateTime.now().minus({ minutes: 10 }) && (props.formMode !== 'edit' && (props.currentPhase !== LiveOpsEventPhase.InReview && props.currentPhase !== LiveOpsEventPhase.Ended))) {
    return 'Start time is in the past.'
  } else if (schedule.value.enabledEndTime === '') {
    return 'End time is required.'
  } else if (DateTime.fromISO(schedule.value.enabledEndTime) <= DateTime.fromISO(schedule.value.enabledStartTime)) {
    return 'End time must be after start time.'
  } else {
    return undefined
  }
})

const previewHint = computed((): HintMessage => {
  if (params.value.schedule === null) {
    return { inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.InPreview)) {
    return { messageText: 'Modifying the preview duration once it has started is not allowed.', inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.Active || props.currentPhase === LiveOpsEventPhase.EndingSoon)) {
    return { messageText: 'Modifying the preview duration of an ongoing event is not allowed.', inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.InReview || props.currentPhase === LiveOpsEventPhase.Ended)) {
    return { messageText: 'Modifying the preview duration of a past event is not allowed.', inputDisabled: true }
  } else {
    return { inputDisabled: false }
  }
})

const endingSoonHint = computed((): HintMessage => {
  if (params.value.schedule === null) {
    return { inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.EndingSoon)) {
    return { messageText: 'Modifying the ending soon duration of an ongoing event is not allowed.', inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.InReview || props.currentPhase === LiveOpsEventPhase.Ended)) {
    return { messageText: 'Modifying the ending soon duration of a past event is not allowed.', inputDisabled: true }
  } else {
    return { inputDisabled: false }
  }
})

const reviewHint = computed((): HintMessage => {
  if (params.value.schedule === null) {
    return { inputDisabled: true }
  } else if (props.formMode === 'edit' && (props.currentPhase === LiveOpsEventPhase.InReview || props.currentPhase === LiveOpsEventPhase.Ended)) {
    return { messageText: 'Modifying the review duration of a past event is not allowed.', inputDisabled: true }
  } else {
    return { inputDisabled: false }
  }
})
</script>
