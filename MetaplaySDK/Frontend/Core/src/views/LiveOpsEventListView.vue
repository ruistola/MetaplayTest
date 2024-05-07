<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  permission="api.liveops_events.view"
  )
  template(#overview)
    meta-page-header-card(data-testid="overview")
      template(#title) Live Ops Events
      p Live Ops Events are a way to schedule and deliver events to your players.

      template(#buttons)
        // TODO: trigger refresh for lists on create
        live-ops-event-form-modal-button(form-mode="create" @refresh="")
        import-live-ops-event-action-modal-button()

  template(#default)
    core-ui-placement(placementId="LiveOpsEvents/List")
    meta-raw-data(:kvPair="liveOpsEventsData" name="liveOpsEventsData")
    meta-raw-data(:kvPair="liveOpsEventTypesData" name="liveOpsEventTypesData")
</template>

<script lang="ts" setup>
import { useSubscription } from '@metaplay/subscriptions'

import ImportLiveOpsEventActionModalButton from '../components/liveopsevents/ImportLiveOpsEventActionModalButton.vue'
import LiveOpsEventFormModalButton from '../components/liveopsevents/LiveOpsEventFormModalButton.vue'
import CoreUiPlacement from '../components/system/CoreUiPlacement.vue'

import { getAllLiveOpsEventsSubscriptionOptions, getLiveOpsEventTypesSubscriptionOptions } from '../subscription_options/liveOpsEvents'
import type { GetLiveOpsEventsListApiResult, LiveOpsEventTypeInfo } from '../liveOpsEventServerTypes'

const {
  data: liveOpsEventsData
} = useSubscription<GetLiveOpsEventsListApiResult>(getAllLiveOpsEventsSubscriptionOptions())

const {
  data: liveOpsEventTypesData
} = useSubscription<LiveOpsEventTypeInfo[]>(getLiveOpsEventTypesSubscriptionOptions())
</script>
