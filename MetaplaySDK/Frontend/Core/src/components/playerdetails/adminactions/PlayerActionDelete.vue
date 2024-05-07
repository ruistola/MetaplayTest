<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  MActionModalButton(
    modal-title="Schedule Player for Deletion"
    :action="updateDeletionSchedule"
    :trigger-button-label="!isCurrentlyScheduledForDeletion ? 'Delete Player' : 'Cancel Deletion'"
    trigger-button-full-width
    variant="danger"
    :ok-button-label="okButtonLabel"
    :ok-button-disabled="!isFormChanged"
    @show="resetModal()"
    permission="api.players.edit_scheduled_deletion"
    data-testid="action-delete-player"
    )
    div(class="tw-flex tw-justify-between")
      span(class="tw-font-semibold") Scheduled for deletion
      MInputSwitch(
        :model-value="scheduleForDeletion"
        @update:model-value="scheduleForDeletion = $event"
        class="tw-relative tw-top-1 tw-mr-1"
        name="isPlayerScheduledToBeDeleted"
        size="sm"
        data-testid="player-delete-toggle"
        )

    MInputDateTime(
      :model-value="scheduledDateTime"
      @update:model-value="onScheduleDateTimeChange"
      min-date-time="now"
      :disabled="!scheduleForDeletion"
      )

    span(class="tw-text-sm tw-text-neutral-500")
      p(v-if="!isCurrentlyScheduledForDeletion && !scheduleForDeletion")
        | This player is not currently scheduled for deletion.
      p(v-else-if="!isCurrentlyScheduledForDeletion && scheduleForDeletion && scheduledDateTime")
        | This player will be deleted #[span.font-weight-bold #[meta-time(:date="scheduledDateTime" showAs="timeago")]].
      p(v-else-if="isCurrentlyScheduledForDeletion && !scheduleForDeletion && scheduledDateTime")
        | This player will no longer be deleted.
      p(v-else-if="scheduledDateTime")
        | This player is currently scheduled for deletion #[span.font-weight-bold #[meta-time(:date="scheduledDateTime" showAs="timeago")]].
        | {{ deletionStatusText }}

    b-alert(show variant="secondary" v-if="!isCurrentlyBanned && !isCurrentlyScheduledForDeletion")
      p Scheduling a player for deletion does not prevent the player from playing the game. The player can still connect and play the game until the deletion has completed. If you wish to stop the player from connecting you should also ban them.
    b-alert(show variant="secondary" v-if="isCurrentlyBanned && isCurrentlyScheduledForDeletion")
      p The player is currently banned and will not be able to play the game, even if you cancel the scheduled deletion. To allow the player to play the game you must also un-ban them.

</template>

<script lang="ts" setup>
import { DateTime } from 'luxon'
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton, MInputDateTime, MInputSwitch } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { useCoreStore } from '../../../coreStore'
import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'
import { parseDotnetTimeSpanToLuxon } from '../../../coreUtils'

const props = defineProps<{
  /**
   * Id of the player to target the reset action at.
   **/
  playerId: string
}>()

const gameServerApi = useGameServerApi()
const coreStore = useCoreStore()

/**
 * Subscribe to player data used to render this component.
 */
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

/**
 * Specifies the date and time when the target player will be deleted.
 */
const scheduledDateTime = ref<DateTime>(DateTime.now().plus({ days: 7 }))

/**
 * When true player is to be deleted at the target date and time.
 */
const scheduleForDeletion = ref(false)

/**
 * Utility function to prevent undefined inputs.
 */
function onScheduleDateTimeChange (value?: DateTime) {
  if (!value) return
  scheduledDateTime.value = value
}

/**
 * When true the target player is currently scheduled for deletion.
 */
const isCurrentlyScheduledForDeletion = computed(() => {
  return playerData.value.model.deletionStatus !== 'None'
})

/**
 * Checks whether the player is currently banned.
 */
const isCurrentlyBanned = computed(() => {
  return playerData.value.model.isBanned
})

const okButtonLabel = computed(() => {
  if (scheduleForDeletion.value && !isScheduledDateTimeInTheFuture.value) {
    return 'Delete Immediately'
  } else if (scheduleForDeletion.value) {
    return 'Schedule for Deletion'
  } else {
    return 'Unschedule for Deletion'
  }
})

/**
 * Checks whether the deletion status or scheduled deletion date/time of a target player has been changed.
 */
const isFormChanged = computed(() => {
  if (scheduleForDeletion.value !== isCurrentlyScheduledForDeletion.value) {
    // Toggle was changed.
    return true
  } else if (scheduleForDeletion.value && !scheduledDateTime.value.equals(DateTime.fromISO(playerData.value.model.scheduledForDeletionAt))) {
    // Was already scheduled, but the date was changed.
    return true
  } else {
    return false
  }
})

/**
 * Indicates whether the target player will be deleted immediately or at a future date/time.
 */
const isScheduledDateTimeInTheFuture = computed(() => {
  if (!scheduledDateTime.value) return false
  else return scheduledDateTime.value.diff(DateTime.now()).toMillis() >= 0
})

/**
 * Human readable description of how the player's deletion was scheduled.
 */
const deletionStatusText = computed(() => {
  switch (playerData.value.model.deletionStatus) {
    case 'ScheduledByAdmin':
      return 'The deletion was scheduled by a dashboard user.'
    case 'ScheduledByUser':
      return 'The deletion was requested in-game by the player.'
    case 'ScheduledBySystem':
      return 'The deletion was scheduled by an automated system.'
    case 'None':
    case 'Deleted':
    default:
      return 'Unexpected deletion status.'
  }
})

/**
 * Reset state of the modal.
 */
function resetModal () {
  scheduleForDeletion.value = isCurrentlyScheduledForDeletion.value
  if (scheduleForDeletion.value) {
    // User has specified an exact time.
    scheduledDateTime.value = DateTime.fromISO(playerData.value.model.scheduledForDeletionAt)
  } else {
    // Use default date of current time + delay.
    const delay = parseDotnetTimeSpanToLuxon(coreStore.hello.playerDeletionDefaultDelay)
    const delayedDateTime = DateTime.now().plus(delay)
    scheduledDateTime.value = delayedDateTime
  }
}

/**
 * Update the date and time when the target player is to be deleted.
 */
async function updateDeletionSchedule () {
  const message = `${playerData.value.model.playerName || 'n/a'} ${scheduleForDeletion.value ? 'scheduled for deletion' : 'is no longer scheduled for deletion'}.`
  if (scheduleForDeletion.value) {
    await gameServerApi.put(`/players/${playerData.value.id}/scheduledDeletion`, { scheduledForDeleteAt: scheduledDateTime.value })
  } else {
    await gameServerApi.delete(`/players/${playerData.value.id}/scheduledDeletion`)
  }
  showSuccessToast(message)
  playerRefresh()
}
</script>
