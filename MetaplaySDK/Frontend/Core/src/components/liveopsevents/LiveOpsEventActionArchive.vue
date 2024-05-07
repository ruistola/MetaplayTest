<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MActionModalButton(
  modal-title="Archive LiveOps Event"
  :action="onOk"
  :trigger-button-label="singleLiveOpsEventData?.isArchived ? 'Unarchive' : 'Archive'"
  :trigger-button-disabled="singleLiveOpsEventData?.currentPhase !== 'Ended' ? 'Event must be concluded before it can be archived.' : undefined"
  ok-button-label="Save Settings"
  :ok-button-disabled="archiveState === initialArchiveState"
  @show="onShow"
  permission="api.liveops_events.edit"
  data-testid="`archive-event-${liveOpsEventId}`"
  )
  div(class="tw-flex tw-justify-between")
    div(class="tw-font-semibold") Archive LiveOps Event
    MInputSwitch(
      :model-value="archiveState"
      @update:model-value="archiveState = $event"
      name="archiveState"
      size="sm"
      data-testid="config-archive-toggle"
      )
  span.small.text-muted Archiving an event will hide it from the list of available events. An archived event can be unarchived at any time.
</template>

<script lang="ts" setup>
import { ref, watch } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton, MInputSwitch } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getSingleLiveOpsEventsSubscriptionOptions } from '../../subscription_options/liveOpsEvents'
import type { LiveOpsEventDetailsInfo } from '../../liveOpsEventServerTypes'

const gameServerApi = useGameServerApi()

const props = defineProps<{
  /**
   * ID of the event to archive.
   */
  eventId: string
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
 * Information about the event.
 */
const {
  data: singleLiveOpsEventData,
  refresh: singleLiveOpsEventRefresh,
} = useSubscription<LiveOpsEventDetailsInfo>(getSingleLiveOpsEventsSubscriptionOptions(props.eventId))

/**
 * Cache initial archive value when the data is available.
 */
watch(() => singleLiveOpsEventData.value, (value) => {
  if (value) {
    initialArchiveState.value = value.isArchived
  }
}, { immediate: true })

/**
 * Called when the modal is about to be shown.
 */
function onShow () {
  archiveState.value = singleLiveOpsEventData.value?.isArchived ?? false
}

/**
 * Called when the modal OK button is clicked.
 */
async function onOk () {
  const params = {
    isArchived: archiveState.value
  }
  await gameServerApi.post(`/setLiveOpsEventArchivedStatus/${props.eventId}`, params)
  showSuccessToast(`Event ${archiveState.value ? 'archived' : 'unarchived'}.`)
  singleLiveOpsEventRefresh()
}
</script>
