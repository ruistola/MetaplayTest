<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MActionModalButton(
  modal-title="Edit Guild Name and Description"
  :action="change"
  trigger-button-label="Edit Name and Description"
  trigger-button-full-width
  ok-button-label="Update"
  :ok-button-disabled="!isValid"
  permission="api.guilds.edit_details"
  data-testid="action-edit-details"
  @show="resetModal"
  )
    //- TODO: Rewrite the validation logic to show a loading variant during validation.
    MInputText(
      label="Display Name"
      :model-value="newDisplayName"
      @update:model-value="newDisplayName = $event; validate()"
      :variant="isValidDisplayName === null ? 'default' : isValidDisplayName ? 'success' : 'danger'"
      hint-message="Same rules are applied to name validation as changing it in-game."
      class="tw-mb-4"
      )

    MInputText(
      label="Description"
      :model-value="newDescription"
      @update:model-value="newDescription = $event; validate()"
      :variant="isValidDescription === null ? 'default' : isValidDescription ? 'success' : 'danger'"
      )
</template>

<script lang="ts" setup>
import { debounce } from 'lodash-es'
import { ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { useSubscription } from '@metaplay/subscriptions'

import { getSingleGuildSubscriptionOptions } from '../../../subscription_options/guilds'
import { MActionModalButton, MInputText } from '@metaplay/meta-ui-next'

const props = defineProps<{
  guildId: string
}>()

const {
  data: guildData,
  refresh: guildTriggerRefresh,
} = useSubscription(getSingleGuildSubscriptionOptions(props.guildId))

const gameServerApi = useGameServerApi()
const newDisplayName = ref('')
const newDescription = ref('')
const isValidDisplayName = ref<boolean | null>(null)
const isValidDescription = ref<boolean | null>(null)
const isValid = ref(false)

function resetModal () {
  newDisplayName.value = guildData.value.model.displayName
  newDescription.value = guildData.value.model.description
  isValidDisplayName.value = null
  isValidDescription.value = null
  isValid.value = false
}

const validateDebounce = debounce(async () => {
  const isDisplayNameChanged = newDisplayName.value !== guildData.value.model.displayName
  const isDescriptionChanged = newDescription.value !== guildData.value.model.description
  if (isDisplayNameChanged || isDescriptionChanged) {
    const res = (await gameServerApi.post(`/guilds/${guildData.value.id}/validateDetails`, { NewDisplayName: newDisplayName.value, NewDescription: newDescription.value })).data
    isValidDisplayName.value = res.displayNameWasValid
    isValidDescription.value = res.descriptionWasValid
  }
  isValid.value = (isDisplayNameChanged || isDescriptionChanged) && Boolean(isValidDisplayName.value) && Boolean(isValidDescription.value)
}, 300)

async function validate () {
  isValidDisplayName.value = null
  isValidDescription.value = null
  isValid.value = false
  await validateDebounce()
}

async function change () {
  try {
    await gameServerApi.post(`/guilds/${guildData.value.id}/changeDetails`, { NewDisplayName: newDisplayName.value, NewDescription: newDescription.value })
    const message = `Guild renamed to '${newDisplayName.value}'.`
    showSuccessToast(message)
  } finally {
    guildTriggerRefresh()
  }
}
</script>
