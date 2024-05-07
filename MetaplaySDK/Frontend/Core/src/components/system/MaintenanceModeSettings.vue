<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(data-testid="system-maintenance-mode-card" v-if="backendStatusData")
  b-card.mb-2
    b-card-title
      b-row(align-v="center" no-gutters)
        fa-icon(:icon="['far', 'window-close']").mr-2
        span Maintenance Mode
        MBadge.ml-2(variant="success" v-if="backendStatusData.maintenanceStatus.isInMaintenance") On
        MBadge.ml-2(variant="primary" v-else-if="backendStatusData.maintenanceStatus.scheduledMaintenanceMode") Scheduled
        MBadge.ml-2(v-else) Off

    div(v-if="backendStatusData.maintenanceStatus.isInMaintenance").mb-3
      div Maintenance mode started #[meta-time(:date="backendStatusData.maintenanceStatus.scheduledMaintenanceMode.startAt")].
      div(v-if="backendStatusData.maintenanceStatus.scheduledMaintenanceMode.estimationIsValid") Estimated duration: {{ backendStatusData.maintenanceStatus.scheduledMaintenanceMode.estimatedDurationInMinutes }} minutes
      div(v-else) Duration: #[MBadge None]
      div Affected platforms: #[MBadge(v-for="plat in maintenancePlatformsOnServer" :key="plat").mr-1 {{ plat }} ]

    div(v-else-if="backendStatusData.maintenanceStatus.scheduledMaintenanceMode")
      p
        | Maintenance mode has been scheduled to start #[meta-time(:date="backendStatusData.maintenanceStatus.scheduledMaintenanceMode.startAt")] on #[meta-time(:date="backendStatusData.maintenanceStatus.scheduledMaintenanceMode.startAt" showAs="datetime")]
      p.m-0(v-if="backendStatusData.maintenanceStatus.scheduledMaintenanceMode.estimationIsValid") Estimated duration: {{ backendStatusData.maintenanceStatus.scheduledMaintenanceMode.estimatedDurationInMinutes }} minutes
      p.m-0(v-else) Duration: #[MBadge Off]
      p Affected platforms: #[MBadge(v-for="plat in maintenancePlatformsOnServer" :key="plat").mr-1 {{ plat }} ]

    div(v-else)
      p Maintenance mode will prevent players from logging into the game. Use it to make backend downtime more graceful for players.

    div(class="tw-flex tw-justify-end")
      MActionModalButton(
        modal-title="Update Maintenance Mode Settings"
        :action="setMaintenance"
        trigger-button-label="Edit Settings"
        :ok-button-label="okButtonDetails.text"
        :ok-button-disabled="!isFormValid"
        permission="api.system.edit_maintenance"
        data-testid="maintenance-mode"
        @show="resetModal"
        )
        template(#ok-button-icon)
          fa-icon(
            v-if="okButtonDetails.icon"
            :icon="okButtonDetails.icon"
            class="tw-h-3.5 tw-w-4 tw-mb-[0.05rem] tw-mr-1"
            )
        div(class="tw-flex tw-justify-between tw-font-semibold") Maintenance Mode Enabled
          MInputSwitch(
            :model-value="maintenanceEnabled"
            @update:model-value="maintenanceEnabled = $event"
            class="tw-relative tw-top-1 tw-mr-1"
            name="maintenanceModeEnabled"
            size="sm"
            data-testid="input-switch-maintenance-enabled"
            )

        MInputDateTime(
          :model-value="maintenanceDateTime"
          @update:model-value="onMaintenanceDateTimeChange"
          :disabled="!maintenanceEnabled"
          min-date-time="now"
          label="Start Time"
          )
        p(class="tw-mt-1 tw-text-neutral-400 tw-font-xs")
          span(v-if="!isMaintenanceDateTimeInFuture && maintenanceEnabled") Maintenance mode will start immediately.
          span(v-else-if="maintenanceEnabled") Maintenance mode will start #[meta-time(:date="maintenanceDateTime")].
          span(v-else) Maintenance mode off.

        MInputMultiSelectCheckbox(
          :model-value="maintenancePlatforms"
          @update:model-value="maintenancePlatforms = $event"
          :options="props.platforms.map(platform => ({ label: platform, value: platform }))"
          label="Platforms"
          :disabled="!maintenanceEnabled"
          hint-message="Maintenance mode will only affect the selected platforms."
          class="tw-mb-4"
          )

        MInputNumber(
          label="Estimated Duration"
          :model-value="maintenanceDuration"
          @update:model-value="maintenanceDuration = $event"
          :disabled="!maintenanceEnabled"
          :variant="maintenanceEnabled && maintenanceDuration && maintenanceDuration > 0 ? 'success' : 'default'"
          hint-message="This is just a number you can display on the client to the players. Maintenance mode will not turn off automatically based on duration."
          :min="0"
          )

//- TODO: This is a placeholder for until we migrate to tailwind with unified styling and extract this logic to own component.
div(v-else)
  b-card.mb-2
    b-card-title
      b-row(align-v="center" no-gutters)
        fa-icon(:icon="['far', 'window-close']").mr-2
        span Maintenance Mode
    p.text-danger Failed to load the current maintenance mode status.
    MErrorCallout(:error="backendStatusError")
</template>

<script lang="ts" setup>
import { DateTime } from 'luxon'
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton, MBadge, MErrorCallout, MInputDateTime, MInputMultiSelectCheckbox, MInputNumber, MInputSwitch } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getBackendStatusSubscriptionOptions } from '../../subscription_options/general'

