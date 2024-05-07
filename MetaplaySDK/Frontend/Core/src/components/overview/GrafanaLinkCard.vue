<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
b-row.justify-content-center
  b-col(lg="9" xl="8")
    b-card(title="Cluster Metrics & Logs" :class="grafanaEnabled ? '' : 'bg-light'" style="min-height: 11rem").mb-3.shadow-sm
      div(v-if="grafanaEnabled")
        p Grafana is an industry standard tool for diving into the "engine room" level system health metrics and server logs.
        div(class="tw-text-right tw-space-x-1.5")
          MButton(permission="dashboard.grafana.view" :to="grafanaMetricsLink" :disabled="!grafanaEnabled") View Metrics
            template(#icon): fa-icon(icon="external-link-alt" class="tw-w-4 tw-h-3.5 tw-mr-1")
          MButton(permission="dashboard.grafana.view" :to="grafanaLogsLink" :disabled="!grafanaEnabled") View Logs
            template(#icon): fa-icon(icon="external-link-alt" class="tw-w-4 tw-h-3.5 tw-mr-1")
      div(v-else).text-center.mt-5
        div.text-muted Grafana has not been configured for this environment.

</template>

<script lang="ts" setup>
import { computed } from 'vue'
import { useCoreStore } from '../../coreStore'
import { MButton } from '@metaplay/meta-ui-next'

const coreStore = useCoreStore()

const grafanaEnabled = computed(() => !!coreStore.hello.grafanaUri)

/**
 * Link to Grafana metrics dashboard.
 */
const grafanaMetricsLink = computed(() => {
  if (grafanaEnabled.value) {
    return coreStore.hello.grafanaUri + '/d/rCI05Y4Mz/metaplay-server'
  } else {
    return undefined
  }
})

/**
 * Link to the Grafana Loki logs.
 */
const grafanaLogsLink = computed(() => {
  if (grafanaEnabled.value) {
    const namespaceStr = coreStore.hello.kubernetesNamespace ? `,namespace=\\"${coreStore.hello.kubernetesNamespace}\\"` : ''
    return `${coreStore.hello.grafanaUri}/explore?orgId=1&left={"datasource": "Loki", "queries":[{"expr":"{app=\\"metaplay-server\\"${namespaceStr}}"}],"range":{"from":"now-1h","to":"now"}}`
  } else {
    return undefined
  }
})
</script>
