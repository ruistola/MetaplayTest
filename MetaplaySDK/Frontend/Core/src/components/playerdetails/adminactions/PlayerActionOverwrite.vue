<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  //- Button to display the modal
  MActionModalButton(
    modal-title="Overwrite Player"
    :action="() => overwritePlayer(entityArchiveFile ?? entityArchiveText)"
    trigger-button-label="Overwrite Player"
    trigger-button-full-width
    variant="danger"
    ok-button-label="Overwrite Player"
    :ok-button-disabled="!isFormValid"
    @show="resetModal"
    @hidden="resetModal"
    permission="api.players.overwrite"
    data-testid="action-overwrite-player"
  )
    template(#default)
      h6 Paste Player Data
      p You can copy & paste the serialized data of a compatible player here to overwrite parts of #[MBadge {{ playerData.model.playerName || 'n/a' }}].
      p This is an #[span(class="tw-text-danger") advanced development feature] and should probably never be used in production!

      MInputTextArea(
        class="tw-mb-2"
        label="Paste in an archive..."
        data-testid="entity-archive-text"
        :model-value="entityArchiveText"
        @update:model-value="entityArchiveText = $event; validatePlayer($event)"
        :placeholder="entityArchiveFile != null ? 'File upload selected' : `{'entities':{'player':...`"
        :variant="isFormValid !== null ? isFormValid ? 'success' : 'danger' : 'default'"
        :rows="5"
        :disabled="!!entityArchiveFile"
      )

      MInputSingleFileContents(
        label="...or upload a file..."
        data-testid="entity-archive-file"
        :model-value="entityArchiveFile"
        @update:model-value="entityArchiveFile = $event; validatePlayer($event)"
        :placeholder="entityArchiveText !== '' ? 'Manual paste selected' : 'Choose or drop an entity archive file'"
        :variant="isFormValid !== null ? isFormValid ? 'success' : 'danger' : 'default'"
        :disabled="entityArchiveText !== ''"
        accept=".json"
      )

    template(#right-panel)
      h6.mb-3 Preview Incoming Data
      b-alert(v-if="entityContainedExtraTypes" show variant="warning") Archive contained the following extra entity types that were removed:&nbsp;
        span(v-for="entity in entityContainedExtraTypes") {{ entity }}
      b-alert(v-if="!validationResultString" show variant="secondary") Paste in a valid player from a compatible game version to preview what data will be copied over.
      b-alert(v-if="displayError" show variant="warning")
        div(v-if="displayError.title").font-weight-bolder {{ displayError?.title }}
        p(v-if="displayError.message") {{ displayError?.message }}
        pre(v-if="displayError.details" style="font-size: 70%") {{ displayError?.details }}

      div(v-if="validationResultString")
        div.code-box.text-monospace.border.rounded.bg-light.w-100(style="max-height: 20.3rem")
          pre {{ validationResultString }}

    template(#bottom-panel v-if="isFormValid")
        meta-no-seatbelts(class="tw-w-7/12 tw-mx-auto" :name="playerData.model.playerName || 'n/a'")

</template>

<script lang="ts" setup>
import axios, { type CancelTokenSource } from 'axios'
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton, MBadge, MInputTextArea, MInputSingleFileContents } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'
import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'

/**
 * Base type definition for the displayed error.
 * TODO: Replace once the new error handling is in place.
 */
interface DisplayError {
  title?: string
  message?: string
  details?: string
}

const props = defineProps<{
  /**
   * Id of the player to target the overwrite action at.
   */
  playerId: string
}>()

const gameServerApi = useGameServerApi()

/**
 * Subscribe to the player whose data will be overwritten.
 */
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

/**
 * The entity archive text pasted into the text field.
 */
const entityArchiveText = ref('')

/**
 * The entity archive file that the user has selected.
 */
const entityArchiveFile = ref<string>()

/**
 * The validation result string.
 */
const validationResultString = ref<string>()

/**
 * When true the entity archive player model is valid to overwrite.
 */
const playerModelValidToOverwrite = ref(false)

/**
 * The extra entity types that were removed from the entity archive.
 */
const entityContainedExtraTypes = ref<string[]>()

/**
 * Error to be displayed.
 */
const displayError = ref<DisplayError>()

/**
 * The cancel token source for the validation request.
 */
const cancelTokenSource = ref<CancelTokenSource>()

/**
 * Whether the form is valid.
 */
const isFormValid = computed(() => {
  const hasEntityArchive = entityArchiveText.value !== '' || entityArchiveFile.value
  if (!hasEntityArchive || (hasEntityArchive && !displayError.value && !validationResultString.value)) {
    return null
  } else if (validationResultString.value && playerModelValidToOverwrite.value) {
    return true
  } else {
    return false
  }
})

/**
 * Reset the modal to its initial state.
 */
function resetModal () {
  entityArchiveText.value = ''
  entityArchiveFile.value = undefined
  validationResultString.value = undefined
  playerModelValidToOverwrite.value = false
  displayError.value = undefined
}

/**
 * Validates the player model data against the server.
 * @param rawData The raw player model data.
 */
async function validatePlayer (rawData?: string) {
  displayError.value = undefined
  validationResultString.value = undefined
  entityContainedExtraTypes.value = undefined
  playerModelValidToOverwrite.value = false

  if (rawData) {
    // Get the payload data that we want to validate.
    let payload
    try {
      payload = calculatePayload(rawData)
    } catch (e: any) {
      displayError.value = { message: e.message }
      return
    }

    // Send the payload to the server to validate it.
    try {
      if (cancelTokenSource.value) { cancelTokenSource.value.cancel('Request canceled by user interaction.') }
      cancelTokenSource.value = axios.CancelToken.source()
      const result = (await gameServerApi.post(`/players/${playerData.value.id}/validateOverwrite`, payload, { cancelToken: cancelTokenSource.value.token })).data
      if (result.error) {
        // If the result has an error object then it failed.
        displayError.value = { message: result.error.message, details: result.error.details }
      } else {
        // Otherwise there must be a data object, indicating success.
        if (result.data.diff === '') {
          validationResultString.value = 'No differences in player model data.\nDid you paste in the correct player?'
        } else {
          validationResultString.value = result.data.diff
          playerModelValidToOverwrite.value = true
        }
      }
    } catch (e: any) {
      if (axios.isCancel(e)) {
        // Ignore
      } else if (e.response) {
        displayError.value = { message: e.response.data.error.message }
      }
    }
  }
}

/**
 * Overwrites the player with the data in the entity archive.
 */
async function overwritePlayer (newData: string) {
  const payload = calculatePayload(newData)
  await gameServerApi.post(`/players/${playerData.value.id}/importOverwrite`, payload)
  showSuccessToast('Player import succeeded.')
  playerRefresh()
}

/**
 * Preview the diffs between the current and the 'new' player model that will be sent to the server.
 * @param rawData The raw player model data.
 */
function calculatePayload (rawData: string) {
  let payload: any
  try {
    payload = JSON.parse(rawData)
  } catch (e: any) {
    throw new Error(`Could not parse archive. Got '${e.message}'.`)
  }

  // Client-side validatation of the entity archive.
  if (typeof payload !== 'object') {
    throw new Error(`Entity archive must be an object. Got '${typeof payload}'.`)
  }
  if (Array.isArray(payload)) {
    throw new Error('Entity archive must be an object. Got \'array\'.')
  }
  if (!('entities' in payload)) {
    throw new Error('Entity archive must contain entities but none found.')
  }
  if (!('player' in payload.entities)) {
    throw new Error('Entity archive must contain player entities but none found.')
  }
  const players = Object.keys(payload.entities.player)
  if (players.length !== 1) {
    throw new Error(`Entity archive may only contain exactly one player entity. Got ${players.length}.`)
  }

  // Remove any non-player entity types.
  const keys = Object.keys(payload.entities).filter(e => e !== 'player')
  // TODO: dynamic delete is not a great pattern. Consider refactoring this implementation.
  // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
  keys.forEach(key => delete payload.entities[key])
  if (keys.length) {
    entityContainedExtraTypes.value = keys
  }

  // Create the remap data.
  const sourcePlayerId = players[0]
  const targetPlayerId = playerData.value.id
  payload.remaps = {
    player: {
    }
  }
  payload.remaps.player[sourcePlayerId] = targetPlayerId
  return payload
}
</script>
