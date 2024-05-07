<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :variant="errorVariant"
  no-bottom-padding
  :is-loading="loadState === 'loading'"
  :show-error-card="loadState === 'error'"
  fluid
  :alerts="alerts"
  permission="api.game_config.view"
  )
  template(#overview)
    meta-page-header-card(v-if="gameConfigData" data-testid="game-config-overview" :id="gameConfigId")
      template(#title) {{ gameConfigData.name || 'No name available' }}
      template(#subtitle) {{ gameConfigData.description|| 'No description available' }}

      //- Overview.
      span.font-weight-bold #[fa-icon(icon="chart-bar")] Overview
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Status
            b-td.text-right
              MBadge(v-if="gameConfigData.isArchived" variant="neutral").mr-1 Archived
              MBadge(v-if="gameConfigData.isActive" variant="success").mr-1 Active
              MBadge(v-else) Not active
          b-tr
            b-td Experiments
            b-td(v-if="totalExperiments === undefined").text-right.text-muted.font-italic Loading...
            b-td(v-else-if="totalExperiments > 0").text-right {{ totalExperiments }}
            b-td(v-else).text-right.text-muted.font-italic None
          b-tr
            b-td Last Published At
            b-td.text-right
              meta-time(v-if="gameConfigData?.publishedAt" :date="gameConfigData?.publishedAt" showAs="timeagoSentenceCase")
              //- Legacy builds may not have a publishedAt date, even though they are active, so the isActive check is
              //- also needed here for now. (18/12/23)
              span(v-else-if="gameConfigData?.isActive").text-muted.font-italic No time recorded
              span(v-else).text-muted.font-italic Never
          b-tr
            b-td Last Unpublished At
            b-td.text-right
              meta-time(v-if="gameConfigData?.unpublishedAt" :date="gameConfigData?.unpublishedAt" showAs="timeagoSentenceCase")
              span(v-else).text-muted.font-italic Never

      //- Build Status.
      span.font-weight-bold #[fa-icon(icon="chart-bar")] Build Status
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Build Status
            b-td.text-right
              div {{ gameConfigData?.status }}
          b-tr
            b-td Built By
            b-td.text-right
              MBadge(v-if="gameConfigData.source === 'disk'") Built-in with the server
              meta-username(v-else :username="gameConfigData.source")
          b-tr(:class="{ 'text-danger': gameConfigData.buildReportSummary?.totalLogLevelCounts.Error }")
            b-td Logged Errors
            b-td(v-if="gameConfigData?.buildReportSummary?.totalLogLevelCounts.Error").text-right {{ gameConfigData.buildReportSummary?.totalLogLevelCounts.Error }}
            b-td(v-else-if="gameConfigData?.buildReportSummary === null").text-right.text-muted.font-italic Not available
            b-td(v-else).text-right.text-muted.font-italic None
          b-tr(:class="{ 'text-warning': gameConfigData?.buildReportSummary?.totalLogLevelCounts.Warning }")
            b-td Logged Warnings
            b-td(v-if="gameConfigData?.buildReportSummary?.totalLogLevelCounts.Warning").text-right {{ gameConfigData.buildReportSummary?.totalLogLevelCounts.Warning }}
            b-td(v-else-if="gameConfigData?.buildReportSummary === null").text-right.text-muted.font-italic Not available
            b-td(v-else).text-right.text-muted.font-italic None

      //- Technical Details.
      span.font-weight-bold #[fa-icon(icon="chart-bar")] Technical Details
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Built At
            b-td.text-right #[meta-time(:date="gameConfigData?.buildStartedAt" showAs="timeagoSentenceCase")]
          b-tr
            b-td Last Modified At
            b-td.text-right #[meta-time(v-if="gameConfigData" :date="gameConfigData?.lastModifiedAt" showAs="timeagoSentenceCase")]
          b-tr
            b-td Full Config Archive Version
            b-td(v-if="!gameConfigData").text-right.text-muted.font-italic Loading...
            b-td(v-else).text-right
              div(v-if="gameConfigData?.fullConfigVersion").text-monospace.small {{ gameConfigData.fullConfigVersion }}
              div(v-else).text-muted.font-italic Not available
          b-tr
            b-td Client Facing Version
            b-td(v-if="!gameConfigData").text-right.text-muted.font-italic Loading...
            b-td(v-else).text-right
              div(v-if="gameConfigData?.cdnVersion").text-monospace.small {{ gameConfigData.cdnVersion }}
              MTooltip(v-else content="Only available for the currently active game config.").text-muted.font-italic Not available

      template(#buttons)
        div(class="tw-inline-flex tw-space-x-1")
          MActionModalButton(
            modal-title="Edit Game Config Archive"
            :action="sendUpdatedConfigDataToServer"
            trigger-button-label="Edit"
            ok-button-label="Update"
            permission="api.game_config.edit"
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
            span.small.text-muted Archived config builds are hidden from the game configs list by default. Config builds that are active cannot be archived.

          MButton(
            :disabled="disallowDiffToActiveReason !== undefined"
            :disabled-tooltip="disallowDiffToActiveReason"
            :to="`diff?newRoot=${gameConfigId}`"
            ) Diff to Active

          game-config-action-publish(
            :gameConfigId="gameConfigId"
            :publishBlocked="gameConfigData?.publishBlockingErrors.length > 0"
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
        core-ui-placement(placementId="GameConfigs/Details/Tab0" :gameConfigId="gameConfigId" alwaysFullWidth)
      div(v-else-if="currentTabIndex === 1")
        core-ui-placement(placementId="GameConfigs/Details/Tab1" :gameConfigId="gameConfigId")
      div(v-else-if="currentTabIndex === 2")
        core-ui-placement(placementId="GameConfigs/Details/Tab2" :gameConfigId="gameConfigId")

    meta-raw-data(:kvPair="gameConfigData" name="gameConfigData")

  template(#error-card-message)
    div.mb-3 Oh no, something went wrong when retrieving this game config from the game server. Are you looking in the right deployment?
    MErrorCallout(:error="gameConfigError")
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'

import { useGameServerApi } from '@metaplay/game-server-api'
import { type MetaPageContainerAlert, maybePlural, maybePluralString, showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton, MBadge, MButton, MErrorCallout, MInputSwitch, MInputText, MInputTextArea, MTooltip } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import GameConfigActionPublish from '../components/gameconfig/GameConfigActionPublish.vue'
import CoreUiPlacement from '../components/system/CoreUiPlacement.vue'
import { getAllGameConfigsSubscriptionOptions, getSingleGameConfigCountsSubscriptionOptions } from '../subscription_options/gameConfigs'
import { useCoreStore } from '../coreStore'
import { routeParamToSingleValue } from '../coreUtils'
import type { LibraryCountGameConfigInfo, MinimalGameConfigInfo } from '../gameConfigServerTypes'

import useHeaderbar from '../useHeaderbar'

const gameServerApi = useGameServerApi()
const route = useRoute()
const coreStore = useCoreStore()

// Load game config data ----------------------------------------------------------------------------------------------

/**
 *  There are two sources of information for this page:
 * 1 - We subscribe to all gameconfig data and pull out just the game config that we're interested in. This loads fast
 *     and allows us to show the overview card very quickly while..
 * 2 - ..we are also subscribed to the full data for the game config that we're interested in. This is much slower to
 *    load because it includes the archive contents. We only need this contents for the the experiments info on the
 *    overview card so we don't want to have to wait for it to be loaded.
 */

/**
 * Fetch all available game configs
 */
const {
  refresh: allGameConfigsRefresh,
} = useSubscription<MinimalGameConfigInfo[]>(getAllGameConfigsSubscriptionOptions())

/**
 * Id of game config that is to be displayed.
 */
const gameConfigId = routeParamToSingleValue(route.params.id)

/**
 * Fetch data for the specific game config that is to be displayed.
 */
const {
  data: gameConfigData,
  error: gameConfigError
} = useSubscription<LibraryCountGameConfigInfo>(getSingleGameConfigCountsSubscriptionOptions(gameConfigId))

// Update the headerbar title dynamically as data changes.
useHeaderbar().setDynamicTitle(gameConfigData, (gameConfigRef) => `View ${gameConfigRef.value?.name ?? 'Config'}`)

/**
 * Indicates whether the page has completed loading or not.
 */
const loadState = computed(() => {
  if (gameConfigData.value) return 'loaded'
  else if (gameConfigError.value) return 'error'
  else return 'loading'
})

/**
 * Experiment data.
 */
const totalExperiments = computed(() => {
  return gameConfigData.value?.contents?.serverLibraries?.PlayerExperiments ?? 0
})

// UI Alerts ----------------------------------------------------------------------------------------------------------

/**
 * Array of error messages to be displayed in the event something goes wrong.
 */
const alerts = computed(() => {
  const allAlerts: MetaPageContainerAlert[] = []

  if (gameConfigData.value?.publishBlockingErrors.length) {
    allAlerts.push({
      title: 'Build Cannot Be Published',
      message: 'This build contains errors and cannot be published.',
      variant: 'danger',
      dataTestid: 'cannot-publish-alert'
    })
  }

  if (gameConfigData.value?.status === 'Building') {
    allAlerts.push({
      title: 'Config building...',
      message: 'This game config is still building and has no content to view for now.',
      variant: 'warning',
      dataTestid: 'building-alert'
    })
  } else if (gameConfigData.value?.libraryImportErrors && Object.keys(gameConfigData.value?.libraryImportErrors).length > 0) {
    allAlerts.push({
      title: 'Library Errors',
      message: 'One or more libraries failed to import.',
      variant: 'danger',
      dataTestid: 'libraries-fail-to-parse-alert'
    })
  } else if (gameConfigData.value?.buildReportSummary?.totalLogLevelCounts.Warning) {
    const warningCount = gameConfigData.value?.buildReportSummary.totalLogLevelCounts.Warning ?? 0
    allAlerts.push({
      title: `${gameConfigData.value.buildReportSummary.totalLogLevelCounts.Warning} Build Warnings`,
      message: `There ${maybePluralString(warningCount, 'was', 'were')} ${maybePlural(warningCount, 'warning')}  when building this config.
        You can still publish it, but it may not work as expected. You can view the full build log for more information.`,
      variant: 'warning',
      dataTestid: 'build-warnings-alert'
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

// Modify the game config ---------------------------------------------------------------------------------------------

/**
 * Information that is to be modified in the game config modal.
 */
interface GameConfigModalInfo {
  /**
   * Display name of the game config.
  */
  name: string
  /**
   * Optional description of what is unique about the game config build.
   */
  description: string
  /**
   * Indicates whether the game config has been archived.
  */
  isArchived: boolean
}

/**
 * Game config data to be modified in the modal.
 */
const editModalConfig = ref<GameConfigModalInfo>({
  name: '',
  description: '',
  isArchived: false
})

/**
 * Reset edit modal.
 */
function resetForm () {
  editModalConfig.value = {
    name: gameConfigData.value?.name ?? '',
    description: gameConfigData.value?.description ?? '',
    isArchived: gameConfigData.value?.isArchived ?? false
  }
}

/**
 * Take game config build data from the modal and send it to the server.
 */
async function sendUpdatedConfigDataToServer () {
  const params = {
    name: editModalConfig.value.name,
    description: editModalConfig.value.description,
    isArchived: editModalConfig.value.isArchived
  }
  await gameServerApi.post(`/gameConfig/${gameConfigId}`, params)
  showSuccessToast('Game config updated.')
  allGameConfigsRefresh()
}

/**
 * Returns a reason why this config cannot be diffed against the active config, or undefined if it can.
 */
const disallowDiffToActiveReason = computed((): string | undefined => {
  if (gameConfigData.value?.isActive) {
    return 'Cannot diff this config against itself.'
  } else if (gameConfigData.value?.status !== 'Success') {
    return 'Cannot diff a config that is not in a valid state.'
  } else {
    return undefined
  }
})

/**
 * Can the displayed game config can be archived?
 */
const canArchive = computed(() => {
  return !gameConfigData.value?.isActive
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
    id: 'Build Log',
    value: '1'
  },
  {
    id: 'Audit Log',
    value: '2'
  }]
}

/**
 * Get placement ID for a tab by its index.
 * @param tabIndex Index of tab to get placement for.
 */
function getUiPlacementForTabByIndex (tabIndex: number) {
  if (tabIndex === 0) return 'GameConfigs/Details/Tab0'
  else if (tabIndex === 1) return 'GameConfigs/Details/Tab1'
  else if (tabIndex === 2) return 'GameConfigs/Details/Tab2'
  else throw new Error('Trying to find a placement for a tab that does not exist!')
}

</script>
