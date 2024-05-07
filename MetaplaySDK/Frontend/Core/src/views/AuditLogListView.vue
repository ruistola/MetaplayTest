<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
//- The meta page container below is not doing any is loading or error handling.
meta-page-container
  template(#overview)
    meta-page-header-card
      template(#title) Search Audit Log Events

      b-row
        b-col(md).mb-3
          h6 By Event
          div.font-weight-bold.mb-1 Event Type
          meta-input-select.mb-2(
            :value="searchAuditLogEventsTargetType"
            @input="searchAuditLogEventsTargetType = $event"
            :options="targetTypes"
            )
          MInputText(
            label="Event ID"
            :model-value="searchAuditLogEventsTargetId"
            @update:model-value="searchAuditLogEventsTargetId = $event"
            :disabled="!searchAuditLogEventsTargetType"
            :variant="searchAuditLogEventIdUiState !== null ? searchAuditLogEventIdUiState ? 'success' : 'danger' : 'default'"
            :hint-message="!searchAuditLogEventsTargetType ? 'Can not search for IDs without a type selected.' : undefined"
            placeholder="For example: ClientCompatibility"
            )

        b-col(md)
          h6 By User
          MInputText.mb-2(
            label="User Account"
            :model-value="searchAuditLogEventsSourceId"
            @update:model-value="searchAuditLogEventsSourceId = $event"
            :variant="searchAuditLogEventUserAccountUiState !== null ? searchAuditLogEventUserAccountUiState ? 'success' : 'danger' : 'default'"
            placeholder="For example: teemu.haila@metaplay.io"
            :debounce="500"
            )
          MInputText.mb-2(
            label="User IP Address"
            :model-value="searchAuditLogEventsSourceIpAddress"
            @update:model-value="searchAuditLogEventsSourceIpAddress = $event"
            :variant="searchAuditLogEventUserIpAddressUiState !== null ? searchAuditLogEventUserIpAddressUiState ? 'success' : 'danger' : 'default'"
            placeholder="192.168.0.1"
            :debounce="500"
            )
          MInputText(
            label="User Country ISO Code"
            :model-value="searchAuditLogEventsSourceCountryIsoCode"
            @update:model-value="searchAuditLogEventsSourceCountryIsoCode = $event"
            :variant="searchAuditLogEventUserCountryIsoCodeUiState !== null ? searchAuditLogEventUserCountryIsoCodeUiState ? 'success' : 'danger' : 'default'"
            :hint-message="searchAuditLogEventUserCountryIsoCodeUiState === false ? 'Invalid ISO code.' : undefined"
            placeholder="FI"
            :debounce="500"
            )
  template(#default)
    div(v-if="showSearchResults")
      b-row
        b-col.mb-3
          b-card(title="Search Results").shadow-sm
            b-row(v-if="searchEvents.length > 0" no-gutters)
              b-table(small striped hover responsive :items="searchEvents" :fields="tableFields" primary-key="eventId" sort-by="startAt" sort-desc @row-clicked="clickOnEventRow" tbody-tr-class="table-row-link")
                template(#cell(target)="data")
                  span.text-nowrap {{ data.item.target.targetType.replace(/\$/, '') }}:{{ data.item.target.targetId }}

                template(#cell(displayTitle)="data")
                  MTooltip(:content="data.item.displayDescription" noUnderline).text-nowrap {{ data.item.displayTitle }}

                template(#cell(source)="data")
                  span.text-nowrap #[meta-username(:username="data.item.source.sourceId")]

                template(#cell(createdAt)="data")
                  meta-time(:date="data.item.createdAt").text-nowrap

              div(class="tw-mt-2 tw-w-full tw-flex tw-justify-center")
                MButton(v-if="searchEventsHasMore" size="small" @click="showMoreSearch") Load More

            b-row(v-else no-gutters align-h="center").mt-4.mb-3
              p.m-0.text-muted No search results.

      meta-raw-data(:kvPair="searchEvents" name="Search results")

    div(v-else)
      b-row
      b-col.mb-3
        b-card(title="Latest Audit Log Events").shadow-sm
          b-table.table-fixed-column(small striped hover responsive :items="latestEvents" :fields="tableFields" primary-key="eventId" sort-by="startAt" sort-desc @row-clicked="clickOnEventRow" tbody-tr-class="table-row-link")
            template(#cell(target)="data")
              span.text-nowrap {{ data.item.target.targetType.replace(/\$/, '') }}:{{ data.item.target.targetId }}

            template(#cell(displayTitle)="data")
              MTooltip(:content="data.item.displayDescription" noUnderline).text-nowrap {{ data.item.displayTitle }}

            template(#cell(source)="data")
              span.text-nowrap #[meta-username(:username="data.item.source.sourceId")]

            template(#cell(createdAt)="data")
              meta-time(:date="data.item.createdAt").text-nowrap

          div(class="tw-mt-2 tw-flex tw-w-full tw-justify-center")
            MButton(v-if="latestEventsHasMore" size="small" @click="showMoreLatest") Load More

      meta-raw-data(:kvPair="latestEvents" name="Latest events")
</template>

<script lang="ts" setup>
import { computed, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { isoCodeToCountryName, type MetaInputSelectOption } from '@metaplay/meta-ui'
import { MButton, MInputText, MTooltip } from '@metaplay/meta-ui-next'
import { useManuallyManagedStaticSubscription } from '@metaplay/subscriptions'

import { getAllAuditLogEventsSubscriptionOptions, getAuditLogEventsSearchSubscriptionOptions } from '../subscription_options/auditLogs'
import { useCoreStore } from '../coreStore'
import { extractSingleValueFromQueryStringOrDefault } from '../coreUtils'

const coreStore = useCoreStore()
const route = useRoute()
const router = useRouter()

const pageSize = 10

// The "search" results list ------------------------------------------------------------------------------------------

const searchAuditLogEventsTargetType = ref(extractSingleValueFromQueryStringOrDefault(route.query, 'targetType', ''))
const searchAuditLogEventsTargetId = ref(extractSingleValueFromQueryStringOrDefault(route.query, 'targetId', ''))
const searchAuditLogEventsSourceId = ref(extractSingleValueFromQueryStringOrDefault(route.query, 'sourceId', ''))
const searchAuditLogEventsSourceIpAddress = ref(extractSingleValueFromQueryStringOrDefault(route.query, 'sourceIpAddress', ''))
const searchAuditLogEventsSourceCountryIsoCode = ref(extractSingleValueFromQueryStringOrDefault(route.query, 'sourceCountryIsoCode', ''))

const targetTypes = computed((): Array<MetaInputSelectOption<string>> => {
  const targetTypes: Array<MetaInputSelectOption<string>> = [
    {
      value: 'Player',
      id: 'Player'
    },
    {
      value: '$GameServer',
      id: 'GameServer'
    },
    {
      value: '$GameConfig',
      id: 'GameConfig'
    },
    {
      value: '$Broadcast',
      id: 'Broadcast'
    },
    {
      value: '$Notification',
      id: 'Notification'
    },
    {
      value: '$Experiment',
      id: 'Experiment'
    },
    {
      value: 'AsyncMatchmaker',
      id: 'AsyncMatchmaker'
    },
  ]

  // Add types for features behind feature flags
  if (coreStore.hello.featureFlags.guilds) {
    targetTypes.push({
      value: 'Guild',
      id: 'Guild'
    })
  }
  if (coreStore.hello.featureFlags.web3) {
    targetTypes.push({
      value: '$Nft',
      id: 'NFT'
    },
    {
      value: '$NftCollection',
      id: 'NFT Collection'
    })
  }
  if (coreStore.hello.featureFlags.playerLeagues) {
    targetTypes.push({
      value: 'LeagueManager',
      id: 'League'
    })
  }
  if (coreStore.hello.featureFlags.localization) {
    targetTypes.push({
      value: '$Localization',
      id: 'Localization'
    })
  }

  return targetTypes
})

/**
 * Subscription details for search events.
 */
const searchEventsSubscription = ref<any>()

/**
 * Current fetch size for search events subscription.
 */
const searchEventsLimit = ref(pageSize)

/**
 * Is there a search in progress?
 */
const showSearchResults = computed(() => {
  return searchAuditLogEventTypeUiState.value ??
    searchAuditLogEventIdUiState.value ??
    searchAuditLogEventUserAccountUiState.value ??
    searchAuditLogEventUserIpAddressUiState.value ??
    searchAuditLogEventUserCountryIsoCodeUiState.value !== null
})

/**
 * Search events.
 */
const searchEvents = computed((): any[] => {
  return searchEventsSubscription.value?.data?.entries || []
})

/**
 * Are there more search events to fetch?
 */
const searchEventsHasMore = computed(() => {
  return !!searchEventsSubscription.value?.data?.hasMore
})

/**
 * Increase fetch size.
 */
function showMoreSearch () {
  searchEventsLimit.value += pageSize
  subscribeShowSearch()
}

/**
 * Set up new subscription for search events.
 */
function subscribeShowSearch () {
  if (searchEventsSubscription.value) {
    searchEventsSubscription.value.unsubscribe()
  }
  searchEventsSubscription.value = useManuallyManagedStaticSubscription(getAuditLogEventsSearchSubscriptionOptions(
    searchAuditLogEventsTargetType.value,
    searchAuditLogEventsTargetId.value,
    searchAuditLogEventsSourceId.value ? '$AdminApi:' + searchAuditLogEventsSourceId.value : '',
    searchAuditLogEventsSourceIpAddress.value,
    searchAuditLogEventsSourceCountryIsoCode.value,
    searchEventsLimit.value
  ))
}

/**
 * Kick off the initial subscription.
 */
subscribeShowSearch()

/**
 * Remember to unsubscribe when page unmounts.
 */
onUnmounted(() => {
  searchEventsSubscription.value.unsubscribe()
})

// If any search parameter updates...
watch([searchAuditLogEventsTargetType, searchAuditLogEventsTargetId, searchAuditLogEventsSourceId, searchAuditLogEventsSourceIpAddress, searchAuditLogEventsSourceCountryIsoCode, searchEventsLimit], async () => {
  // Update the URL with the new search parameters.
  const params: {[key: string]: string} = {}

  if (searchAuditLogEventsTargetType.value) {
    params.targetType = searchAuditLogEventsTargetType.value
    if (searchAuditLogEventsTargetId.value) {
      params.targetId = searchAuditLogEventsTargetId.value
    }
  }
  if (searchAuditLogEventsSourceId.value) {
    params.sourceId = searchAuditLogEventsSourceId.value
  }
  if (searchAuditLogEventsSourceIpAddress.value) {
    params.sourceIpAddress = searchAuditLogEventsSourceIpAddress.value
  }
  if (upperCasedSearchAuditLogEventsSourceCountryIsoCode.value) {
    params.sourceCountryIsoCode = upperCasedSearchAuditLogEventsSourceCountryIsoCode.value
  }

  // Touching the router will immediately re-initialize the whole page and thus reload the subscription.
  await router.replace({ path: '/auditLogs', query: params })
})

const upperCasedSearchAuditLogEventsSourceCountryIsoCode = computed(() => {
  return searchAuditLogEventsSourceCountryIsoCode.value ? searchAuditLogEventsSourceCountryIsoCode.value.toUpperCase() : null
})

/* UI state for the Event Type form component. */
const searchAuditLogEventTypeUiState = computed(() => {
  return searchAuditLogEventsTargetType.value ? true : null
})

/* UI state for the Event Id form component. */
const searchAuditLogEventIdUiState = computed(() => {
  return searchAuditLogEventsTargetId.value ? true : null
})

/* UI state for the User Account form component. */
const searchAuditLogEventUserAccountUiState = computed(() => {
  return searchAuditLogEventsSourceId.value ? true : null
})

/* UI state for the User IP Address form component. */
const searchAuditLogEventUserIpAddressUiState = computed(() => {
  return searchAuditLogEventsSourceIpAddress.value ? true : null
})

/* UI state for the User Country ISO Code form component. */
const searchAuditLogEventUserCountryIsoCodeUiState = computed(() => {
  const isoCode = upperCasedSearchAuditLogEventsSourceCountryIsoCode.value
  if (isoCode) {
    return isoCodeToCountryName(isoCode) !== isoCode
  } else {
    return null
  }
})

async function clearSearchAuditLogEventsType () {
  searchAuditLogEventsTargetType.value = ''
}

async function clearSearchAuditLogEventsTargetId () {
  searchAuditLogEventsTargetId.value = ''
}

async function clearSearchAuditLogEventsSourceId () {
  searchAuditLogEventsSourceId.value = ''
}

async function clearSearchAuditLogEventsSourceIpAddress () {
  searchAuditLogEventsSourceIpAddress.value = ''
}

async function clearSearchAuditLogEventsSourceCountryIsoCode () {
  searchAuditLogEventsSourceCountryIsoCode.value = ''
}

// The "latest" events list -------------------------------------------------------------------------------------------

/**
 * Subscription details for latest events.
 */
const latestEventsSubscription = ref<any>()

/**
 * Current fetch size for latest events subscription.
 */
const latestEventsLimit = ref(pageSize)

/**
 * Latest events.
 */
const latestEvents = computed((): any[] => {
  return latestEventsSubscription.value?.data?.entries || []
})

/**
 * Are there more latest events to fetch?
 */
const latestEventsHasMore = computed(() => {
  return !!latestEventsSubscription.value?.data?.hasMore
})

/**
 * Increase fetch size.
 */
function showMoreLatest () {
  latestEventsLimit.value += pageSize
  subscribeShowLatest()
}

/**
 * Set up new subscription for latest events.
 */
function subscribeShowLatest () {
  if (latestEventsSubscription.value) {
    latestEventsSubscription.value.unsubscribe()
  }
  latestEventsSubscription.value = useManuallyManagedStaticSubscription(getAllAuditLogEventsSubscriptionOptions(
    '',
    '',
    latestEventsLimit.value
  ))
}

/**
 * Kick off the initial subscription.
 */
subscribeShowLatest()

/**
 * Remember to unsubscribe when page unmounts.
 */
onUnmounted(() => {
  latestEventsSubscription.value.unsubscribe()
})

// Other --------------------------------------------------------------------------------------------------------------

const tableFields = [
  {
    key: 'target',
    label: 'Event'
  },
  {
    key: 'displayTitle',
    label: 'Title'
  },
  {
    key: 'source',
    label: 'User'
  },
  {
    key: 'createdAt',
    label: 'Date'
  }
]

async function clickOnEventRow (item: any) {
  await router.push(`/auditLogs/${item.eventId}`)
}
</script>
