<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<!-- An element for use in the localization select component to show details about a given localization. -->

<template lang="pug">
div(style="font-size: 0.95rem;")
  div.font-weight-bold {{ localizationFromId?.name ?? 'No name available' }}
  div.small.font-mono {{ localizationId }}
  div.small(v-if="localizationFromId") Built #[meta-time(:date="localizationFromId?.buildStartedAt")]
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import { useSubscription } from '@metaplay/subscriptions'

import { getAllLocalizationsSubscriptionOptions } from '../../subscription_options/localization'
import type { MinimalLocalizationInfo } from '../../localizationServerTypes'

const props = defineProps<{
  /**
   * Id of the localization to show in the card.
   */
  localizationId: string
}>()

const {
  data: allLocalizationsData,
} = useSubscription<MinimalLocalizationInfo[]>(getAllLocalizationsSubscriptionOptions())

const localizationFromId = computed((): MinimalLocalizationInfo | undefined => {
  return (allLocalizationsData.value ?? []).find((localization) => localization.id === props.localizationId)
})
</script>
