<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
//- TODO: Migrate to MActionModal and MTextButton/MButton
MTextButton(
  v-if="textButton"
  @click="gameConfigPublishModal?.open"
  :disabled="bestGuessDisallowPublishReason !== undefined || publishBlocked"
  :disabled-tooltip="bestGuessDisallowPublishReason"
  ) Publish

MButton(
  v-else
  @click="gameConfigPublishModal?.open()"
  :disabled="bestGuessDisallowPublishReason !== undefined || publishBlocked"
  :disabled-tooltip="bestGuessDisallowPublishReason"
  ) Publish

//- Publish game config modal.
MActionModal(
  ref="gameConfigPublishModal"
  title="Publish Game Config"
  :action="publishConfig"
  ok-button-label="Publish Config"
  :ok-button-disabled="validatedDisallowPublish !== false"
  @show="onShow"
  )
    div(v-if="validatedDisallowPublish === undefined").w-100.text-center.my-5
      div.font-weight-bold Validating Game Config
      div.small.text-muted.mt-1 Please wait while we check that this config is valid for publishing.
      b-spinner(label="Validating...").tw-mt-3
    div(v-else-if="validatedDisallowPublish")
      MCallout(title="Cannot Publish Game Config" variant="danger")
        span This config cannot be validated for publishing because it contains errors.
        span Please view the #[MTextButton(:to="`/gameConfigs/${gameConfigId}`") config's details page] to see the errors.
    div(v-else)
      p Publishing #[MBadge(variant="neutral") {{ gameConfigName }}] will make it the active game config, effective immediately.
      p.small.text-muted Players will download the new config the next time they login. Currently live players will continue using the config they started with until the end of their current play session.
      p.small.text-muted Other people using the LiveOps Dashboard at the moment may be disrupted by the game data changing while they work, so make sure to let them know you are publishing an update!

      // R26 Temporarily disabled until it can be checked
      div(v-if="(archivableGameConfigIds ?? []).length > 0")
        div.d-flex.justify-content-between.mb-2
          span.font-weight-bold Archive All Older Configs
          MInputSwitch(
            :model-value="archiveOlderConfigs"
            @update:model-value="archiveOlderConfigs = $event"
            )
        div.small.text-muted At the same time as publishing this game config, you can also automatically archive {{ maybePlural(archivableGameConfigIds?.length, 'older unpublished config') }}. This is useful in keeping your game config history manageable.

</template>

<script lang="ts" setup>
import { computed, ref, watch } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast, maybePlural, useUiStore } from '@metaplay/meta-ui'
import { MActionModal, MBadge, MButton, MCallout, MInputSwitch, MTextButton } from '@metaplay/meta-ui-next'
import { fetchSubscriptionDataOnceOnly, useSubscription } from '@metaplay/subscriptions'

import { getAllGameConfigsSubscriptionOptions, getSingleGameConfigCountsSubscriptionOptions } from '../../subscription_options/gameConfigs'
import type { LibraryCountGameConfigInfo, MinimalGameConfigInfo } from '../../gameConfigServerTypes'

const gameServerApi = useGameServerApi()
const uiStore = useUiStore()

const props = defineProps<{
  /**
   * ID of the game config to publish.
   */
  gameConfigId: string
  /**
   * Optional: Whether to use a text button instead of a regular button.
   */
  textButton?: boolean
  /**
   * Optional: Publish is not possible.
   */
  publishBlocked?: boolean
}>()

/**
 * Reference to the game config publish modal.
 */
const gameConfigPublishModal = ref<typeof MActionModal>()

/**
 * Get a list of "archivable" game config IDs. These are game configs that are older than the one being published, and
 * have not been published themselves.
 */
