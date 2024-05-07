<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
b-container(fluid).pb-5
  //- Show game config contents or an alert.
  div(v-if="gameConfigData?.status === 'Building'")
    //- Game config still building.
    b-row.justify-content-center
      b-col(lg="8")
        b-alert(show) This game config is still building. Check back soon!

  div(v-else-if="!gameConfigData?.contents?.sharedLibraries || !gameConfigData?.contents?.serverLibraries")
    //- Game config was not built due to an error.
    b-row.justify-content-center
      b-col(lg="8")
        MCard(
          title="Build Error"
          subtitle="This game config failed to build correctly and has no configuration attached to it. The following errors were encountered:"
          )
          MErrorCallout(v-for="error in gameConfigData?.publishBlockingErrors" :error="gameConfigErrorToDisplayError(error)").mb-3

  div(v-else)
    config-contents-card(:gameConfigId="gameConfigId" show-experiment-selector)

</template>

<script lang="ts" setup>
import { MCard, MErrorCallout } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import type { LibraryCountGameConfigInfo } from '../../gameConfigServerTypes'
import { gameConfigErrorToDisplayError } from '../../gameConfigUtils'
import { getSingleGameConfigCountsSubscriptionOptions } from '../../subscription_options/gameConfigs'
import ConfigContentsCard from '../global/ConfigContentsCard.vue'

const props = defineProps<{
  /**
   * Id of game config to display.
   */
  gameConfigId: string
}>()

// Load game config data ----------------------------------------------------------------------------------------------

/**
 * Fetch data for the specific game config that is to be displayed.
 */
const {
  data: gameConfigData,
} = useSubscription<LibraryCountGameConfigInfo>(getSingleGameConfigCountsSubscriptionOptions(props.gameConfigId))
</script>
