<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div(v-if="playerData")
  MActionModalButton(
    modal-title="GDPR Data Export"
    :action="gdprDownloadOk"
    trigger-button-label="GDPR Export"
    trigger-button-full-width
    ok-button-label="Download"
    :ok-button-disabled="!exportData"
    disable-safety-lock
    @show="fetchData()"
    permission="api.players.gdpr_export"
    data-testid="gdpr-export"
    )
      template(#action-button-icon)
        fa-icon(icon="file-download").mr-2/
      template(#default)
        h6 Personal Data of {{ playerData.model.playerName || 'n/a' }}
        p You can use this function to download personal data associated to player ID #[MBadge {{ playerData.id }}] stored in this deployment's database.

      template(#right-panel)
        meta-alert(
          variant="secondary"
          title="Other Data Sources"
          class="tw-text-sm"
          )
            p Your company might have other personal data associated with this ID in third party tools like analytics. You'll need to export those separately.
            p The player ID might also show up in short lived system logs like automatic error reports. Those logs do not contain any personal infomation in addition to this ID and will be automatically deleted according to your retention policies.

      template(#bottom-panel)
        h6(class="tw-mb-2") Export Preview
        pre(v-if="exportData" data-testid="export-payload" style="max-height: 10rem").border.rounded.bg-light.w-100.code-box {{ exportData }}

</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { useGameServerApi } from '@metaplay/game-server-api'
import { MActionModalButton, MBadge, MButton } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'
import { getSinglePlayerSubscriptionOptions } from '../../../subscription_options/players'

const props = defineProps<{
  playerId: string
}>()

const gameServerApi = useGameServerApi()
const { data: playerData } = useSubscription(getSinglePlayerSubscriptionOptions(props.playerId))
const downloadUrl = ref('')
const exportData = ref<string>()

async function fetchData () {
  exportData.value = (await gameServerApi.get(`/players/${playerData.value.id}/gdprExport`)).data
  downloadUrl.value = window.URL.createObjectURL(new Blob([JSON.stringify(exportData.value, null, 2)]))
}

async function gdprDownloadOk () {
  const a = document.createElement('a')
  a.href = downloadUrl.value
  a.download = `${playerData.value.id}.json`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
}

</script>
