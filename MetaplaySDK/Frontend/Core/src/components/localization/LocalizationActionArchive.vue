<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MIconButton(
  @click="localizationArchiveModal?.open()"
  permission="api.localization.edit"
  data-testid="`archive-localization-${localizationId}`"
  )
  //- Archive button.
  fa-icon(v-if="!singleLocalizationWithoutContents?.isArchived" icon="box-archive" class="tw-w-4 tw-h-4 tw-relative -tw-bottom-[3px]")

  //- Unarchive icon button.
  svg(
    v-else
    xmlns="http://www.w3.org/2000/svg"
    fill="currentColor"
    class="tw-w-5 tw-h-4 tw-inline-flex"
    viewBox="0 0 640 512"
    )
    path(d="M256 48c0-26.5 21.5-48 48-48H592c26.5 0 48 21.5 48 48V464c0 26.5-21.5 48-48 48H381.3c1.8-5 2.7-10.4 2.7-16V253.3c18.6-6.6 32-24.4 32-45.3V176c0-26.5-21.5-48-48-48H256V48zM571.3 347.3c6.2-6.2 6.2-16.4 0-22.6l-64-64c-6.2-6.2-16.4-6.2-22.6 0l-64 64c-6.2 6.2-6.2 16.4 0 22.6s16.4 6.2 22.6 0L480 310.6V432c0 8.8 7.2 16 16 16s16-7.2 16-16V310.6l36.7 36.7c6.2 6.2 16.4 6.2 22.6 0zM0 176c0-8.8 7.2-16 16-16H368c8.8 0 16 7.2 16 16v32c0 8.8-7.2 16-16 16H16c-8.8 0-16-7.2-16-16V176zm352 80V480c0 17.7-14.3 32-32 32H64c-17.7 0-32-14.3-32-32V256H352zM144 320c-8.8 0-16 7.2-16 16s7.2 16 16 16h96c8.8 0 16-7.2 16-16s-7.2-16-16-16H144z")

MActionModal(
  ref="localizationArchiveModal"
  title="Archive Localization"
  :action="onOk"
  ok-button-label="Save Settings"
  :ok-button-disabled="archiveState === initialArchiveState"
  data-testid="`archive-config-${localizationId}`"
  @show="onShow"
  )
  div(class="tw-flex tw-justify-between")
    div(class="tw-font-semibold") Archive Localizatoin
    MInputSwitch(
      :model-value="archiveState"
      @update:model-value="archiveState = $event"
      name="archiveState"
      size="sm"
      data-testid="localization-archive-toggle"
      )
  span.small.text-muted Archiving a localization will hide it from the list of available localizations. An archived localization can be unarchived at any time.
</template>

<script lang="ts" setup>
import { computed, ref, watch } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModal, MIconButton, MInputSwitch } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getAllLocalizationsSubscriptionOptions } from '../../subscription_options/localization'
import type { MinimalLocalizationInfo } from '../../localizationServerTypes'

const gameServerApi = useGameServerApi()

const props = defineProps<{
  /**
   * ID of the localization to publish.
   */
  localizationId: string
}>()

/**
 * Model for the toggle switch.
 */
const archiveState = ref(false)

/**
 * Cached initial archive value.
 */
const initialArchiveState = ref(false)

/**
 * Reference to the localization archive modal.
 */
const localizationArchiveModal = ref<typeof MActionModal>()

/**
 * Fetch all available localizations.
 */
const {
  data: allLocalizationsData,
  refresh: allLocalizationsRefresh,
} = useSubscription<MinimalLocalizationInfo[]>(getAllLocalizationsSubscriptionOptions())

/**
 * Localization data without the detailed content.
 */
const singleLocalizationWithoutContents = computed((): MinimalLocalizationInfo | undefined => {
  if (allLocalizationsData.value) {
    return allLocalizationsData.value.find((x) => x.id === props.localizationId)
  } else {
    return undefined
  }
})

/**
 * Cache initial archive value when the data is available.
 */
watch(() => singleLocalizationWithoutContents.value, (value) => {
  if (value) {
    initialArchiveState.value = value.isArchived
  }
}, { immediate: true })

/**
 * Called when the modal is about to be shown.
 */
function onShow () {
  archiveState.value = singleLocalizationWithoutContents.value?.isArchived ?? false
}

/**
 * Called when the modal OK button is clicked.
 */
async function onOk () {
  const params = {
    isArchived: archiveState.value
  }
  await gameServerApi.post(`/localization/${props.localizationId}`, params)
  showSuccessToast(`Localization ${archiveState.value ? 'archived' : 'unarchived'}.`)
  allLocalizationsRefresh()
}
</script>
