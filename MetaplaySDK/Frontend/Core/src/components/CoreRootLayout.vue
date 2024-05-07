<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MRootLayout(
  :headerBadgeLabel="gameServerApiStore.auth.userDetails.name"
  :headerAvatarImageUrl="gameServerApiStore.auth.userDetails.picture"
  @headerAvatarClick="router.push('/user')"
  :headerBackgroundColorString="dashboardOptionsData?.dashboardHeaderColorInHex ?? undefined"
  )
  template(#sidebar-header)
    //- A masking element for the monogram image.
    div(class="tw-rounded-full")

      //- Monogram.
      img(
        :src="coreStore.gameSpecific.gameIconUrl"
        width="45"
        height="45"
        class="tw-rounded-lg tw-cursor-pointer tw-filter hover:tw-brightness-90 active:tw-brightness-75"
        role="button"
        @click="router.push('/')"
        data-testid="overview-link"
        )

    //- Project name.
    div
      div(
        role="heading"
        class="tw-font-semibold tw-text-lg"
        ) {{ coreStore.hello.projectName }}

      //- Quick links.
      MPopover(
        v-if="quickLinksDropdownEnabled"
        :triggerLabel="coreStore.hello.environment"
        title="Quick Links"
        data-testid="quick-link"
        size="small"
        noBodyPadding
        )
        //- Quick links content.
        MList
          div(
            v-for="(quickLink, index) in quickLinksData"
            :key="quickLink.uri"
            :tabindex="index"
            role="link"
            class="tw-py-3 tw-px-4 tw-space-x-2 tw-flex tw-items-center hover:tw-bg-neutral-200 active:tw-bg-neutral-300 tw-cursor-pointer -tw-outline-offset-2"
            @click="openQuickLink(quickLink.uri)"
            :data-testid="'quick-link-' + index"
            )
              img(
                v-if="quickLink.icon"
                :src="quickLink.icon === '@game-icon' ? coreStore.gameSpecific.gameIconUrl : quickLink.icon"
                class="tw-h-8 tw-w-8 tw-rounded-lg"
                )
              div(class="tw-grow tw-space-y-1.5 tw-flex tw-justify-between tw-items-baseline")
                span {{ quickLink.title }}
                fa-icon(icon="external-link-alt" size="sm" class="tw-ml-2 tw-text-neutral-500 tw-relative" style="bottom: -1px")

      //- Quick links disabled state.
      div(
        v-else
        class="tw-text-sm"
        ) {{ coreStore.hello.environment }}

  template(#sidebar="{ closeSidebarOnNarrowScreens }")
    //- Top.
    div(data-testid="sidebar")
      //- Sidebar links.
      div(v-for="category in sortedCategories" :key="category")
        MSidebarSection(:title="category")
          RouterLink(
            v-for="route in sidebarRoutes[category]"
            :to="gameServerApiStore.doesHavePermission(getRouteOptions(route).permission) ? route.path : ''"
            class="hover:tw-no-underline tw-block"
            )
            MSidebarLink(
              :label="getRouteOptions(route)?.sidebarTitle || 'Title TBD'"
              :permission="getRouteOptions(route)?.permission"
              :active-path-fragment="route.path"
              :secondaryPathHighlights="getRouteOptions(route)?.secondaryPathHighlights || []"
              @click="closeSidebarOnNarrowScreens"
              )
                template(#icon v-if="getRouteOptions(route)?.icon")
                  FontAwesomeIcon(:icon="getRouteOptions(route).icon ?? ''" fixed-width)

      //- List debug roles if needed.
      div(
        v-if="gameServerApiStore.auth.userAssumedRoles.length > 0"
        class="tw-px-4 tw-mb-3"
        )
        div(
          class="tw-font-bold tw-mb-2"
          role="heading"
          )
          meta-plural-label(:value="gameServerApiStore.auth.userAssumedRoles.length" label="Assumed Role" hide-count)
        MBadge(
          class="tw-mr-1"
          variant="warning"
          v-for="role in gameServerApiStore.auth.userAssumedRoles"
          :key="role"
          ) {{ role }}

      //- Logout.
      MTooltip(
        :content="!gameServerApiStore.auth.canLogout ? 'Cannot log out when authentication is disabled.' : undefined"
        no-underline
        )
        MSidebarLink(
          label="Log Out"
          icon="sign-out-alt"
          @click="logout"
          :disabled="!gameServerApiStore.auth.canLogout"
          )
            template(#icon)
              FontAwesomeIcon(:icon="'sign-out-alt'" fixed-width)

  //- Alerts.
  header-alerts

  //- router-view element will contain the currently active view as controlled by the Vue router.
  router-view(role="main" :key="route.fullPath")
</template>

<script lang="ts" setup>
import HeaderAlerts from './navigation/HeaderAlerts.vue'
import { MRootLayout, MSidebarSection, MSidebarLink, MPopover, MList, MBadge, MTooltip } from '@metaplay/meta-ui-next'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { logout, useGameServerApiStore } from '@metaplay/game-server-api'
import { useSubscription } from '@metaplay/subscriptions'
import { getDashboardOptionsSubscriptionOptions, getQuickLinksSubscriptionOptions } from '../subscription_options/general'

import { useRoute, useRouter } from 'vue-router'
import { useCoreStore } from '../coreStore'
import { computed } from 'vue'

import type { RouteRecordNormalized } from 'vue-router'
import type { NavigationEntryOptions } from '../integration_api/integrationApi'

const route = useRoute()
const router = useRouter()
const coreStore = useCoreStore()
const gameServerApiStore = useGameServerApiStore()

const { data: dashboardOptionsData } = useSubscription(getDashboardOptionsSubscriptionOptions())

const sidebarRoutes = computed(() => {
  const routes: { [key: string]: RouteRecordNormalized[] } = {}
  // Build an object of routes grouped by category
  for (const route of router.getRoutes()) {
    if (typeof route.meta.category === 'string') {
      if (routes[route.meta.category]) routes[route.meta.category].push(route)
      else routes[route.meta.category] = [route]
    }
  }
  // Sort the routes by sidebarOrder
  for (const category of Object.keys(routes)) {
    routes[category].sort((a, b) => {
      if (typeof a.meta.sidebarOrder === 'number' && typeof b.meta.sidebarOrder === 'number') return a.meta.sidebarOrder - b.meta.sidebarOrder
      if (a.meta?.sidebarOrder) return -1
      if (b.meta?.sidebarOrder) return 1
      return 0
    })
  }

  return routes
})

const sortedCategories = computed(() => {
  return Object.keys(sidebarRoutes.value).sort()
})

function getRouteOptions (route: RouteRecordNormalized) {
  return route.meta as NavigationEntryOptions
}

const {
  data: quickLinksData,
  hasPermission: quickLinksHasPermission,
} = useSubscription<Array<{ icon?: string, title: string, uri: string }>>(getQuickLinksSubscriptionOptions())

/**
 * Should the environment links dropdown by clickable or not?
 */
const quickLinksDropdownEnabled = computed(() => {
  if (quickLinksData.value) {
    return quickLinksHasPermission.value && quickLinksData.value.length > 0
  }
  return false
})

function openQuickLink (uri: string) {
  window.open(uri, '_blank')
}
</script>
