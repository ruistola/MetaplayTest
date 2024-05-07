<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
//- TODO: This is a bit old code now and doesn't have a discreet loading state. Clean up when refactoring to MetaUiNext.
div(data-testid="system-redirect-card" v-if="backendStatusData")
  b-card
    b-card-title
      span.mt-2 📱 Client Compatibility Settings
    p Prevent incompatible clients from connecting and redirects clients compatible with a newer logic version to another backend deployment.

    div(class="tw-font-semibold")
      p Active Logic Version Range: {{ backendStatusData.clientCompatibilitySettings.activeLogicVersionRange.minVersion }} - {{ backendStatusData.clientCompatibilitySettings.activeLogicVersionRange.maxVersion }}
      p New Version Redirect:
        span(class="tw-ml-2")
          MBadge(clavariant="success" v-if="backendStatusData.clientCompatibilitySettings.redirectEnabled") On
          MBadge(v-else) Off

      p(class="tw-text-xs+ tw-text-neutral-500") Clients using logic version {{ backendStatusData.clientCompatibilitySettings.activeLogicVersionRange.minVersion - 1 }} or older
        span.pl-1(v-if="backendStatusData.clientCompatibilitySettings.redirectEnabled") will not be able to connect and clients using logic version {{ backendStatusData.clientCompatibilitySettings.activeLogicVersionRange.maxVersion + 1 }} or newer will be redirected.
        span.pl-1(v-else) and clients using logic version {{ backendStatusData.clientCompatibilitySettings.activeLogicVersionRange.maxVersion + 1 }} or newer will not be able to connect to this deployment.

    div(v-if="backendStatusData.clientCompatibilitySettings.redirectEnabled")
      span(class="tw-text-xs+") Redirect Settings
      b-table-simple(small responsive)
        b-tbody
          b-tr
            b-td Host
            b-td {{ backendStatusData.clientCompatibilitySettings.redirectServerEndpoint.serverHost }}
          b-tr
            b-td Port
            b-td {{ backendStatusData.clientCompatibilitySettings.redirectServerEndpoint.serverPort }}
          b-tr
            b-td TLS enabled
            b-td {{ backendStatusData.clientCompatibilitySettings.redirectServerEndpoint.enableTls }}
          b-tr
            b-td CDN URL
            b-td {{ backendStatusData.clientCompatibilitySettings.redirectServerEndpoint.cdnBaseUrl }}

    div(class="tw-flex tw-justify-end")
      MActionModalButton(
        modal-title="Update Client Compatibility Settings"
        :action="setClientCompatibilitySettings"
        trigger-button-label="Edit Settings"
        :trigger-button-disabled="!staticConfig?.supportedLogicVersions"
        ok-button-label="Save Settings"
        :ok-button-disabled="!isLogicVersionFormValid"
        permission="api.system.edit_logicversioning"
        data-testid="client-settings"
        @show="resetModal"
        )
        template(#default)
          MInputSingleSelectRadio(
            label="Active LogicVersion Min"
            :model-value="activeLogicVersionMin"
            @update:model-value="activeLogicVersionMin = $event"
            :options="minLogicVersionOptions"
            hint-message="Only clients with this version or newer will be allowed to connect."
            class="tw-mb-6"
            size="small"
            )
          MInputSingleSelectRadio(
            label="Active LogicVersion Max"
            :model-value="activeLogicVersionMax"
            @update:model-value="activeLogicVersionMax = $event"
            :options="maxLogicVersionOptions"
            hint-message="Only clients with this version or older will be allowed to connect."
            class="tw-mb-6"
            size="small"
            )

          b-alert(:show="isInvalidLogicVersionRange" variant="danger")
            h6(style="color: inherit") ⚠️ Invalid LogicVersion range
            p(class="tw-m-0") The active #[MBadge LogicVersion] range is invalid. Make sure the #[MBadge Min] version is lower than or equal to the #[MBadge Max] version.

          b-alert(:show="!isInvalidLogicVersionRange && (activeLogicVersionMax && activeLogicVersionMax < backendStatusData.clientCompatibilitySettings.activeLogicVersionRange.maxVersion) || (activeLogicVersionMin && activeLogicVersionMin < backendStatusData.clientCompatibilitySettings.activeLogicVersionRange.minVersion)" variant="warning")
            h6(style="color: inherit") ⚠️ Tread carefully, brave knight
            p.m-0 Rolling back the active #[MBadge LogicVersion] can have very bad unintended consequences and should ideally never be done. Please make sure you know what you are doing before saving this action!

          div(class="tw-flex tw-justify-between")
            span(class="tw-font-semibold") New Client Redirect Enabled
            MInputSwitch(
              :model-value="redirectEnabled"
              @update:model-value="redirectEnabled = $event"
              class="tw-relative tw-top-1 tw-mr-1"
              name="newClientRedirectEnabled"
              size="sm"
              data-testid="input-switch-redirect-enabled"
              )

          MInputText(
            label="Host"
            :model-value="redirectHost"
            @update:model-value="redirectHost = $event"
            :disabled="!redirectEnabled"
            :variant="redirectEnabled && !!redirectHost ? 'success' : 'default'"
            class="tw-mb-3"
            data-testid="input-text-host"
            )

          MInputText(
            label="Port"
            :model-value="redirectPort"
            @update:model-value="redirectPort = $event"
            :disabled="!redirectEnabled"
            :variant="redirectEnabled && !!redirectPort ? 'success' : 'default'"
            class="tw-mb-3"
            data-testid="input-text-port"
            )

          MInputText(
            label="CDN URL"
            :model-value="redirectCdnUrl"
            @update:model-value="redirectCdnUrl = $event"
            :disabled="!redirectEnabled"
            :variant="redirectEnabled && !!redirectCdnUrl ? 'success' : 'default'"
            class="tw-mb-3"
            data-testid="input-text-cdn-url"
            )

          div(class="tw-flex tw-justify-between tw-mb-3")
            span TLS Enabled
            MInputSwitch(
              :model-value="redirectTls"
              @update:model-value="redirectTls = $event"
              :disabled="!redirectEnabled"
              class="tw-relative tw-top-1 tw-mr-1"
              name="redirectTls"
              size="sm"
              )
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton, MBadge, MInputSingleSelectRadio, MInputSwitch, MInputText } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { getBackendStatusSubscriptionOptions, getStaticConfigSubscriptionOptions } from '../../subscription_options/general'

