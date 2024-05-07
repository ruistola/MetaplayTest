<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
b-container(fluid).pb-5
  //- Show localization contents or an alert.
  div(v-if="localizationData?.bestEffortStatus === 'Building'")
    //- Localization still building.
    b-row.justify-content-center
      b-col(lg="8")
        b-alert(show) This localization is still building. Check back soon!

  div(v-else-if="localizationData?.bestEffortStatus === 'Failed'")
    //- Localization was not built due to an error.
    b-row.justify-content-center
      b-col(lg="8")
        MCard(
          title="Build Error"
          subtitle="This localization failed to build correctly and has no configuration attached to it. The following errors were encountered:"
          )
          MErrorCallout(v-for="error in localizationData?.publishBlockingErrors" :error="localizationErrorToDisplayError(error)").mb-3

  div(v-else)
    localization-contents-card(:localizationId="localizationId")

</template>

<script lang="ts" setup>
import { computed } from 'vue'

import { MCard, MErrorCallout } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import type { MinimalLocalizationInfo } from '../../localizationServerTypes'
import { localizationErrorToDisplayError } from '../../localizationUtils'
import { getAllLocalizationsSubscriptionOptions } from '../../subscription_options/localization'
import LocalizationContentsCard from '../global/LocalizationContentsCard.vue'

const props = defineProps<{
  /**
   * Id of localization to display.
   */
  localizationId: string
}>()

// Load localization data ----------------------------------------------------------------------------------------------

/**
 * Fetch data for the specific localization that is to be displayed.
 */
const {
  data: allLocalizationsData,
} = useSubscription<MinimalLocalizationInfo[]>(getAllLocalizationsSubscriptionOptions())

const localizationData = computed((): MinimalLocalizationInfo | undefined => {
  return (allLocalizationsData.value ?? []).find((localization) => localization.id === props.localizationId)
})
</script>
