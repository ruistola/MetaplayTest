<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MListItem
  | {{ item.displayTitle }}
  span.small.text-muted  by #[meta-username(:username="item.source.sourceId")] #[meta-country-code(:isoCode="item.sourceCountryIsoCode ? item.sourceCountryIsoCode : undefined")]
  template(#top-right): meta-time(:date="item.createdAt")
  template(#bottom-left)
    div {{ item.displayDescription }}
    div(v-if="showTarget") Target:
      |
      |
      MTextButton(
        permission="api.audit_logs.search"
        data-testid="view-event-link"
        :to="`/auditLogs?targetType=${item.target.targetType}&targetId=${item.target.targetId}`"
        ) {{ item.target.targetType.replace(/\$/, '') }}:{{ item.target.targetId }}
  template(#bottom-right): MTextButton(:to="`/auditLogs/${item.eventId}`" permission="api.audit_logs.view") View log event
</template>

<script lang="ts" setup>
import { MListItem, MTextButton } from '@metaplay/meta-ui-next'

defineProps<{
  item?: any
  showTarget?: boolean
}>()
</script>
