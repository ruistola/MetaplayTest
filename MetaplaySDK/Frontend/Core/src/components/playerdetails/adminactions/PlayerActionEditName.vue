<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MActionModalButton(
  v-if="playerData"
  modal-title="Edit Player Name"
  :action="updateName"
  trigger-button-label="Edit Name"
  trigger-button-full-width
  variant="warning"
  :ok-button-disabled="variant !== 'success'"
  @show="resetModal"
  permission="api.players.edit_name"
  data-testid="action-edit-name"
  )
  template(#default)
    span(class="tw-font-semibold") Current Name
    p(class="tw-mt-1") {{ playerData.model.playerName || 'n/a' }}

    MInputText(
      label="New Name"
      :model-value="newName"
      @update:model-value="newName = $event; validate()"
      :variant="variant"
      hint-message="Same rules are applied to name validation as changing it in-game."
      placeholder="DarkAngel87"
      data-testid="name-input"
      :debounce="300"
      )
</template>

<script lang="ts" setup>
import { ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MInputText, MActionModalButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'
import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'

const props = defineProps<{
  /**
   * ID of the player to rename.
   */
  playerId: string
}>()

const gameServerApi = useGameServerApi()
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))
const newName = ref('')

const variant = ref<'default' | 'loading' | 'success' | 'danger'>('default')

function resetModal () {
  newName.value = ''
  variant.value = 'default'
}

async function validate () {
  if (newName.value === '') {
    variant.value = 'danger'
  } else {
    variant.value = 'loading'
    variant.value = (await gameServerApi.post(`/players/${playerData.value.id}/validateName`, { NewName: newName.value })).data.nameWasValid ? 'success' : 'danger'
  }
}

async function updateName () {
  try {
    await gameServerApi.post(`/players/${playerData.value.id}/changeName`, { NewName: newName.value })
    const message = `Player '${playerData.value.id}' is now '${newName.value}'.`
    showSuccessToast(message)
  } finally {
    playerRefresh()
  }
}
</script>