const gameServerApi = useGameServerApi()

const {
  data: backendStatusData,
  refresh: backendStatusTriggerRefresh,
  error: backendStatusError
} = useSubscription(getBackendStatusSubscriptionOptions())
const {
  data: staticConfig,
} = useSubscription(getStaticConfigSubscriptionOptions())

const activeLogicVersionMin = ref<number>()
const activeLogicVersionMax = ref<number>()
const redirectHost = ref<any>(null)
const redirectPort = ref<any>(null)
const redirectTls = ref(false)
const redirectCdnUrl = ref<any>(null)
const redirectEnabled = ref(false)

const minLogicVersionOptions = computed((): Array<{ label: string, value: number }> => {
  const versions = []
  for (let i = staticConfig.value.supportedLogicVersions.minVersion; i <= staticConfig.value.supportedLogicVersions.maxVersion; i++) {
    versions.push({
      label: i === backendStatusData.value.clientCompatibilitySettings.activeLogicVersionRange.minVersion ? `${i} (current)` : i,
      value: i
    })
  }
  return versions
})

const maxLogicVersionOptions = computed((): Array<{ label: string, value: number }> => {
  const versions = []
  for (let i = staticConfig.value.supportedLogicVersions.minVersion; i <= staticConfig.value.supportedLogicVersions.maxVersion; i++) {
    versions.push({
      label: i === backendStatusData.value.clientCompatibilitySettings.activeLogicVersionRange.maxVersion ? `${i} (current)` : i,
      value: i,

    })
  }
  return versions
})

const isInvalidLogicVersionRange = computed(() => {
  return activeLogicVersionMin.value && activeLogicVersionMax.value && activeLogicVersionMin.value > activeLogicVersionMax.value
})

const isLogicVersionFormValid = computed(() => {
  return (!redirectEnabled.value || (!!redirectHost.value && !!redirectPort.value && !!redirectCdnUrl.value)) &&
    !!activeLogicVersionMin.value &&
    !!activeLogicVersionMax.value &&
    !isInvalidLogicVersionRange.value
})

async function setClientCompatibilitySettings () {
  const payload = {
    activeLogicVersionRange: {
      minVersion: activeLogicVersionMin.value,
      maxVersion: activeLogicVersionMax.value,
    },
    redirectEnabled: redirectEnabled.value,
    redirectServerEndpoint: {
      serverHost: redirectHost.value,
      serverPort: redirectPort.value,
      enableTls: redirectTls.value,
      cdnBaseUrl: redirectCdnUrl.value,
    }
  }
  await gameServerApi.post('/clientCompatibilitySettings', payload)
  showSuccessToast('Client compatibility settings updated.')
  backendStatusTriggerRefresh()
}

function resetModal () {
  const settings = backendStatusData.value.clientCompatibilitySettings
  const redirectEndpoint = settings.redirectServerEndpoint
  activeLogicVersionMin.value = settings.activeLogicVersionRange.minVersion
  activeLogicVersionMax.value = settings.activeLogicVersionRange.maxVersion
  redirectEnabled.value = settings.redirectEnabled
  redirectHost.value = redirectEndpoint ? settings.redirectServerEndpoint.serverHost : ''
  redirectPort.value = redirectEndpoint ? settings.redirectServerEndpoint.serverPort : 9339
  redirectTls.value = redirectEndpoint ? settings.redirectServerEndpoint.enableTls : true
  redirectCdnUrl.value = redirectEndpoint ? settings.redirectServerEndpoint.cdnBaseUrl : ''
}
</script>
