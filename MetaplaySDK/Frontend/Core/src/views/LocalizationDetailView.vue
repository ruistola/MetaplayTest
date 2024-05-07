<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :variant="errorVariant"
  no-bottom-padding
  :is-loading="loadState === 'loading'"
  :show-error-card="loadState === 'error'"
  fluid
  :alerts="alerts"
  permission="api.localization.view"
  )
  template(#overview)
    meta-page-header-card(v-if="localizationData" data-testid="localization-overview" :id="localizationId")
      template(#title) {{ localizationData.name || 'No name available' }}
      template(#subtitle) {{ localizationData.description|| 'No description available' }}

      //- Overview.
      span.font-weight-bold #[fa-icon(icon="chart-bar")] Overview
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Status
            b-td.text-right
              MBadge(v-if="localizationData.isArchived" variant="neutral").mr-1 Archived
              MBadge(v-if="localizationData.isActive" variant="success").mr-1 Active
              MBadge(v-else) Not active
          b-tr
            b-td Last Published At
            b-td.text-right
              meta-time(v-if="localizationData?.publishedAt" :date="localizationData?.publishedAt" showAs="timeagoSentenceCase")
              //- Legacy builds may not have a publishedAt date, even though they are active, so the isActive check is
              //- also needed here for now. (18/12/23)
              span(v-else-if="localizationData?.isActive").text-muted.font-italic No time recorded
              span(v-else).text-muted.font-italic Never
          b-tr
            b-td Last Unpublished At
            b-td.text-right
              meta-time(v-if="localizationData?.unpublishedAt" :date="localizationData?.unpublishedAt" showAs="timeagoSentenceCase")
              span(v-else).text-muted.font-italic Never

      //- Build Status.
      span.font-weight-bold #[fa-icon(icon="chart-bar")] Build Status
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Build Status
            b-td.text-right
              div {{ localizationData?.bestEffortStatus }}
          b-tr
            b-td Built By
            b-td.text-right
              MBadge(v-if="localizationData.source === 'disk'") Built-in with the server
              meta-username(v-else :username="localizationData.source")
          b-tr(:class="{ 'text-danger': localizationData.publishBlockingErrors.length > 0 }")
            b-td Publish Blocking Errors
            b-td.text-right
              span(v-if="localizationData.publishBlockingErrors.length") {{ localizationData.publishBlockingErrors.length }}
              span(v-else).text-muted.font-italic None

      //- Technical Details.
      span.font-weight-bold #[fa-icon(icon="chart-bar")] Technical Details
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Built At
            b-td.text-right #[meta-time(:date="localizationData?.buildStartedAt" showAs="timeagoSentenceCase")]
          b-tr
            b-td Last Modified At
            b-td.text-right #[meta-time(v-if="localizationData" :date="localizationData?.lastModifiedAt" showAs="timeagoSentenceCase")]
          b-tr
            b-td Version Hash
            b-td(v-if="!localizationData").text-right.text-muted.font-italic Loading...
            b-td(v-else).text-right
              div.text-monospace.small {{ localizationData.versionHash }}

      template(#buttons)
        div(class="tw-inline-flex tw-space-x-1")
          MActionModalButton(
            modal-title="Edit Localization Archive"
            :action="sendUpdatedLocalizationDataToServer"
            trigger-button-label="Edit"
            ok-button-label="Update"
            permission="api.localization.edit"
            data-testid="edit-config"
            @show="resetForm"
            )
            MInputText(
              label="Name"
              :model-value="editModalConfig.name"
              @update:model-value="editModalConfig.name = $event"
              :variant="editModalConfig.name.length > 0 ? 'success' : 'default'"
              placeholder="For example: 1.0.4 release candidate"
              class="tw-mb-2"
              )

            MInputTextArea(
              label="Description"
              :model-value="editModalConfig.description"
              @update:model-value="editModalConfig.description = $event"
              :variant="editModalConfig.description.length > 0 ? 'success' : 'default'"
              placeholder="What is unique about this config build that will help you find it later?"
              :rows="3"
              )
            div(class="tw-inline-flex tw-justify-between tw-items-center tw-w-full")
              span(class="tw-font-semibold") Archived
              MTooltip(
                :content="!canArchive? 'This config cannot be archived as it is currently active.' : undefined"
                no-underline
                )
                MInputSwitch(
                  :model-value="editModalConfig.isArchived"
                  @update:model-value="editModalConfig.isArchived = $event"
                  :disabled="!canArchive"
                  name="isConfigBuildArchived"
                  size="sm"
                )
            span.small.text-muted Archived localization builds are hidden from the localizations list by default. Localization builds that are active cannot be archived.

          MButton(
            :disabled-tooltip="disallowDiffToActiveReason"
            :to="`diff?newRoot=${localizationId}`"
            ) Diff to Active

          localization-action-publish(
            :localizationId="localizationId"
            :publishBlocked="localizationData?.publishBlockingErrors.length > 0"
            )

  template(#default)
    //- Tabs.
    div(
      class="tw-hidden sm:tw-flex tw-flex-row tw-justify-center tw-items-center tw-mb-10 tw-border-t tw-border-b tw-border-neutral-200 tw-bg-white tw-py-1 tw-sticky tw-z-10 tw-space-x-1"
      style="top: -1px;"
      )
        span(v-for="i in getTabMultiselectOptions()" :key="i.value")
          button(
            v-if="!coreStore.isUiPlacementEmpty(getUiPlacementForTabByIndex(0))"
            :class="['tw-font-semibold tw-rounded tw-py-2 tw-px-4', { 'tw-bg-blue-500 tw-text-white': currentTabIndex === parseInt(i.value), 'tw-text-blue-500 hover:tw-bg-neutral-300 active:tw-bg-neutral-400': currentTabIndex !== parseInt(i.value)}]"
            @click="tabClicked(parseInt(i.value))"
            :data-testid="`player-details-tab-${i}`"
            ) {{ i.id }}
    div(class="sm:tw-hidden tw-px-4 tw-mb-10")
      meta-input-select(
        :value="String(currentTabIndex)"
        :options="getTabMultiselectOptions()"
        @input="tabClicked"
        no-clear
        no-deselect
        )

    //- Selected tab content.
    b-container(style="min-height: 30rem;")
      div(v-if="currentTabIndex === 0")
        core-ui-placement(placementId="Localizations/Details/Tab0" :localizationId="localizationId" alwaysFullWidth)
      div(v-else-if="currentTabIndex === 1")
        core-ui-placement(placementId="Localizations/Details/Tab1" :localizationId="localizationId")

    meta-raw-data(:kvPair="localizationData" name="localizationData")

  template(#error-card-message)
    div.mb-3 Oh no, something went wrong when retrieving this localization from the game server. Are you looking in the right deployment?
    MErrorCallout(v-if="localizationError" :error="localizationError")
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'

import { useGameServerApi } from '@metaplay/game-server-api'
import { type MetaPageContainerAlert, showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton, MBadge, MButton, MErrorCallout, MInputSwitch, MInputText, MInputTextArea, MTooltip } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import LocalizationActionPublish from '../components/localization/LocalizationActionPublish.vue'
import CoreUiPlacement from '../components/system/CoreUiPlacement.vue'
import { getAllLocalizationsSubscriptionOptions } from '../subscription_options/localization'
import { useCoreStore } from '../coreStore'
import { routeParamToSingleValue } from '../coreUtils'
import type { MinimalLocalizationInfo } from '../localizationServerTypes'

import useHeaderbar from '../useHeaderbar'

const gameServerApi = useGameServerApi()
const route = useRoute()
const coreStore = useCoreStore()

// Load localization data ----------------------------------------------------------------------------------------------

/**
 * Fetch all available localizations
 */
const {
  data: allLocalizationsData,
  refresh: allLocalizationsRefresh,
} = useSubscription<MinimalLocalizationInfo[]>(getAllLocalizationsSubscriptionOptions())

/**
 * Id of localization that is to be displayed.
 */
const localizationId = routeParamToSingleValue(route.params.id)

const localizationData = computed(() => {
  return allLocalizationsData.value?.find((localization) => localization.id === localizationId)
})
const localizationError = computed(() => {
  if (allLocalizationsData.value) {
    return undefined
  } else {
    return new Error('No localization data found.')
  }
})

// Update the headerbar title dynamically as data changes.
useHeaderbar().setDynamicTitle(localizationData, (localizationRef) => `View ${localizationRef.value?.name ?? 'Localization'}`)

/**
 * Indicates whether the page has completed loading or not.
 */
const loadState = computed(() => {
  if (localizationData.value) return 'loaded'
  else if (localizationError.value) return 'error'
  else return 'loading'
})

// UI Alerts ----------------------------------------------------------------------------------------------------------

/**
 * Array of error messages to be displayed in the event something goes wrong.
 */
const alerts = computed(() => {
  const allAlerts: MetaPageContainerAlert[] = []

  if (localizationData.value?.publishBlockingErrors.length) {
    allAlerts.push({
      title: 'Build Cannot Be Published',
      message: 'This build contains errors and cannot be published.',
      variant: 'danger',
      dataTestid: 'cannot-publish-alert'
    })
  }

  if (localizationData.value?.bestEffortStatus === 'Building') {
    allAlerts.push({
      title: 'Config building...',
      message: 'This localization is still building and has no content to view for now.',
      variant: 'warning',
      dataTestid: 'building-alert'
    })
  }
  return allAlerts
})

/**
 * Custom background color that indicates the type of alert message.
 */
const errorVariant = computed(() => {
  if (alerts.value.find((alert) => alert.variant === 'danger')) return 'danger'
  else if (alerts.value.find((alert) => alert.variant === 'warning')) return 'warning'
  else return undefined
})

// Modify the localization --------------------------------------------------------------------------------------------

/**
 * Information that is to be modified in the localization modal.
 */
interface LocalizationModalInfo {
  /**
   * Display name of the localization.
  */
  name: string
  /**
   * Optional description of what is unique about the localization build.
   */
  description: string
  /**
   * Indicates whether the localization has been archived.
  */
  isArchived: boolean
}

/**
 * Localization data to be modified in the modal.
 */
const editModalConfig = ref<LocalizationModalInfo>({
  name: '',
  description: '',
  isArchived: false
})

/**
 * Reset edit modal.
 */
function resetForm () {
  editModalConfig.value = {
    name: localizationData.value?.name ?? '',
    description: localizationData.value?.description ?? '',
    isArchived: localizationData.value?.isArchived ?? false
  }
}

/**
 * Take localization build data from the modal and send it to the server.
 */
async function sendUpdatedLocalizationDataToServer () {
  const params = {
    name: editModalConfig.value.name,
    description: editModalConfig.value.description,
    isArchived: editModalConfig.value.isArchived
  }
  await gameServerApi.post(`/localization/${localizationId}`, params)
  showSuccessToast('Localization updated.')
  allLocalizationsRefresh()
}

/**
 * Returns a reason why this localization cannot be diffed against the active localization, or undefined if it can.
 */
const disallowDiffToActiveReason = computed((): string | undefined => {
  if (localizationData.value?.isActive) {
    return 'Cannot diff this localization against itself.'
  } else if (localizationData.value?.bestEffortStatus !== 'Success') {
    return 'Cannot diff a localization that is not in a valid state.'
  } else {
    return undefined
  }
})

/**
 * Can the displayed localization can be archived?
 */
const canArchive = computed(() => {
  return !localizationData.value?.isActive
})

// Tabs ---------------------------------------------------------------------------------------------------------------

const currentTabIndex = ref(0)

/**
 * Change active tab.
 * @param newTabIndex Index of tab to switch to.
 */
async function tabClicked (newTabIndex: number) {
  currentTabIndex.value = newTabIndex
}

/**
 * Get name and ID for each tab.
 */
function getTabMultiselectOptions () {
  return [{
    id: 'Details',
    value: '0'
  },
  {
    id: 'Audit Log',
    value: '1'
  }]
}

/**
 * Get placement ID for a tab by its index.
 * @param tabIndex Index of tab to get placement for.
 */
function getUiPlacementForTabByIndex (tabIndex: number) {
  if (tabIndex === 0) return 'Localizations/Details/Tab0'
  else if (tabIndex === 1) return 'Localizations/Details/Tab1'
  else throw new Error('Trying to find a placement for a tab that does not exist!')
}

</script>
