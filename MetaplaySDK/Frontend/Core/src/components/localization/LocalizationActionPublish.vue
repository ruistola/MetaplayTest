<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
//- TODO: Migrate to MActionModal
meta-action-modal-button.ml-1(
  :id="`publish-localization-${localizationId}`"
  permission="api.localization.edit"
  action-button-text="Publish"
  modal-title="Publish Localization"
  ok-button-text="Publish Localization"
  :on-ok="publishLocalization"
  :ok-button-disabled="validatedDisallowPublish !== false"
  :on-show="onShow"
  :action-button-disabled="bestGuessDisallowPublishReason !== undefined || publishBlocked"
  :action-button-tooltip="bestGuessDisallowPublishReason"
  :link="link"
  noUnderline
  )
    div(v-if="validatedDisallowPublish === undefined").w-100.text-center.my-5
      div.font-weight-bold Validating Localization
      div.small.text-muted.mt-1 Please wait while we check that this localization is valid for publishing.
      b-spinner(label="Validating...").tw-mt-3
    div(v-else-if="validatedDisallowPublish")
      MCallout(title="Cannot Publish Localization" variant="danger")
        span This localization cannot be validated for publishing because it contains errors.
        span Please view the #[MTextButton(:to="`/localizations/${localizationId}`") localization's details page] to see the errors.
    div(v-else)
      p Publishing #[MBadge(variant="neutral") {{ localizationName }}] will make it the active localization, effective immediately.
      p.small.text-muted Players will download the new localization the next time they login. Currently live players will continue using the localization they started with until the end of their current play session.
      p.small.text-muted Other people using the LiveOps Dashboard at the moment may be disrupted by the game data changing while they work, so make sure to let them know you are publishing an update!

      // R26 Temporarily disabled until it can be checked
      div(v-if="(archivableLocalizationIds ?? []).length > 0")
        div.d-flex.justify-content-between.mb-2
          span.font-weight-bold Archive All Older Localizations
          MInputSwitch(
            :model-value="archiveOlderLocalizations"
            @update:model-value="archiveOlderLocalizations = $event"
            )
        div.small.text-muted At the same time as publishing this localization, you can also automatically archive {{ maybePlural(archivableLocalizationIds?.length, 'older unpublished localizations') }}. This is useful in keeping your localization history manageable.
</template>

<script lang="ts" setup>
import { computed, ref, watch } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast, maybePlural, useUiStore } from '@metaplay/meta-ui'
import { MBadge, MCallout, MInputSwitch, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getAllLocalizationsSubscriptionOptions } from '../../subscription_options/localization'
import type { MinimalLocalizationInfo } from '../../localizationServerTypes'

const gameServerApi = useGameServerApi()
const uiStore = useUiStore()

const props = defineProps<{
  /**
   * ID of the localization to publish.
   */
  localizationId: string
  /**
   * Optional: Whether to use a link button instead of a regular button.
   */
  link?: boolean
  /**
   * Optional: Publish is not possible.
   */
  publishBlocked?: boolean
}>()

/**
 * Get a list of "archivable" localization IDs. These are localizations that are older than the one being published, and
 * have not been published themselves.
 */
const archivableLocalizationIds = computed((): string[] => {
  const targetLocalization = allLocalizationsData.value?.find((x: MinimalLocalizationInfo) => x.id === props.localizationId)
  if (targetLocalization) {
    return (allLocalizationsData.value ?? [] as MinimalLocalizationInfo[])
      // Legacy builds may not have a publishedAt date, even though they are active, so the isActive check is also
      // needed here for now. (18/12/23)
      .filter((x: MinimalLocalizationInfo) => !x.isArchived && !(x.publishedAt !== null || x.unpublishedAt !== null || x.isActive) && x.buildStartedAt < targetLocalization?.buildStartedAt)
      .map((x: MinimalLocalizationInfo) => x.id)
  } else {
    return []
  }
})

/**
 * If `true` then we also automatically archive older unpublished localizations when publishing this one.
 */
const archiveOlderLocalizations = ref(false)

/**
 * Watch for changes in `archiveOlderLocalizations` and automatically update the UI store.
 */
watch(() => archiveOlderLocalizations.value, (newValue) => {
  uiStore.toggleAutoArchiveWhenPublishing(newValue)
})

/**
 * Name of the localization.
 */
const localizationName = computed(() => {
  return singleLocalizationWithoutContents.value?.name ?? 'No name available'
})

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
 * Returns a reason why this localization cannot be published, or undefined if it can. This is based on limited information
 * so it's a best guess only. It's possible that this returns true yet the localization can still not be published.
 */
const bestGuessDisallowPublishReason = computed((): string | undefined => {
  if (singleLocalizationWithoutContents.value?.isActive) {
    return 'This localization is already active.'
  } else if (singleLocalizationWithoutContents.value?.publishBlockingErrors.length ?? props.publishBlocked) {
    return 'Cannot publish a localization that contains errors.'
  } else {
    return undefined
  }
})

/**
 * Can the localization really be published? This is expensive to fetch, so we only fetch when the modal is opened.
 */
const validatedDisallowPublish = ref<boolean>()

/**
 * Called when the modal is about to be shown.
 */
function onShow () {
  // Figure out if the localization can really be published. Note that this request will complete almost immediately in some
  // cases, causing a messy visual flick as the loading spinner is shown and then hidden again. To avoid this, we add
  // an artificial short delay first so that the spinner is always visible.
  validatedDisallowPublish.value = undefined
  setTimeout(() => {
    // In game configs we have to actually make a request to server to check if the config can be published. We don't
    // have that here, so we just set it to true if there are any errors. We want to keep the same flow in this
    // component (ie: the `validation` state) so that the two components remain broadly similar.
    validatedDisallowPublish.value = !!singleLocalizationWithoutContents.value?.publishBlockingErrors.length
  }, 1000)

  // Default value for deleting older archives.
  archiveOlderLocalizations.value = uiStore.autoArchiveWhenPublishing
}

/**
 * Publish the displayed localization to the server.
 */
async function publishLocalization () {
  // Publish the localization.
  await gameServerApi.post('/localization/publish?parentMustMatchActive=false', { Id: props.localizationId })
  showSuccessToast('Localization published.')

  // Archive old configs.
  if (archiveOlderLocalizations.value && archivableLocalizationIds.value.length > 0) {
    for (let id = 0; id < archivableLocalizationIds.value.length; ++id) {
      const localizationId = archivableLocalizationIds.value[id]
      await gameServerApi.post(`/localization/${localizationId}`, { isArchived: true })
    }
    showSuccessToast(`Archived ${archivableLocalizationIds.value.length} localizations.`)
  }

  // Force reload the page as new localizations are now in play.
  // TODO: look into hot-loading the localizations instead to solve this for all other dash users as well while making it less intrusive.
  // NOTE: This will lose the toasts that we just created..
  window.location.reload()
}
</script>
