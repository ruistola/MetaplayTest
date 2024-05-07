<template lang="pug">
meta-list-card(
  title="Build Log"
  description="Messages generated during the config build process. Errors will cause the build to fail."
  icon="clipboard-list"
  :itemList="buildMessages"
  :searchFields="searchFields"
  :sortOptions="sortOptions"
  :filterSets="filterSets"
  :emptyMessage="buildMessagesMissing ? 'Logs not available.' : 'No log entries.'"
  ).mb-3.list-group-stripes
  template(#item-card="{ item: buildReport }")
    MCollapse(extraMListItemMargin)
      //- Row header
      template(#header)
        //- Title can be too long and truncate when width is too narrow.
        MListItem(noLeftPadding) {{ buildReport.message }}
          template(#top-right)
            MBadge(:variant="messageLevelBadgeVariant(buildReport.level)") {{ buildReport.level }}
          template(#bottom-right)
            MTextButton(v-if="isUrl(buildReport.locationUrl)" :to="buildReport.locationUrl || ''") View source #[fa-icon(icon="external-link-alt")]

      //- Collapse content
      MList(class="tw-mt-2 tw-mb-3" showBorder striped)
        //- Source information.
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Source Info
          template(#top-right)
            span(v-if="buildReport.sourceInfo")
              span(class="tw-mr-1 tw-font-mono").small {{ buildReport.sourceInfo.slice(0, 30) }}...
              meta-clipboard-copy(:contents="`${buildReport.sourceInfo}`")
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Source Location
          template(#top-right)
            span(v-if="buildReport.sourceLocation")
              span(class="tw-mr-1 tw-font-mono").small {{ buildReport.sourceLocation.slice(0, 30) }}...
              meta-clipboard-copy(:contents="`${buildReport.sourceLocation}`")
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Location URL
          template(#top-right)
            span(v-if="buildReport.locationUrl")
              span(class="tw-mr-1 tw-font-mono").small ...{{ buildReport.locationUrl.slice(-30) }}
              meta-clipboard-copy(:contents="`${buildReport.locationUrl}`")
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Item ID
          template(#top-right)
            span(v-if="buildReport.itemId") {{ buildReport.itemId }}
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Variant ID
          template(#top-right)
            span(v-if="buildReport.variantId") {{ buildReport.variantId }}
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Exception
          template(#top-right)
            span(v-if="buildReport.exception") {{ buildReport.exception }}
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Caller
          template(#top-right)
            span(v-if="buildReport.callerFileName")
              span(class="tw-mr-1 tw-font-mono").small {{ buildReport.callerMemberName }}:...{{ buildReport.callerFileName.slice(-10) }}@{{ buildReport.callerLineNumber }}
              meta-clipboard-copy(:contents="`${buildReport.callerMemberName}:${buildReport.callerFileName}@${buildReport.callerLineNumber}`")
            span(v-else class="tw-italic tw-text-neutral-500") None
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import type { GameConfigLogLevel, GameConfigBuildMessage, LibraryCountGameConfigInfo } from '../../gameConfigServerTypes'

import { MetaListFilterOption, MetaListFilterSet, MetaListSortOption, MetaListSortDirection } from '@metaplay/meta-ui'
import { MBadge, MCollapse, type Variant, MListItem, MList, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getSingleGameConfigCountsSubscriptionOptions } from '../../subscription_options/gameConfigs'

const props = defineProps<{
  /**
   * Id of game config whose Build report we want to show.
   */
  gameConfigId: string
}>()

/**
 * Fetch data for the specific game config that is to be displayed.
 */
const {
  data: gameConfigData,
} = useSubscription<LibraryCountGameConfigInfo>(getSingleGameConfigCountsSubscriptionOptions(props.gameConfigId))

/**
 * Are the build messages completely missing?
 */
const buildMessagesMissing = computed(() => {
  return gameConfigData.value?.contents === null
})

/**
 * Extract build messages from the build report.
 */
const buildMessages = computed(() => {
  if (buildMessagesMissing.value) {
    return []
  } else {
    return gameConfigData.value?.contents?.metaData.buildReport?.buildMessages ?? undefined
  }
})

/**
 * Card search fields.
 */
const searchFields = [
  'message',
  'sheetName',
  'configKey',
  'columnHint',
]

/**
 * Card sort options.
 * */
const sortOptions = [
  new MetaListSortOption('Message', 'message', MetaListSortDirection.Ascending),
  new MetaListSortOption('Message', 'message', MetaListSortDirection.Descending),
  new MetaListSortOption('Warning Level', 'level', MetaListSortDirection.Ascending),
  new MetaListSortOption('Warning Level', 'level', MetaListSortDirection.Descending),
  new MetaListSortOption('Library', 'sheetName', MetaListSortDirection.Ascending),
  new MetaListSortOption('Library', 'sheetName', MetaListSortDirection.Descending),
  new MetaListSortOption('Config Key', 'configKey', MetaListSortDirection.Ascending),
  new MetaListSortOption('Config Key', 'configKey', MetaListSortDirection.Descending),
  new MetaListSortOption('Column', 'columnHint', MetaListSortDirection.Ascending),
  new MetaListSortOption('Column', 'columnHint', MetaListSortDirection.Descending),
  MetaListSortOption.asUnsorted(),
]

/**
 * Card filters.
 */
const filterSets = computed(() => {
  return [
    new MetaListFilterSet('level',
      [
        new MetaListFilterOption('Verbose', (x: any) => x.level === 'Verbose'),
        new MetaListFilterOption('Debug', (x: any) => x.level === 'Debug'),
        new MetaListFilterOption('Information', (x: any) => x.level === 'Information'),
        new MetaListFilterOption('Warning', (x: any) => x.level === 'Warning'),
        new MetaListFilterOption('Error', (x: any) => x.level === 'Error'),
      ]
    ),
  ]
})

/**
 * Calculate variant (ie: color) for badges based on message level.
 * @param level Message level of warning.
 */
function messageLevelBadgeVariant (level: GameConfigLogLevel): Variant {
  const mappings: {[level: string]: Variant} = {
    Verbose: 'neutral',
    Debug: 'neutral',
    Information: 'primary',
    Warning: 'warning',
    Error: 'danger',
  }
  return mappings[level] ?? 'danger'
}

/**
 * Crudely determine if the given string is a valid URL or not. Used to decide if a warning's URL should be a clickable
 * link or not.
 * @param url URL to check.
 */
function isUrl (url: string | undefined | null) {
  return url && (url.startsWith('http://') || url.startsWith('https://'))
}
</script>
