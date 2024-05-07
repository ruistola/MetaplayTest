<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  MActionModalButton(
    modal-title="Manage Developer Status"
    :action="updateDeveloperStatus"
    :trigger-button-label="playerData.model.isDeveloper ? 'Remove Dev Status' : 'Mark as Developer'"
    trigger-button-full-width
    variant="warning"
    ok-button-label="Save Settings"
    :ok-button-disabled="!isStatusChanged"
    @show="resetModal"
    permission="api.players.set_developer"
    data-testid="action-set-developer"
    )
    template(#default)
      div(class="tw-flex tw-justify-between")
        span(class="tw-font-semibold") Developer Status
        MInputSwitch(
          :model-value="isDeveloper"
          @update:model-value="isDeveloper = $event"
          class="tw-relative tw-top-1 tw-mr-3"
          name="isPlayerDeveloper"
          size="sm"
          data-testid="developer-status-toggle"
          )
      div(class="tw-text-neutral-500 tw-text-xs+")
        div(class="tw-mb-1") Developer players have special powers. For instance, developers can:
        ul(class="tw-ps-1.5")
          li - Login during maintenance.
          li - Execute development-only actions in production.
          li - Allow validating iOS sandbox in-app purchases.
          li - Bypass logic version downgrade check in production.
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'
import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MInputSwitch, MActionModalButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'
import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'

const props = defineProps<{
  /**
   * ID of the player to set as developer.
   */
  playerId: string
}>()

const gameServerApi = useGameServerApi()
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))
const isDeveloper = ref(false)

const isStatusChanged = computed(() => {
  return isDeveloper.value !== playerData.value.model.isDeveloper
})

function resetModal () {
  isDeveloper.value = playerData.value.model.isDeveloper
}

async function updateDeveloperStatus () {
  const response = await gameServerApi.post(`/players/${playerData.value.id}/developerStatus?newStatus=${isDeveloper.value}`)
  showSuccessToast(`${playerData.value.model.playerName || 'n/a'} ${response.data.isDeveloper ? 'set as developer' : 'no longer set as developer'}.`)
  playerRefresh()
}
</script>
