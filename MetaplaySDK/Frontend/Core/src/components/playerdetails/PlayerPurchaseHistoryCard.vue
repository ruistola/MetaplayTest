<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  meta-list-card(
    title="Purchase History"
    icon="money-check-alt"
    :itemList="displayedHistory"
    :searchFields="searchFields"
    :sortOptions="sortOptions"
    :defaultSortOption="defaultSortOption"
    :filterSets="filterSets"
    emptyMessage="No purchases... yet!"
    data-testid="player-purchase-history-card"
    ).mb-3
    template(#item-card="{ item: iap }")
      MCollapse(extraMListItemMargin)
        //- Row header
        template(#header)
          MListItem(noLeftPadding)
            span(class="tw-mr-1") {{ iap.productId }}
            span(v-if="iap.subscriptionQueryResult === null" class="tw-text-neutral-500") (${{ iap.referencePrice.toFixed(2) }})
            span(v-else) (
              MTooltip(content="Subscription purchases are not applied directly towards the player's total spend. Please see Subscription History instead.") ${{ iap.referencePrice.toFixed(2) }}
              |)
            template(#badge)
              MBadge(v-if="iap.isPending" variant="warning") Pending
              MBadge(v-else-if="iap.isDuplicate") Duplicate
              MBadge(v-else :variant="validationStatusInfo(iap).badgeVariant") {{ validationStatusInfo(iap).text }}
            template(#top-right)
              meta-time(:date="iap.purchaseTime" showAs="date")
            template(#bottom-left) {{ iap.transactionId }}
            template(#bottom-right) {{ iap.platform }}

        //- Collapse content
        MList(showBorder striped)
          MListItem(condensed) Platform
            template(#top-right) {{ iap.platform }}
          MListItem(condensed) Transaction ID
            template(#top-right)
              span.text-muted {{ iap.transactionId }}
          MListItem(condensed) Order ID
            template(#top-right)
              span(v-if="iap.orderId === null").text-muted None
              span(v-else) {{ iap.orderId }}
          MListItem(condensed) Product ID
            template(#top-right) {{ iap.productId }}
          MListItem(condensed) Platform Product ID
            template(#top-right) {{ iap.platformProductId }}
          MListItem(condensed) Reference Price
            template(#top-right) ${{ iap.referencePrice.toFixed(2) }}
          MListItem(condensed) Related Offer
            template(#top-right)
              span(v-if="!iap.dynamicContent").text-muted None
              span(v-else-if="tryGetDynamicContentString(iap)") {{ tryGetDynamicContentString(iap) }}
              span(v-else) Unknown {{ iap.dynamicContent.$type }}
          MListItem(condensed) Content
            template(#top-right)
              span(v-if="!iap.resolvedContent && !iap.resolvedDynamicContent").text-muted None
              //div(v-else)
                //pre {{ iap.resolvedDynamicContent }}
                pre {{ iap.resolvedContent }}
              span(v-else)
                //- Show both resolvedContent (based on normal IAP contents) and resolvedDynamicContent (based on dynamicContent)
                span(v-if="iap.resolvedContent")
                  //- resolvedContent is either ResolvedPurchaseMetaRewards, or a game-specific subclass
                  offer-span(:offer="iap.resolvedContent")
                span(v-if="iap.resolvedDynamicContent")
                  //- resolvedDynamicContent is known to be of type ResolvedPurchaseMetaRewards
                  meta-reward-badge(v-for="(reward, key) in iap.resolvedDynamicContent.rewards" :reward="reward" :key="key")
          MListItem(condensed) Purchase Time
            template(#top-right) #[meta-time(:date="iap.purchaseTime" showAs="datetime")]
          MListItem(condensed) Claim Time
            template(#top-right) #[meta-time(:date="iap.claimTime" showAs="datetime")]
          MListItem(condensed) Status
            template(#top-right)
              div.text-right.text-break-word(:class="validationStatusInfo(iap).statusClass") {{ validationStatusInfo(iap).text }}
          MListItem(v-if="iap.platform == 'Google' && !iap.isPending && iap.status != 'Refunded'" condensed)
            div(v-if="!coreStore.hello.enableGooglePlayInAppPurchaseRefunds")
              span(v-if="!iap.isPending") Google Play refunds not configured.
              span(v-else) You can only refund valid Google Play purchases.
            template(#top-right)
              MActionModalButton(
                v-if="coreStore.hello.enableGooglePlayInAppPurchaseRefunds"
                modal-title="Refund In-App-Purchase"
                :action="refund"
                trigger-button-label="Google Play Refund"
                trigger-button-size="small"
                variant="danger"
                ok-button-label="Refund"
                @show="iapToRefund = iap"
                permission="api.players.iap_refund"
                )
                p You can request a refund for the Google Play purchase #[MBadge {{ iapToRefund.transactionId }}]. The refund result will be visible in the Google Play Publisher console.
                span(class="tw-text-neutral-500 tw-text-xs+") Note: You cannot see refund status here because the Google Play API does not return the result to us. Requesting a refund multiple times from here will not issue the refund more than once.
                meta-no-seatbelts(class="tw-mt-4")
</template>

<script lang="ts" setup>
import { computed, ref, watch } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { MetaListFilterOption, MetaListFilterSet, MetaListSortDirection, MetaListSortOption, showSuccessToast } from '@metaplay/meta-ui'
import { MTooltip, MBadge, MList, MListItem, MCollapse, MActionModalButton } from '@metaplay/meta-ui-next'

import { useSubscription } from '@metaplay/subscriptions'
import { getSinglePlayerSubscriptionOptions } from '../../subscription_options/players'

import OfferSpan from '../offers/OfferSpan.vue'
import { useCoreStore } from '../../coreStore'

const props = defineProps<{
  /**
   * Id of the player whose purchase history we want to show.
   */
  playerId: string
}>()

const gameServerApi = useGameServerApi()
const coreStore = useCoreStore()
const {
  data: playerData,
  refresh: playerRefresh,
} = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))

const displayedHistory = ref<any[] | undefined>()
const iapToRefund = ref<any>({})
const searchFields = [
  'platform',
  'productId',
  'transactionId',
  'orderId'
]
const sortOptions = [
  new MetaListSortOption('Purchase time', 'purchaseTime', MetaListSortDirection.Ascending),
  new MetaListSortOption('Purchase time', 'purchaseTime', MetaListSortDirection.Descending),
  new MetaListSortOption('Price', 'referencePrice', MetaListSortDirection.Ascending),
  new MetaListSortOption('Price', 'referencePrice', MetaListSortDirection.Descending),
]
const defaultSortOption = ref(1)

const filterSets = computed(() => {
  return [
    MetaListFilterSet.asDynamicFilterSet(displayedHistory.value, 'product-id', (x: any) => x.productId),
    new MetaListFilterSet('status', [
      new MetaListFilterOption('Valid receipt', (x: any) => x.status === 'ValidReceipt'),
      new MetaListFilterOption('Invalid receipt', (x: any) => x.status === 'InvalidReceipt'),
      new MetaListFilterOption('Pending validation', (x: any) => x.status === 'PendingValidation'),
      new MetaListFilterOption('Refunded', (x: any) => x.status === 'Refunded'),
      new MetaListFilterOption('Receipt already used', (x: any) => x.status === 'ReceiptAlreadyUsed'),
      new MetaListFilterOption('Other', (x: any) => !['ValidReceipt', 'PendingValidation', 'Refunded', 'ReceiptAlreadyUsed', 'InvalidReceipt'].includes(x.status))
    ])
  ]
})

watch(playerData, updateDisplayedHistory, { immediate: true })

function updateDisplayedHistory () {
  displayedHistory.value = []
  if (playerData.value) {
    // Add past purchases
    for (const purchase of playerData.value.model.inAppPurchaseHistory) {
      purchase.isPending = false
      displayedHistory.value.unshift(purchase)
    }
    // Add pending purchases
    for (const key in playerData.value.model.pendingInAppPurchases) {
      // TODO: figure out a better way to do this!
      // eslint-disable-next-line vue/no-mutating-props
      playerData.value.model.pendingInAppPurchases[key].isPending = true
      displayedHistory.value.unshift(playerData.value.model.pendingInAppPurchases[key])
    }
    // Add duplicate purchases
    for (const purchase of playerData.value.model.duplicateInAppPurchaseHistory) {
      purchase.isDuplicate = true
      displayedHistory.value.push(purchase)
    }
    // Add failed purchases
    for (const key in playerData.value.model.failedInAppPurchaseHistory) {
      displayedHistory.value.push(playerData.value.model.failedInAppPurchaseHistory[key])
    }
  }
}

async function refund () {
  await gameServerApi.post(`/players/${playerData.value.id}/refund/${iapToRefund.value.transactionId}`)
  const message = 'Refund requested successfully.'
  showSuccessToast(message)
  playerRefresh()
}

function resetModal () {
  iapToRefund.value = {}
}

function validationStatusInfo (iap: any) {
  const statuses: {[key: string]: any} = {
    PendingValidation: { text: 'Pending validation', statusClass: 'text-warning', badgeVariant: 'neutral' },
    ValidReceipt: { text: 'Validated', statusClass: 'text-success', badgeVariant: 'success' },
    InvalidReceipt: { text: 'Invalid Receipt', statusClass: 'text-danger', badgeVariant: 'danger' },
    ReceiptAlreadyUsed: { text: 'Receipt already used', statusClass: 'text-warning', badgeVariant: 'neutral' },
    Refunded: { text: 'Refunded', statusClass: 'text-warning', badgeVariant: 'neutral' }
  }
  if (iap.status in statuses) {
    return statuses[iap.status]
  } else {
    return { text: iap.status, statusClass: 'text-warning', badgeVariant: 'neutral' }
  }
}

function tryGetDynamicContentString (iap: any) {
  const contents = coreStore.gameSpecific.iapContents.find(content => content.$type === iap.dynamicContent.$type)
  if (contents) {
    return contents.getDisplayContent(iap.dynamicContent).join(', ')
  } else {
    return null
  }
}
</script>
