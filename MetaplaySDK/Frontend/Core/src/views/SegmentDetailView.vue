<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :is-loading="!singlePlayerSegmentData"
  :meta-api-error="singlePlayerSegmentError"
  )
  template(#error-card-message)
    p Oh no, something went wrong while trying to access the segment!

  template(#overview)
    meta-page-header-card(data-testid="segment-overview" :id="singlePlayerSegmentData.details.info.segmentId")
      template(#title) {{ singlePlayerSegmentData.details.info.displayName }}
      template(#subtitle) {{ singlePlayerSegmentData.details.info.description }}

      div.font-weight-bold #[fa-icon(icon="chart-bar")] Overview
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Audience Size Estimate
            b-td.text-right #[meta-audience-size-estimate(:sizeEstimate="singlePlayerSegmentData.details.sizeEstimate")]

  template(#default)
    b-row(no-gutters align-v="center").mt-3.mb-2
      h3 Configuration

    b-row(align-h="center")
      b-col(lg="6").mb-3
        b-card(title="Conditions" data-testid="segment-conditions").h-100.shadow-sm
          //- Property requirements
          span(v-if="singlePlayerSegmentData.details.info.playerCondition.propertyRequirements")
              div
                span.font-weight-bold Must match #[span.font-weight-bold.font-italic all] properties
              div.pl-2.pr-2.pt-1.pb-1.bg-light.rounded.border.mb-3
                b-table-simple(small responsive).m-0
                  b-thead
                    b-tr
                      b-th.border-0.pl-0 Property
                      b-th.border-0.text-center Min
                      b-th.border-0.text-center Max
                  b-tbody
                    b-tr(v-for="requirement in singlePlayerSegmentData.details.info.playerCondition.propertyRequirements" :key="requirement.id.displayName")
                      b-td(style="padding-left: .1rem").small {{ requirement.id.displayName }}
                      b-td.text-center.small {{ requirement.min?.constantValue }}
                      b-td.text-center.small {{ requirement.max?.constantValue }}

          //- ANY segment requirements
          span(v-if="singlePlayerSegmentData.details.info.playerCondition.requireAnySegment && singlePlayerSegmentData.details.info.playerCondition.requireAnySegment.length > 0")
            div.mb-1
              span.font-weight-bold(v-if="singlePlayerSegmentData.details.info.playerCondition.propertyRequirements") And must match #[span.font-weight-bold.font-italic any] segments from
              span.font-weight-bold(v-else) Must match #[span.font-weight-bold.font-italic at least one] of these segments:
            MList(showBorder)
              MListItem(
                v-for="requiredSegment in singlePlayerSegmentData.details.info.playerCondition.requireAnySegment"
                :key="requiredSegment"
                class="tw-px-3"
                condensed
                )
                span #[fa-icon(icon="user-tag").mr-1] {{ getSegmentNameById(requiredSegment) }}
                template(#top-right)
                  MTextButton(:to="`/segments/${requiredSegment}`") View segment

          //- ALL segment requirements
          span(v-if="singlePlayerSegmentData.details.info.playerCondition.requireAllSegments && singlePlayerSegmentData.details.info.playerCondition.requireAllSegments.length > 0")
            div.mb-1
              span.font-weight-bold(v-if="singlePlayerSegmentData.details.info.playerCondition.propertyRequirements || singlePlayerSegmentData.details.info.playerCondition.requireAnySegment") And must match #[span.font-weight-bold.font-italic all] segments from
              span.font-weight-bold(v-else) Must match #[span.font-weight-bold.font-italic all] of these segments:
            MList(showBorder)
              MListItem(
                v-for="requiredSegment in singlePlayerSegmentData.details.info.playerCondition.requireAllSegments"
                :key="requiredSegment"
                class="tw-px-3"
                condensed
                )
                span #[fa-icon(icon="user-tag").mr-1] {{ getSegmentNameById(requiredSegment) }}
                template(#top-right)
                  MTextButton(:to="`/segments/${requiredSegment}`") View segment

      b-col(lg="6")
        meta-list-card.h-100(
          data-testid="segment-references"
          title="Referenced by"
          icon="exchange-alt"
          tooltip="Other game systems that reference this segment in their conditions or targeting."
          :itemList="referenceList"
          :searchFields="['displayName', 'type']"
          :filterSets="filterSets"
          :sortOptions="referencesSortOptions"
          emptyMessage="No game systems reference this segment."
          )
          template(#item-card="{ item: segmentReference }")
            MListItem
              MBadge
                template(#icon)
                  fa-icon(:icon="segmentReference.icon")
                | {{ segmentReference.displayType }}
              span(class="tw-ml-1") {{ segmentReference.displayName }}

              template(#top-right v-if="segmentReference.linkUrl")
                MTextButton(:to="segmentReference.linkUrl") View {{ `${segmentReference.linkText.toLocaleLowerCase()}` }}

              template(#bottom-right v-if="segmentReference.type === 'Activable'")
                //- meta-activable-phase-badge(:activable="slot.item.id") <- TODO figure this out

    meta-raw-data(:kvPair="singlePlayerSegmentData" name="segment")
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'

import { routeParamToSingleValue } from '../coreUtils'
import { MetaListFilterOption, MetaListFilterSet, MetaListSortDirection, MetaListSortOption } from '@metaplay/meta-ui'
import { MBadge, MList, MListItem, MTextButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { useCoreStore } from '../coreStore'
import { getPlayerSegmentsSubscriptionOptions, getSinglePlayerSegmentSubscriptionOptions, getStaticConfigSubscriptionOptions } from '../subscription_options/general'

import MetaAudienceSizeEstimate from '../components/MetaAudienceSizeEstimate.vue'

const coreStore = useCoreStore()
const route = useRoute()
const segmentId = routeParamToSingleValue(route.params.id)
/**
 * Fetching all segments for getSegmentNameById function below. Edge case scenario that we want avoid in the future.
 */
const { data: allPlayerSegmentsData } = useSubscription(getPlayerSegmentsSubscriptionOptions())

const {
  data: singlePlayerSegmentData,
  error: singlePlayerSegmentError
} = useSubscription(getSinglePlayerSegmentSubscriptionOptions(segmentId))

const {
  data: staticConfigData
} = useSubscription(getStaticConfigSubscriptionOptions())

// Segment data ----------------------------------------------------------------------------------------------------

/**
 * Utility function to get the display name of a segment by its ID.
 * TODO: Single endpoint that has segment requirements should return its segment name in addition to segment id.
 * @param id Id of the selected segment
 */
function getSegmentNameById (id: string): string {
  if (allPlayerSegmentsData.value?.segments) return (Object.values(allPlayerSegmentsData.value.segments).find((x: any) => x.info.configKey === id) as any).info.displayName
  else return 'Loading...'
}

/**
 * Utility computed to get the "selected" segment's metadata.
 */
const activablesMetadata = computed(() => {
  return staticConfigData.value?.serverReflection.activablesMetadata
})

/**
 * Information for a single reference.
 */
interface ReferenceDetails {
  type: string
  displayType: string
  displayName: string
  icon: string
  linkUrl: string
  linkText: string
  activableCategory?: string
}

/**
 * Computed to get all segments that reference the "selected" segment.
 */
const referenceList = computed((): ReferenceDetails[] => {
  // Decorate references with links
  return singlePlayerSegmentData.value.details.usedBy.map((referenceSource: any) => {
    switch (referenceSource.type) {
      case 'Activable':
      {
        const kindId = referenceSource.subtype
        const categoryId = activablesMetadata.value.kinds[kindId].category
        const categoryName = activablesMetadata.value.categories[categoryId].shortSingularDisplayName
        const urlPathName = '/' + (coreStore.gameSpecific.activableCustomization[categoryId]?.pathName || `activables/${categoryId}`)
        return {
          type: referenceSource.type,
          displayType: categoryName,
          displayName: referenceSource.displayName,
          icon: coreStore.gameSpecific.activableCustomization[categoryId]?.icon || 'calendar-alt',
          linkUrl: `${urlPathName}/${kindId}/${referenceSource.id}`,
          linkText: categoryName,
          activableCategory: categoryId,
        }
      }
      case 'Offer':
        return {
          type: referenceSource.type,
          displayType: referenceSource.type,
          displayName: referenceSource.displayName,
          icon: 'tags',
          linkUrl: `/offerGroups/offer/${referenceSource.id}`,
          linkText: referenceSource.type,
        }
      case 'Broadcast':
        return {
          type: referenceSource.type,
          displayType: referenceSource.type,
          displayName: referenceSource.displayName,
          icon: 'broadcast-tower',
          linkUrl: `/broadcasts/${referenceSource.id}`,
          linkText: referenceSource.type,
        }
      case 'Experiment':
        return {
          type: referenceSource.type,
          displayType: referenceSource.type,
          displayName: referenceSource.displayName,
          icon: 'flask',
          linkUrl: `/experiments/${referenceSource.id}`,
          linkText: referenceSource.type,
        }
      case 'Notification':
        return {
          type: referenceSource.type,
          displayType: referenceSource.type,
          displayName: referenceSource.displayName,
          icon: 'comment-alt',
          linkUrl: `/notifications/${referenceSource.id}`,
          linkText: referenceSource.type,
        }
      case 'Segment':
        return {
          type: referenceSource.type,
          displayType: referenceSource.type,
          displayName: referenceSource.displayName,
          icon: 'user-tag',
          linkUrl: `/segments/${referenceSource.id}`,
          linkText: referenceSource.type,
        }
      case 'LiveOpsEvent':
        return {
          type: referenceSource.type,
          displayType: referenceSource.type,
          displayName: referenceSource.displayName,
          icon: 'calendar-alt',
          linkUrl: `/liveOpsEvents/${referenceSource.id}`,
          linkText: 'event',
        }
      default:
        return {
          type: referenceSource.type,
          displayType: referenceSource.type,
          displayName: referenceSource.displayName,
          icon: 'question-circle',
          linkUrl: '',
          linkText: '',
        }
    }
  })
})

// Filtering ----------------------------------------------------------------------------------------------------------

function activableKindFilters (): MetaListFilterOption[] {
  const filters = []
  for (const categoryId in activablesMetadata.value.categories) {
    const category = activablesMetadata.value.categories[categoryId]
    filters.push(new MetaListFilterOption(category.displayName, (x: any) => x.type === 'Activable' && x.activableCategory === categoryId))
  }
  return filters
}

const filterSets = computed((): MetaListFilterSet[] => {
  return [
    new MetaListFilterSet('type',
      [
        new MetaListFilterOption('Segments', (x: any) => x.type === 'Segment'),
        new MetaListFilterOption('Broadcasts', (x: any) => x.type === 'Broadcast'),
        new MetaListFilterOption('Experiments', (x: any) => x.type === 'Experiment'),
        new MetaListFilterOption('Notifications', (x: any) => x.type === 'Notification'),
        new MetaListFilterOption('Offers', (x: any) => x.type === 'Offer'),
        new MetaListFilterOption('LiveOps Events', (x: any) => x.type === 'LiveOpsEvent'),
        ...activableKindFilters()
      ].sort((a, b) => a.displayName.localeCompare(b.displayName)) // Sort alphabetically
    )
  ]
})

const referencesSortOptions = [
  new MetaListSortOption('Type', 'type', MetaListSortDirection.Ascending),
  new MetaListSortOption('Type', 'type', MetaListSortDirection.Descending),
  new MetaListSortOption('Name', 'displayName', MetaListSortDirection.Ascending),
  new MetaListSortOption('Name', 'displayName', MetaListSortDirection.Descending)
]
</script>
