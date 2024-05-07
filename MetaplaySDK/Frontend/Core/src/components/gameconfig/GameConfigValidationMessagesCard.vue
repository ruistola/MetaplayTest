<template lang="pug">
meta-list-card(
  title="Validation Log"
  description="Messages generated during validation of the game config. Validation issues may label the game config as failed."
  icon="clipboard-list"
  :itemList="validationMessages"
  :searchFields="searchFields"
  :sortOptions="sortOptions"
  :filterSets="filterSets"
  :emptyMessage="validationMessagesMissing ? 'Logs not available.' : 'No log entries.'"
  ).list-group-stripes
  template(#item-card="{ item }")
    MCollapse(extraMListItemMargin)
      //- Row header
      template(#header)
        //- Title can be too long and truncate when width is too narrow.
        MListItem(noLeftPadding) {{ item.message }}
          template(#top-right)
            MBadge(:variant="messageLevelBadgeVariant(item.messageLevel)") {{ item.messageLevel }}
          template(#bottom-left)
            div {{ item.sheetName }} / {{ item.configKey }} / {{ item.columnHint }}
            div #[meta-plural-label(:value="item.variants.length" label="variant")] affected
          template(#bottom-right)
            MTextButton(v-if="isUrl(item.url)" :to="item.url") View source #[fa-icon(icon="external-link-alt" class="tw-ml-1")]

      //- Collapse content
      MList(class="tw-mt-2 tw-mb-3" showBorder striped)
        //- Source information.
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Library
          template(#top-right) {{ item.sheetName }}
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Config Key
          template(#top-right) {{ item.configKey }}
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Column
          template(#top-right) {{ item.columnHint }}
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Source
          template(#top-right)
            span(v-if="item.sourcePath")
              span(class="tw-mr-1 tw-font-mono").small {{ item.sourceMember }}:...{{ item.sourcePath.slice(-10) }}@{{ item.sourceLineNumber }}
              meta-clipboard-copy(:contents="`${item.sourceMember}:${item.sourcePath}@${item.sourceLineNumber}`")
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) URL
          template(#top-right)
            span(v-if="item.url")
              span(class="tw-mr-1 tw-font-mono").small ...{{ item.url.slice(-30) }}
              meta-clipboard-copy(:contents="item.url")
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Variants affected
          template(#top-right)
            span(v-if="item.variants.length > 0")
             div(v-for="(value, key) in item.variants" :key="key" class="tw-text-left") {{ value }}
            span(v-else class="tw-italic tw-text-neutral-500") None
        MListItem(class="tw-px-5 !tw-py-2.5" condensed) Additional data
          template(#top-right)
            span(v-if="item.additionalData")
              //- Not tested in UI, due to no data. Should it be div instead of span below?
              span(v-for="(value, key) in item.additionalData" :key="key") {{ key }}:{{ value }}
            span(v-else class="tw-italic tw-text-neutral-500") None
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import { MetaListFilterOption, MetaListFilterSet, MetaListSortOption, MetaListSortDirection } from '@metaplay/meta-ui'
import { MBadge, MTextButton, MCollapse, type Variant, MListItem, MList } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import type { GameConfigLogLevel, GameConfigValidationMessage, LibraryCountGameConfigInfo } from '../../gameConfigServerTypes'
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
 * Are the validation messages completely missing?
 */
const validationMessagesMissing = computed(() => {
  return gameConfigData.value?.contents === null
})

/**
 * Extract validation messages from the build report.
 */
const validationMessages = computed(() => {
  if (validationMessagesMissing.value) {
    return []
  } else {
    return gameConfigData.value?.contents?.metaData.buildReport?.validationMessages ?? undefined
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
  new MetaListSortOption('Warning Level', 'messageLevel', MetaListSortDirection.Ascending),
  new MetaListSortOption('Warning Level', 'messageLevel', MetaListSortDirection.Descending),
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
    new MetaListFilterSet('messageLevel',
      [
        new MetaListFilterOption('Verbose', (x: any) => x.messageLevel === 'Verbose'),
        new MetaListFilterOption('Debug', (x: any) => x.messageLevel === 'Debug'),
        new MetaListFilterOption('Information', (x: any) => x.messageLevel === 'Information'),
        new MetaListFilterOption('Warning', (x: any) => x.messageLevel === 'Warning'),
        new MetaListFilterOption('Error', (x: any) => x.messageLevel === 'Error'),
      ]
    ),
  ]
})

/**
 * Calculate variant (ie: color) for badges based on message level.
 * @param messageLevel Message level of warning.
 */
function messageLevelBadgeVariant (messageLevel: GameConfigLogLevel): Variant {
  const mappings: {[messageLevel: string]: Variant} = {
    Verbose: 'neutral',
    Debug: 'neutral',
    Information: 'primary',
    Warning: 'warning',
    Error: 'danger',
  }
  return mappings[messageLevel] ?? 'danger'
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
