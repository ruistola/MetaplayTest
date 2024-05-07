<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  MActionModalButton(
    modal-title="Reset Player State"
    :action="resetPlayerState"
    trigger-button-label="Reset Player"
    trigger-button-full-width
    variant="danger"
    ok-button-label="Reset Player"
    permission="api.players.reset_player"
    data-testid="action-reset-player-state"
    )
    template(#default)
      p Resetting the player will re-initialize their game progression. Important things like past purchases and connected devices will not be wiped.
      p This action is great during development, but should likely never be used in production with real players!
      meta-no-seatbelts(
        :name="playerData.model.playerName || 'n/a'"
        class="tw-mt-4")
</template>

<script lang="ts" setup>
import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton } from '@metaplay/meta-ui-next'
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

async function resetPlayerState () {
  await gameServerApi.post(`/players/${playerData.value.id}/resetState`)
  showSuccessToast('Player state reset to defaults.')
  playerRefresh()
}
</script>
