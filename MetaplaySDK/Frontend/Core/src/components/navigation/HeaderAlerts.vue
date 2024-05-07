<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(style="position: sticky; top: 0; z-index: 12")
  // Connection status
  div.header-notification.mx-3.px-3.py-3.text-center.rounded-bottom.shadow-sm.alert-danger(v-if="!gameServerApiStore.isConnected")
    div.font-weight-bolder
      fa-icon(:icon="['far', 'window-close']").mr-2
      | Lost connection to the backend!
    div Please check that the instance is running and that you are connected to the internet.

  // Game config updates
  div.header-notification.mx-3.px-3.py-3.text-center.rounded-bottom.shadow-sm.alert-danger(v-if="uiStore.isNewGameConfigAvailable")
    div.font-weight-bolder
      fa-icon(:icon="['far', 'window-close']").mr-2
      | Server game configs updated!
    div Please #[MTextButton(:to="route.fullPath") refresh the page] to get the latest changes.

  // Maintenance mode
  div.header-notification.mx-3.px-3.py-3.text-center.rounded-bottom.shadow-sm.alert-warning(data-testid="maintenance-mode-header-notification" v-if="backendStatus && (backendStatus.maintenanceStatus.isInMaintenance || backendStatus.maintenanceStatus.scheduledMaintenanceMode)")
    div(v-if="backendStatus.maintenanceStatus.isInMaintenance")
      fa-icon(:icon="['far', 'window-close']").mr-2
      span.font-weight-bolder(data-testid="maintenance-on-label") Maintenance mode on
      div You can turn it off from the #[MTextButton(to="/system") system settings page].
    div(v-else-if="backendStatus.maintenanceStatus.scheduledMaintenanceMode")
      fa-icon(:icon="['far', 'window-close']").mr-2
      span.font-weight-bolder(data-testid="maintenance-scheduled-label") Maintenance scheduled
      div Maintenance mode will start #[meta-time(:date="backendStatus.maintenanceStatus.scheduledMaintenanceMode.startAt")].
</template>

<script lang="ts" setup>
import { useRoute } from 'vue-router'

import { useGameServerApiStore } from '@metaplay/game-server-api'
import { useUiStore } from '@metaplay/meta-ui'
import { useSubscription } from '@metaplay/subscriptions'

import { getBackendStatusSubscriptionOptions } from '../../subscription_options/general'
import { MTextButton } from '@metaplay/meta-ui-next'

const route = useRoute()
const uiStore = useUiStore()
const gameServerApiStore = useGameServerApiStore()
const {
  data: backendStatus,
} = useSubscription(getBackendStatusSubscriptionOptions())
</script>
