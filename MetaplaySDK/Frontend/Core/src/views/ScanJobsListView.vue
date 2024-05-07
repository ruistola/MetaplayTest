<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :is-loading="!allScanJobsData"
  :meta-api-error="allScanJobsError"
  :alerts="alerts"
)
  template(#overview)
    meta-page-header-card(data-testid="scan-jobs-overview")
      template(#title) Database Scan Jobs

      p Scan jobs are slow-running workers that crawl the database and perform operations on its data.
      div.small.text-muted You can create your own scan jobs and tune the performance of the job runners to perform complicated operations on live games for hundreds of millions of players.

      template(#buttons)
        div(class="tw-space-x-2")
          MActionModalButton(
            modal-title="Pause All Database Scan Jobs"
            :action="onPauseAllJobsOk"
            :trigger-button-label="!allScanJobsData?.globalPauseIsEnabled ? 'Pause Jobs' : 'Resume Jobs'"
            ok-button-label="Apply"
            :ok-button-disabled="willPauseAllJobs === allScanJobsData?.globalPauseIsEnabled"
            variant="warning"
            permission="api.scan_jobs.manage"
            data-testid="pause-all-jobs"
            @show="willPauseAllJobs = allScanJobsData?.globalPauseIsEnabled"
            )
            div(class="tw-flex tw-justify-between")
              span(class="tw-font-semibold") Scan Jobs Paused
              MInputSwitch(
                :model-value="willPauseAllJobs"
                @update:model-value="willPauseAllJobs = $event"
                class="tw-relative tw-top-1 tw-mr-2"
                name="pauseAllScanJobs"
                size="sm"
                data-testid="pause-jobs-toggle"
                )
            p You can pause the execution of all database scan jobs. This can be helpful to debug the performance of slow-running jobs.

          MActionModalButton(
            modal-title="Create a New Maintenance Scan Job"
            :action="onNewScanJobOk"
            trigger-button-label="Create Job"
            ok-button-label="Create Job"
            :ok-button-disabled="!selectedJobKind"
            variant="primary"
            permission="api.scan_jobs.manage"
            data-testid="new-scan-job"
            @show="selectedJobKind = null"
            )
              p Database maintenance jobs handle routine operations such as deleting players. They are safe to use in production, but it is still a good idea to try them once is staging to verify that the various jobs do what you expect them to to!

              div(class="tw-font-semibold") Scan Job Type
              meta-input-select(
                data-testid="job-kind-select"
                :value="selectedJobKind?.id ?? 'none'"
                @input="selectJobKind"
                :options="jobKindOptions"
                :variant="selectedJobKind ? 'success' : 'default'"
                )

              div(v-if="selectedJobKind" class="tw-mt-2 tw-text-xs tw-text-neutral-500") {{ selectedJobKind.spec.jobDescription }}

  template(#default)
    core-ui-placement(:placement-id="'ScanJobs/List'")

    meta-raw-data(:kvPair="allScanJobsData" name="scanJobs")
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import type { MetaInputSelectOption, MetaPageContainerAlert } from '@metaplay/meta-ui'
import { MActionModalButton, MInputSwitch } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import CoreUiPlacement from '../components/system/CoreUiPlacement.vue'

import { getAllMaintenanceJobTypesSubscriptionOptions, getAllScanJobsSubscriptionOptions } from '../subscription_options/scanJobs'

const gameServerApi = useGameServerApi()
const { data: allMainentenanceJobTypesData } = useSubscription(getAllMaintenanceJobTypesSubscriptionOptions())
const {
  data: allScanJobsData,
  error: allScanJobsError,
  refresh: allScanJobsRefresh,
} = useSubscription(getAllScanJobsSubscriptionOptions())

const jobKindOptions = computed((): Array<MetaInputSelectOption<string | null>> => {
  const options: Array<MetaInputSelectOption<string | null>> = [{
    value: 'none',
    id: 'Select a job type...',
  }]
  if (allMainentenanceJobTypesData.value?.supportedJobKinds) {
    for (const jobKindInfo of Object.values(allMainentenanceJobTypesData.value.supportedJobKinds) as any) {
      options.push({
        value: jobKindInfo.id,
        id: jobKindInfo.spec.jobTitle
      })
    }
  }
  return options
})

const selectedJobKind = ref<any>(null)
function selectJobKind (newSelectedJobKind: any): void {
  selectedJobKind.value = allMainentenanceJobTypesData.value.supportedJobKinds.find((jobKind: any) => jobKind.id === newSelectedJobKind)
}

async function onNewScanJobOk (): Promise<void> {
  const job = (await gameServerApi.post('/maintenanceJobs', { jobKindId: selectedJobKind.value.id })).data
  showSuccessToast(`New job '${job.spec.jobTitle}' enqueued.`)
  allScanJobsRefresh()
}

const willPauseAllJobs = ref(false)
async function onPauseAllJobsOk (): Promise<void> {
  await gameServerApi.post('databaseScanJobs/setGlobalPause', { isPaused: willPauseAllJobs.value })
  showSuccessToast(`All scan jobs ${willPauseAllJobs.value ? 'paused' : 'resumed'}.`)
  allScanJobsRefresh()
}

const alerts = computed(() => {
  const allAlerts: MetaPageContainerAlert[] = []
  if (allScanJobsData.value?.globalPauseIsEnabled === true) {
    allAlerts.push({
      title: 'All Scan Jobs Paused',
      message: 'All database scan jobs are currently paused. You can resume them by clicking the Resume Jobs button.',
      variant: 'warning',
      dataTestid: 'all-jobs-paused-alert',
    })
  }
  return allAlerts
})
</script>
