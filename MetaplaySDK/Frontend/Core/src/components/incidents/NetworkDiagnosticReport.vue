<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
b-row(v-if="playerIncidentData && playerIncidentData.networkReport")
  b-col(sm="6")
    h6 Game Server Probes
    MList(v-if="gameServerProbes && gameServerProbes.length > 0" showBorder)
      div(v-for="probe in gameServerProbes")
        MListItem(v-for="(gateway, port) in probe.gateways" :key="port" class="tw-px-3")
          span(:class="isEndpointSuccess(gateway) ? '' : 'text-danger'") {{ probe.hostname }} : {{ port }}
          template(#badge)
            span(class="tw-text-xs+ tw-text-neutral-500 tw-relative tw-top-0.5") {{ prettyPrintLatency(getEndpointDuration(gateway)) }}
          template(#top-right)
            MBadge(v-if="isEndpointSuccess(gateway)" variant="success") Success
            MBadge(v-else variant="danger") Failed
          template(#bottom-left)
            div.text-monospace.log.border.rounded.bg-light.w-100.mt-1(v-if="!isEndpointSuccess(gateway)")
              div(v-for="(step, stepName) in gateway" :key="stepName")
                pre(v-if="step && typeof(step) === 'object'")
                  b-row(no-gutters)
                    span
                      span(:class="step.isSuccess ? 'text-success' : 'text-danger'") {{ stepName }}
                      span(class="tw-text-xs+ tw-text-neutral-500 tw-relative tw-top-0.5")  {{ prettyPrintLatency(parseDotnetTimeSpanToLuxon(step.elapsed)) }}
                  b-row(no-gutters v-if="!step.isSuccess")
                    span.text-danger Error: {{ step.error }}
    div(v-else class="tw-text-red-500 tw-italic tw-text-xs+") Data unavailable for this probe.

    h6.mt-3 HTTP Probes
    MList(v-if="httpProbes && httpProbes.length > 0" showBorder)
      div(v-for="probe in httpProbes" :key="probe.name")
        MListItem(v-if="probe.result.status !== null" class="tw-px-3")
          span(:class="probe.result.status.isSuccess ? '' : 'text-danger'") {{ probe.name }}

          template(#badge)
            span(class="tw-text-xs+ tw-text-neutral-500 tw-relative tw-top-0.5") {{ prettyPrintLatency(parseDotnetTimeSpanToLuxon(probe.result.status.elapsed)) }}
          template(#top-right)
            MBadge(v-if="probe.result.status.isSuccess" variant="success") Success
            MBadge(v-else variant="danger") Failed
          template(#bottom-left)
            div.text-monospace.log.border.rounded.bg-light.w-100.mt-1(v-if="!probe.result.status.isSuccess")
              span.text-danger {{ probe.result.status.error }}
        MListItem(v-else class="tw-px-3")
          span.text-danger {{ probe.name }}
          template(#top-right)
            MBadge(variant="danger") No data
    div(v-else class="tw-text-red-500 tw-italic tw-text-xs+") Data unavailable for this probe.

  b-col(sm="6")
    h6 Internet Probes
    MList(v-if="internetProbes && internetProbes.length > 0" showBorder)
      MListItem(v-for="gateway in internetProbes" :key="gateway.name" class="tw-px-3")
        span(:class="isEndpointSuccess(gateway.result) ? '' : 'text-danger'") {{ gateway.name }}
        template(#badge)
          span(class="tw-text-xs+ tw-text-neutral-500 tw-relative tw-top-0.5")  {{ prettyPrintLatency(getEndpointDuration(gateway.result)) }}
        template(#top-right)
          MBadge(v-if="isEndpointSuccess(gateway.result)" variant="success") Success
          MBadge(v-else variant="danger") Failed
        template(#bottom-left)
          pre.text-monospace.log.border.rounded.bg-light.w-100.mt-1.mb-0(v-if="!isEndpointSuccess(gateway.result)")
            template(v-for="(step, stepName) in gateway.result" :key="stepName")
              div(v-if="step && stepName !== '$type'" :class="step.isSuccess ? 'text-success' : 'text-danger'") {{ stepName }} #[span.small.text-muted {{ prettyPrintLatency(parseDotnetTimeSpanToLuxon(step.elapsed)) }}]
                div(v-if="!step.isSuccess").text-danger Error: {{ step.error }}
    div(v-else class="tw-text-red-500 tw-italic tw-text-xs+") Data unavailable for this probe.
</template>

<script lang="ts" setup>
import { Duration } from 'luxon'
import { computed } from 'vue'

import { MBadge, MList, MListItem } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getPlayerIncidentSubscriptionOptions } from '../../subscription_options/incidents'

import { parseDotnetTimeSpanToLuxon } from '../../coreUtils'

const props = defineProps<{
  /**
   * ID of the incident to show.
   */
  incidentId: string
  /**
   * ID of the player to show.
   */
  playerId: string
}>()

const {
  data: playerIncidentData,
} = useSubscription(getPlayerIncidentSubscriptionOptions(props.playerId, props.incidentId))

const report = playerIncidentData.value?.networkReport

const gameServerProbes = computed(() => {
  return [
    report.gameServerIPv4,
    report.gameServerIPv6
  ]
})
const internetProbes = computed((): Array<{ name: string, result: { [key: string]: any } }> => {
  return [
    { name: 'CDN IPv4', result: report.gameCdnSocketIPv4 },
    { name: 'Google.com IPv4', result: report.googleComIPv4 },
    { name: 'Microsoft.com IPv4', result: report.microsoftComIPv4 },
    { name: 'Apple.com IPv4', result: report.appleComIPv4 },
    { name: 'CDN IPv6', result: report.gameCdnSocketIPv6 },
    { name: 'Google.com IPv6', result: report.googleComIPv6 },
    { name: 'Microsoft.com IPv6', result: report.microsoftComIPv6 },
    { name: 'Apple.com IPv6', result: report.appleComIPv6 }
  ]
})
const httpProbes = computed(() => {
  return [
    { name: 'CDN IPv4', result: report.gameCdnHttpIPv4 },
    { name: 'CDN IPv6', result: report.gameCdnHttpIPv6 }
  ]
})

function prettyPrintLatency (input: any) {
  const duration = Duration.fromISO(input)
  return duration.as('milliseconds') + 'ms'
}

function isEndpointSuccess (endpoint: any) {
  let result = true
  for (const key in endpoint) {
    if (endpoint[key]?.isSuccess === false) {
      result = false
    }
  }
  return result
}

function getEndpointDuration (endpoint: any) {
  let result = Duration.fromMillis(0)
  for (const key in endpoint) {
    if (endpoint[key]?.elapsed) {
      const elapsed = parseDotnetTimeSpanToLuxon(endpoint[key].elapsed)
      if (elapsed > result) {
        result = elapsed
      }
    }
  }

  return result
}
</script>

<style scoped>
.log {
  font-size: 8pt;
  padding: 0.5rem;
  overflow-wrap: break-word;
  word-break: break-all;
  overflow: scroll;
}

.log pre {
  overflow: visible;
  margin: 0;
}
</style>