const archivableGameConfigIds = computed((): string[] => {
  const targetGameConfig = allGameConfigsData.value?.find((x: MinimalGameConfigInfo) => x.id === props.gameConfigId)
  if (targetGameConfig) {
    return (allGameConfigsData.value ?? [] as MinimalGameConfigInfo[])
      // Legacy builds may not have a publishedAt date, even though they are active, so the isActive check is also
      // needed here for now. (18/12/23)
      .filter((x: MinimalGameConfigInfo) => !x.isArchived && !(x.publishedAt !== null || x.unpublishedAt !== null || x.isActive) && x.buildStartedAt < targetGameConfig?.buildStartedAt)
      .map((x: MinimalGameConfigInfo) => x.id)
  } else {
    return []
  }
})

/**
 * If `true` then we also automatically archive older unpublished game configs when publishing this one.
 */
const archiveOlderConfigs = ref(false)

/**
 * Watch for changes in `archiveOlderConfigs` and automatically update the UI store.
 */
watch(() => archiveOlderConfigs.value, (newValue) => {
  uiStore.toggleAutoArchiveWhenPublishing(newValue)
})

/**
 * Name of the game config.
 */
const gameConfigName = computed(() => {
  return singleGameConfigWithoutContents.value?.name ?? 'No name available'
})

/**
 * Fetch all available game configs
 */
const {
  data: allGameConfigsData,
  refresh: allGameConfigsRefresh,
} = useSubscription<MinimalGameConfigInfo[]>(getAllGameConfigsSubscriptionOptions())

/**
 * Game config data without the detailed content.
 */
const singleGameConfigWithoutContents = computed((): MinimalGameConfigInfo | undefined => {
  if (allGameConfigsData.value) {
    return allGameConfigsData.value.find((x) => x.id === props.gameConfigId)
  } else {
    return undefined
  }
})

/**
 * Returns a reason why this config cannot be published, or undefined if it can. This is based on limited information
 * so it's a best guess only. It's possible that this returns true yet the config can still not be published.
 */
const bestGuessDisallowPublishReason = computed((): string | undefined => {
  if (singleGameConfigWithoutContents.value?.isActive) {
    return 'This game config is already active.'
  } else if (singleGameConfigWithoutContents.value?.publishBlockingErrors.length ?? props.publishBlocked) {
    return 'Cannot publish a game config that contains errors.'
  } else {
    return undefined
  }
})

/**
 * Can the config really be published? This is expensive to fetch, so we only fetch when the modal is opened.
 */
const validatedDisallowPublish = ref<boolean>()

/**
 * Called when the modal is about to be shown.
 */
function onShow () {
  // Figure out if the config can really be published. Note that this request will complete almost immediately in some
  // cases, causing a messy visual flick as the loading spinner is shown and then hidden again. To avoid this, we add
  // an artificial short delay first so that the spinner is always visible.
  validatedDisallowPublish.value = undefined
  setTimeout(() => {
    void fetchSubscriptionDataOnceOnly<LibraryCountGameConfigInfo>(getSingleGameConfigCountsSubscriptionOptions(props.gameConfigId))
      .then(data => {
        validatedDisallowPublish.value = data?.publishBlockingErrors.length > 0
      })
  }, 1000)

  // Default value for deleting older archives.
  archiveOlderConfigs.value = uiStore.autoArchiveWhenPublishing
}

/**
 * Publish the displayed game config to the server.
 */
async function publishConfig () {
  // Publish the config.
  await gameServerApi.post('/gameConfig/publish?parentMustMatchActive=false', { Id: props.gameConfigId })
  showSuccessToast('Game config published.')

  // Archive old configs.
  if (archiveOlderConfigs.value && archivableGameConfigIds.value.length > 0) {
    for (let id = 0; id < archivableGameConfigIds.value.length; ++id) {
      const gameConfigId = archivableGameConfigIds.value[id]
      await gameServerApi.post(`/gameConfig/${gameConfigId}`, { isArchived: true })
    }
    showSuccessToast(`Archived ${archivableGameConfigIds.value.length} game configs.`)
  }

  // Force reload the page as new configs are now in play.
  // TODO: look into hot-loading the configs instead to solve this for all other dash users as well while making it less intrusive.
  // NOTE: This will lose the toasts that we just created..
  window.location.reload()
}
</script>
