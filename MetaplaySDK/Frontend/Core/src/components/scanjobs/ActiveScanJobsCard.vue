<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-list-card(
  data-testid="active-scan-jobs-card"
  icon="business-time"
  title="Active Scan Jobs"
  :itemList="activeAndEnqueuedJobs"
  :getItemKey="getItemKey"
  emptyMessage="No active scan jobs."
  )
  template(#item-card="slot")
    MCollapse(extraMListItemMargin data-testid="scan-jobs-entry")
      template(#header)
        MListItem(noLeftPadding) {{ slot.item.jobTitle }}
          template(#top-right)
            span(class="tw-space-x-0.5")
              MTooltip(:content="slot.item.canCancel ? 'Cancel this scan job.' : undefined" no-underline)
                MIconButton(
                  @click="cancelScanJobsModal?.open(slot.item); jobToCancel = slot.item"
                  variant="danger"
                  :disabled="slot.item.canCancel === false"
                  :disabled-tooltip="slot.item.cannotCancelReason"
                  permission="api.scan_jobs.manage"
                  aria-label="Cancel this scan job."
                  )
                  fa-icon(icon="trash-alt")
              MTooltip(v-if="slot.item.anyWorkersFailed" content="Some workers failed during this job." noUnderline).mr-2.text-danger: fa-icon(icon="triangle-exclamation")
              MBadge(:variant="getJobVariant(slot.item)") {{ slot.item.phase }}

          template(#bottom-left)
            div(v-if="slot.item.phaseCategory === 'Upcoming'") {{ slot.item.jobDescription }}
              div(v-if="slot.item.startTime") Scheduled to start #[meta-time(:date="slot.item.startTime")].
            div(v-else) Started #[meta-time(:date="slot.item.startTime")].

          template(v-if="slot.item.phaseCategory === 'Active'" #bottom-right)
            div Items scanned: #[meta-abbreviate-number(:value="slot.item.scanStatistics.numItemsScanned")]
            div Progress: {{ Math.round(slot.item.scanStatistics.scannedRatioEstimate * 10000) / 100 }}%

      //- TODO: migrate this to use generated UI
      pre.code-box.border.rounded.bg-light {{ slot.item }}
      //- meta-generated-content(
        :value="slot.item"
        )
    MActionModal(
      ref="cancelScanJobsModal"
      :title="`Cancel ${jobToCancel?.jobTitle}`"
      :action="cancelScanJob"
      @hidden="jobToCancel = null"
      )
      p(v-if="jobToCancel.phaseCategory === 'Upcoming'") This job has not yet been started. Cancelling an upcoming job will delete it, and it will not appear in the job history list.
        p(class="tw-text-xs tw-text-neutral-500 tw-mt-2") Note: The job may already be in progress by the time the request is processed.

      p(v-else) This job has been started and may already have executed partially. Cancelling an active job will terminate it as soon as possible and move it into the job history list.
      meta-no-seatbelts(message="Are you sure you want to cancel this scan job?")

</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MCollapse, MTooltip, MBadge, MListItem, MIconButton, MActionModal } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getAllScanJobsSubscriptionOptions } from '../../subscription_options/scanJobs'

const {
  data: allScanJobsData,
  refresh: allScanJobsRefresh,
} = useSubscription(getAllScanJobsSubscriptionOptions())

const cancelScanJobsModal = ref<typeof MActionModal>()

const gameServerApi = useGameServerApi()

const activeAndEnqueuedJobs = computed(() => {
  const active = allScanJobsData.value.activeJobs
  const upcoming = allScanJobsData.value.upcomingJobs
  return active.concat(upcoming)
})

function getJobVariant (job: any) {
  if (job.phase === 'Paused') return 'danger'
  else if (job.phase === 'Running') return 'success'
  else if (job.phase === 'Enqueued') return 'warning'
  else if (job.phase === 'Scheduled') return 'primary'
  else return 'neutral'
}

const jobToCancel = ref<any>()
async function cancelScanJob () {
  const response = (await gameServerApi.put(`/databaseScanJobs/${jobToCancel.value.id}/cancel`)).data
  showSuccessToast(response.message)
  allScanJobsRefresh()
}

function getItemKey (job: any) {
  return job.id
}
</script>

<style scoped>
.progress-text-overlay {
  filter: drop-shadow(0px 1px 0px rgba(255, 255, 255, 0.2));
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-size: 0.65rem;
}
</style>
