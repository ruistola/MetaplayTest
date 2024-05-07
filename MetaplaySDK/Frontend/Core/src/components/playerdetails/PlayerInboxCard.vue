<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-list-card(
  data-testid="player-inbox-card"
  title="Inbox"
  icon="inbox"
  :itemList="playerData.model.mailInbox"
  :getItemKey="getItemKey"
  tooltip="Messages currently waiting in the player's inbox."
  :searchFields="searchFields"
  :filterSets="filterSets"
  :sortOptions="sortOptions"
  :defaultSortOption="defaultSortOption"
  emptyMessage="Inbox empty."
  )
  template(#item-card="{ item: mail }")
    MCollapse(extraMListItemMargin)
      template(#header)
        MListItem(noLeftPadding data-testid="mail-item" ) {{ mail.contents.description }}
          template(#badge)
            fa-icon(v-if="Object.keys(mail.contents.consumableRewards).length > 0" icon="paperclip" size="sm" class="tw-text-neutral-500 tw-mt-1")
          template(#top-right)
            span(class="tw-text-sm tw-text-neutral-500") {{ mail.id }}
            MIconButton(
              @click="deleteInboxModal?.open(mail); mailToDelete = mail"
              permission="api.players.mail"
              variant="danger"
              aria-label="Delete this mail."
              data-testid="confirm-mail-delete"
              )
              FontAwesomeIcon(icon="trash-alt")

      //- Collapse content (mail contents)
      meta-generated-content(
        :value="mail.contents"
        :previewLocale="playerData.model.language"
        )

    //- Modal
    MActionModal(
      ref="deleteInboxModal"
      title="Delete Mail"
      :action="() => deleteMail(mailToDelete)"
      data-testid="confirm-mail-delete"
      @hidden="mailToDelete = null"
      )
      p #[MBadge {{ playerData.model.playerName || 'n/a' }}] has not claimed this mail yet. Are you sure that you want to delete the mail #[MBadge {{ mailToDeleteTitle }}]?

      meta-no-seatbelts
      template(#bottom-right)
        meta-time(:date="mail.sentAt" showAs="timeagoSentenceCase")

</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'

import { useGameServerApi } from '@metaplay/game-server-api'
import { MetaListFilterSet, MetaListFilterOption, MetaListSortDirection, MetaListSortOption, showSuccessToast } from '@metaplay/meta-ui'
import { MActionModal, MBadge, MCollapse, MIconButton, MListItem } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'
import { getSinglePlayerSubscriptionOptions } from '../../subscription_options/players'

import MetaGeneratedContent from '../generatedui/components/MetaGeneratedContent.vue'

const props = defineProps<{
  /**
   * Id of the player whose inbox to show.
   */
  playerId: string
}>()

const gameServerApi = useGameServerApi()
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

const searchFields = [
  'contents.description'
]

const filterSets = [
  new MetaListFilterSet('attachments',
    [
      new MetaListFilterOption('Has attachments', (x: any) => x.contents.consumableRewards.length > 0),
      new MetaListFilterOption('No attachments', (x: any) => x.contents.consumableRewards.length === 0)
    ]
  )
]

const sortOptions = [
  new MetaListSortOption('Time', 'sentAt', MetaListSortDirection.Ascending),
  new MetaListSortOption('Time', 'sentAt', MetaListSortDirection.Descending),
]

const defaultSortOption = 1
const mailToDelete = ref<any>(null)
const deleteInboxModal = ref<typeof MActionModal>()

const mailToDeleteTitle = computed(() => {
  return mailToDelete.value ? mailToDelete.value.contents.description as string : ''
})

function getItemKey (item: any) {
  return item.id
}

async function deleteMail (mail: any) {
  await gameServerApi.delete(`/players/${playerData.value.id}/deleteMail/${mail.id}`)
  const message = `${mail.contents.description}' deleted from ${playerData.value.model.playerName || 'n/a'}.`
  showSuccessToast(message)
  playerRefresh()
}
</script>
