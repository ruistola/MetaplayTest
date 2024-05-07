<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  MActionModalButton(
    modal-title="Change Banned Status"
    :action="updateBannedStatus"
    :trigger-button-label="playerData.model.isBanned ? 'Un-Ban Player' : 'Ban Player'"
    trigger-button-full-width
    variant="warning"
    ok-button-label="Save Settings"
    :ok-button-disabled="!isStatusChanged"
    @show="resetModal"
    permission="api.players.ban"
    data-testid="action-ban-player"
    )
    template(#default)
      div(class="tw-flex tw-justify-between")
        span(class="tw-font-semibold") Player Banned
        MInputSwitch(
          :model-value="isCurrentlyBanned"
          @update:model-value="isCurrentlyBanned = $event"
          class="tw-relative tw-top-1 tw-mr-3"
          name="isPlayerBanned"
          size="sm"
          data-testid="player-ban-toggle"
          )
      span(class="tw-text-neutral-500 tw-text-xs+") Banning will disconnect the player and prevent them from logging into the game.
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
   * ID of the player to ban.
   */
  playerId: string
}>()

const gameServerApi = useGameServerApi()
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))
const isCurrentlyBanned = ref(false)

const isStatusChanged = computed(() => {
  return isCurrentlyBanned.value !== playerData.value.model.isBanned
})

function resetModal () {
  isCurrentlyBanned.value = playerData.value.model.isBanned
}

async function updateBannedStatus () {
  const isBanned = isCurrentlyBanned.value // \note Copy, because this.isBanned might get modified before toast is shown
  await gameServerApi.post(`/players/${playerData.value.id}/ban`, { isBanned })
  showSuccessToast(`${playerData.value.model.playerName || 'n/a'} ${isBanned ? 'banned' : 'un-banned'}.`)
  playerRefresh()
}
</script>
