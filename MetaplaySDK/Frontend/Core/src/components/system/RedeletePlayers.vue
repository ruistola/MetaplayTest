<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div
  //- Card.
  b-card(title="Re-Delete Players")
    p Batch re-delete players that have been marked for deletion, but the deletion was un-done during a backup restore.
    //- Action Modal.
    div(class="tw-flex tw-justify-end")
      MActionModalButton(
        modal-title="Re-Delete Players"
        :action="redeletePlayers"
        trigger-button-label="Open Re-Delete Menu"
        ok-button-label="Re-Delete Players"
        :ok-button-disabled="!isFormValid"
        permission="api.system.player_redelete"
        @show="resetModal()"
        )
          template(#default)
            p You can use this tool to upload player deletion logs and recover the deletion status of players in case something may have been lost during backup recovery.
            p.small.text-muted This feature is a practical way to respect GDPR and the right for your players to be forgotten even during backup recovery scenarios!

            div(class="tw-space-y-2")
              MInputSingleFile(
                label="Log File"
                :model-value="file"
                @update:model-value="file = $event; fetchRedeletionList()"
                :variant="file ? error ? 'danger' : 'success' : 'default'"
                )

              MInputDateTime(
                label="Re-Delete Cutoff Time"
                :model-value="cutoffTime"
                @update:model-value="onDateUpdated"
                max-date-time="now"
                )

          template(#right-panel)
            h6(class="tw-mb-3") Select Players
            div(v-if="players && players.length > 0")
              MList(:showBorder="true")
                MListItem(v-for="(player, key) in players" :key="key").tw-px-3
                  span {{ player.playerName || 'n/a' }}
                  template(#top-right)
                    span {{ player.playerId }}
                  template(#bottom-left)
                    div(class="tw-text-xs+ tw-text-neutral-500 tw-break-words ") Deleted #[meta-time(:date="player.scheduledDeletionTime")] by {{ player.deletionSource }}
                    span
                      span Marked for re-deletion:
                      MInputCheckbox(
                        :model-value="player.redelete"
                        @update:model-value="player.redelete = $event"
                        name="isPlayerMarkedForRedeletion"
                        )
                    div(class="tw-mt-2")
                      MBadge(v-if="player.redelete" variant="danger") To Be Deleted
                      MBadge(v-else) Skip

            div(v-else-if="players && players.length == 0")
              b-alert(variant="danger" show) Based on the selected log file and cutoff time, there are no players who are eligible for re-deletion.

            div(v-else)
              b-alert(variant="secondary" show) Choose a valid log file to preview players for re-deletion.

            div(:class="[{'tw-text-neutral-500': players}, 'tw-mb-3']")
              h6 Confirm
              div(class="tw-flex tw-justify-between")
                span I know what I am doing
                MInputSwitch(
                  :model-value="confirmRedeletion"
                  @update:model-value="confirmRedeletion = $event"
                  :disabled="!players || players.length == 0"
                  class=""
                  name="confirmRedeletion"
                  size="sm"
                  )

            MErrorCallout(v-if="error" :error="error")

          template(#bottom-panel)
            meta-no-seatbelts(class="tw-w-7/12 tw-mx-auto")

</template>

<script lang="ts" setup>
import { DateTime, Duration } from 'luxon'
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { DisplayError, MActionModalButton, MBadge, MErrorCallout, MInputCheckbox, MInputDateTime, MInputSingleFile, MInputSwitch, MList, MListItem } from '@metaplay/meta-ui-next'

const gameServerApi = useGameServerApi()
const players = ref<any>(null)
const cutoffTime = ref<DateTime>(DateTime.now())
const result = ref<any>(null)
const error = ref<DisplayError>()
const confirmRedeletion = ref(false)
const file = ref<File>()

const isFormValid = computed(() => {
  return players.value !== null && confirmRedeletion.value
})
const defaultCutoffTime = computed((): DateTime => {
  const offset = Duration.fromDurationLike({ days: 60 })
  return DateTime.now().minus(offset)
})

function resetModal () {
  players.value = null
  result.value = null
  confirmRedeletion.value = false
  cutoffTime.value = defaultCutoffTime.value
  file.value = undefined
  error.value = undefined
}
async function redeletePlayers () {
  if (!file.value) return

  // Build the request.
  const formData = new FormData()
  formData.append('file', file.value)
  formData.append('cutoffTime', cutoffTime.value.toISO() as string)
  players.value.filter((p: any) => p.redelete).forEach((p: any) => {
    formData.append('playerIds', p.playerId)
  })
  // Send.
  await gameServerApi.post(
    'redeletePlayers/execute',
    formData,
    {
      headers: { 'Content-Type': 'multipart/form-data' }
    }
  )
  const message = 'Player re-deletion started.'
  showSuccessToast(message)
}

/**
 * Utility function to prevent undefined inputs.
 */
async function onDateUpdated (value?: DateTime) {
  if (!value) return
  cutoffTime.value = value

  // Handle empty state.
  if (file.value !== null) {
    await fetchRedeletionList()
  }
}

async function fetchRedeletionList () {
  // Reset the results.
  players.value = null
  confirmRedeletion.value = false
  error.value = undefined

  if (!file.value) return

  // Build the request.
  const formData = new FormData()
  formData.append('file', file.value)
  formData.append('cutoffTime', cutoffTime.value.toISO() as string)

  // Send.
  try {
    const res = (await gameServerApi.post(
      'redeletePlayers/list',
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' }
      })).data

    // Process results.
    players.value = res.playerInfos.map((p: any) => ({ ...p, redelete: true }))
  } catch (e: any) {
    // TODO: This doesn't look nice. Make it nice.
    error.value = e.response.data.error
  }
}
</script>
