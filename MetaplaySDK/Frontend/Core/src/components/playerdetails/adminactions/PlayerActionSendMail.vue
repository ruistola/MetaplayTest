<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  MActionModalButton(
    modal-title="Send In-Game Mail"
    :action="sendMail"
    trigger-button-label="Send Mail"
    trigger-button-full-width
    ok-button-label="Send Mail"
    :ok-button-disabled="!isValid"
    @show="resetModal"
    permission="api.players.mail"
    data-testid="action-mail"
    )
    template(#action-button-icon)
      fa-icon(icon="paper-plane").mr-2/
    template(#modal-subtitle)
      span(class="tw-text-neutral-500 tw-text-xs+ tw-mr-32") Player's language:&#32;
        meta-language-label(:language="playerData.model.language" variant="badge")
    template(#default)
      //- Styling issue where there is y scroll bar for overflow.
      //- TODO: Need to remove it.
      meta-generated-form(
        :typeName="'Metaplay.Core.InGameMail.MetaInGameMail'"
        v-model="mail"
        :forcedLocalization="playerData.model.language"
        :page="'PlayerActionSendMail'"
        @status="isValid = $event"
        class="!tw-overflow-x-hidden"
        )
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'
import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'

import MetaGeneratedForm from '../../generatedui/components/MetaGeneratedForm.vue'
import MetaLanguageLabel from '../../MetaLanguageLabel.vue'

const props = defineProps<{
  /**
   * ID of the player to send the mail to.
   */
  playerId: string
}>()

const gameServerApi = useGameServerApi()
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

const mail = ref<any>({})
const isValid = ref(false)

function resetModal () {
  mail.value = {}
  isValid.value = false
}

async function sendMail () {
  await gameServerApi.post(`/players/${playerData.value.id}/sendMail`, mail.value)
  showSuccessToast(`In-game mail sent to ${playerData.value.model.playerName || 'n/a'}.`)
  playerRefresh()
}
</script>