const props = withDefaults(defineProps<{
  platforms?: string[]
}>(), {
  platforms: () => ['iOS', 'Android', 'WebGL', 'UnityEditor'],
})

const gameServerApi = useGameServerApi()
const {
  data: backendStatusData,
  refresh: backendStatusTriggerRefresh,
  error: backendStatusError
} = useSubscription(getBackendStatusSubscriptionOptions())

const maintenanceEnabled = ref<boolean>(false)
const maintenanceDateTime = ref<DateTime>(DateTime.now())
const maintenanceDuration = ref<number>()
const maintenancePlatforms = ref<any>(null)

const maintenancePlatformsOnServer = computed(() => {
  return props.platforms.filter(platform => !backendStatusData.value.maintenanceStatus.scheduledMaintenanceMode?.platformExclusions?.includes(platform))
})

const isFormValid = computed(() => {
  if (!maintenanceEnabled.value && !(backendStatusData.value.maintenanceStatus.isInMaintenance || backendStatusData.value.maintenanceStatus.scheduledMaintenanceMode)) {
    return false
  }
  if (!maintenanceEnabled.value) {
    return true
  }
  if (maintenancePlatforms.value.length === 0) {
    return false
  }
  return true
})

function onMaintenanceDateTimeChange (value?: DateTime) {
  if (!value) return
  maintenanceDateTime.value = value
}

const isMaintenanceDateTimeInFuture = computed(() => {
  return maintenanceDateTime.value.diff(DateTime.now()).toMillis() >= 0
})

async function setMaintenance () {
  if (maintenanceEnabled.value) {
    const payload = {
      StartAt: maintenanceDateTime.value.toISO(),
      EstimatedDurationInMinutes: maintenanceDuration.value ? maintenanceDuration.value : 0,
      EstimationIsValid: !!maintenanceDuration.value,
      PlatformExclusions: props.platforms.filter(platform => !maintenancePlatforms.value.includes(platform))
    }
    await gameServerApi.put('maintenanceMode', payload)

    if (isMaintenanceDateTimeInFuture.value) {
      const message = 'Maintenance mode enabled.'
      showSuccessToast(message)
    } else {
      const message = 'Maintenance mode scheduled.'
      showSuccessToast(message)
    }
  } else {
    await gameServerApi.delete('maintenanceMode')
    const message = 'Maintenance mode disabled.'
    showSuccessToast(message)
  }
  backendStatusTriggerRefresh()
}

const okButtonDetails = computed(() => {
  if (maintenanceEnabled.value) {
    if (isMaintenanceDateTimeInFuture.value) {
      return {
        text: 'Schedule',
        icon: 'calendar-alt',
      }
    } else {
      return {
        text: 'Set Immediately',
        icon: ['far', 'window-close'],
      }
    }
  } else {
    return {
      text: 'Save Settings',
    }
  }
})

function resetModal () {
  maintenanceDuration.value = backendStatusData.value.maintenanceStatus.scheduledMaintenanceMode?.estimationIsValid ? backendStatusData.value.maintenanceStatus.scheduledMaintenanceMode.estimatedDurationInMinutes : undefined
  maintenanceDateTime.value = backendStatusData.value.maintenanceStatus.scheduledMaintenanceMode ? DateTime.fromISO(backendStatusData.value.maintenanceStatus.scheduledMaintenanceMode.startAt) : DateTime.now().plus({ minutes: 60 })
  maintenanceEnabled.value = !!((backendStatusData.value.maintenanceStatus.isInMaintenance || backendStatusData.value.maintenanceStatus.scheduledMaintenanceMode))
  maintenancePlatforms.value = props.platforms.filter(platform => !backendStatusData.value.maintenanceStatus.scheduledMaintenanceMode?.platformExclusions?.includes(platform))
}
</script>
