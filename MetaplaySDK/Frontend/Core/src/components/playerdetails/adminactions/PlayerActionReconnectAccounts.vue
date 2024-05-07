<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  MActionModalButton(
    modal-title="Move Devices from Another Account"
    :action="moveAuth"
    trigger-button-label="Reconnect Devices"
    trigger-button-full-width
    variant="warning"
    ok-button-label="Reconnect Devices"
    :ok-button-disabled="!isFormValid"
    @show="resetModal"
    permission="api.players.reconnect_account"
    data-testid="action-merge-accounts"
    )
    template(#default)
      span(class="tw-font-semibold tw-mb-1") Move Devices & Social Auths from this account..
      meta-input-player-select(
        :value="selectedPlayer"
        @input="updateSourcePlayer"
        :ignorePlayerIds="[playerData.id]"
        )
      MCard(title="Source Account Preview" class="tw-mt-3")
        div(v-if="selectedPlayerData?.model")
          overview-list(listTitle="Overview" icon="chart-bar" :sourceObject="selectedPlayerData" :items='coreStore.overviewLists.playerReconnectPreview').mb-3

          span.font-weight-bold Login Methods
          MList(v-if="sourceAuthMethods && sourceAuthMethods.length > 0")
            MListItem(v-for="auth in sourceAuthMethods" :key="auth.id" class="tw-pl-0 tw-pb-0")
              span.font-weight-bold(v-if="auth.type === 'device'").text-danger 📱 {{ auth.displayString }} #[MBadge(variant="danger") Will be removed]
              span.font-weight-bold(v-else).text-danger 👨‍👩‍👧‍👦 {{ auth.displayString }} #[MBadge(variant="danger") Will be removed]
              template(#bottom-left)
                p.text-truncate {{ auth.id }}

          b-alert.mt-2(v-else show variant="secondary") This account has no credentials attached.
        div(v-else) Type in a valid player ID to preview what device reconnecting would do.
    template(#right-panel)
      span(class="tw-font-semibold tw-mb-1") ...to the current account
      p(class="tw-mt-1 tw-mb-1") {{ playerData.id }}

      MCard(title="Current Account Preview" class="tw-mt-6")
        overview-list(listTitle="Overview" icon="chart-bar" :sourceObject="playerData" :items='coreStore.overviewLists.playerReconnectPreview' ).mb-3

        span.font-weight-bold Login Methods
        MList(v-if="targetAuthMethods && targetAuthMethods.length > 0")
          MListItem(v-for="auth in targetAuthMethods" :key="auth.id" class="tw-pl-0 tw-pb-0")
            span.font-weight-bold(v-if="auth.type === 'device'") 📱 {{ auth.displayString }} #[MBadge(v-show="isFormValid") Will remain]
            span.font-weight-bold(v-else) 👨‍👩‍👧‍👦 {{ auth.displayString }} #[MBadge Will remain]
            template(#bottom-left)
              p.text-truncate {{ auth.id }}

          MListItem(v-for="auth in sourceAuthMethods" :key="auth.id" class="tw-pl-0 tw-pb-0")
            span.font-weight-bold(v-if="auth.type === 'device'").text-success 📱 {{ auth.displayString }} #[MBadge(variant="success") Will be added]
            span.font-weight-bold(v-else).text-success 👨‍👩‍👧‍👦 {{ auth.displayString }} #[MBadge(variant="success") Will be added]
            template(#bottom-left)
              p.text-truncate {{ auth.id }}

        b-alert.mt-2(v-else show variant="secondary") This account has no credentials attached.

    template(#bottom-panel)
      meta-no-seatbelts(
        v-if="isFormValid"
        :name="selectedPlayerData.model.playerName || 'n/a'"
        class="tw-w-7/12 tw-mx-auto"
        )
</template>

<script lang="ts" setup>
import axios from 'axios'
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { parseAuthMethods, showSuccessToast } from '@metaplay/meta-ui'
import type { PlayerListItem } from '@metaplay/meta-ui/src/additionalTypes'
import { MBadge, MList, MListItem, MActionModalButton, MCard } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'
import { useCoreStore } from '../../../coreStore'
import OverviewList from '../../global/OverviewList.vue'

const props = defineProps<{
  /**
   * Id of the player to target the reconnect action at.
   */
  playerId: string
}>()

const coreStore = useCoreStore()

/**
 * Subscribe to date about the target player.
 */
const {
  data: playerData,
  refresh: playerRefresh
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

/**
 * Selected player.
 */
const selectedPlayer = ref<PlayerListItem>()

/**
 * Full model for selected player.
 */
const selectedPlayerData = ref<any>()

/**
 * Validates that source player's data is available.
 */
const isSourcePlayerFetched = computed(() => {
  if (selectedPlayerData.value?.model) {
    return true
  }
  return false
})

/**
 * Computes whether the form is valid, id: can OK be pressed or not?
 */
const isFormValid = computed(() => {
  if (!isSourcePlayerFetched.value) {
    // Source player must have been loaded.
    return false
  }
  if (Object.keys(selectedPlayerData.value?.model.attachedAuthMethods).length === 0) {
    // Source player must have some authentication method attached to their account.
    return false
  }

  // Everything is ok!
  return true
})

/**
 * @returns An array of the social authentication methods enabled in source player account.
 */
const sourceAuthMethods = computed(() => {
  if (selectedPlayerData.value?.model) {
    return parseAuthMethods(selectedPlayerData.value.model.attachedAuthMethods, selectedPlayerData.value.model.deviceHistory)
  } else {
    return []
  }
})

/**
 * @returns An array of the social authentication methods attached to the target player account.
 */
const targetAuthMethods = computed(() => {
  return parseAuthMethods(playerData.value.model.attachedAuthMethods, playerData.value.model.deviceHistory)
})

function resetModal () {
  selectedPlayer.value = undefined
  selectedPlayerData.value = undefined
}

let cancelTokenSource = axios.CancelToken.source()
async function updateSourcePlayer (playerInfo: PlayerListItem) {
  selectedPlayer.value = playerInfo
  selectedPlayerData.value = null
  try {
    // Cancel the previous request.
    if (cancelTokenSource) {
      cancelTokenSource.cancel('Request cancelled by user interaction.')
    }

    cancelTokenSource = axios.CancelToken.source()

    const res = (await useGameServerApi().get(`/players/${playerInfo.id}`, { cancelToken: cancelTokenSource.token })).data
    if (res) {
      selectedPlayerData.value = res
    }
  } catch (err) {
    if (!axios.isCancel(err)) {
      // TODO: handle actual error
    }
  }
}

async function moveAuth () {
  if (selectedPlayer.value && selectedPlayerData.value) {
    await useGameServerApi().post(`/players/${selectedPlayer.value.id}/moveAuthTo/${playerData.value.id}`)
    showSuccessToast(`Login methods from '${selectedPlayer.value.id}' moved to the current player.`)
    playerRefresh()
  } else {
    throw new Error('Trying to trigger "moveAuth" without a source player!')
  }
}
</script>
