<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MActionModalButton(
  modal-title="Import Live Ops Event"
  :action="importLiveOpsEvent"
  trigger-button-label="Import"
  :ok-button-label="importEventCount > 1 ? 'Import Events' : 'Import Event'"
  triggerButtonDisabled="Import feature is currently under construction."
  data-testid="new-event-form"
  )
  template(#default)
    div(class="tw-font-semibold tw-text-md") Upload Live Ops Event Data
    div(class="tw-mb-2 tw-text-neutral-500 tw-text-xs+") description about process below
    MInputSingleFileContents(
      label="File Upload"
      class="tw-mb-3"
      )
    MInputSegmentedSwitch(
      :model-value="overwritePolicy"
      @update:model-value="updateOverwritePolicy"
      :options="overwritePolicyOptions"
      label="Overwrite policy"
      )

  template(#right-panel)
    div(class="tw-font-semibold tw-text-md tw-mb-3") Preview Incoming Data
    MList(showBorder striped)
      MListItem Event Name
        template(#bottom-left) description
        template(#top-right)
          MBadge(variant="success") Import Successful
      MListItem Event Name
        template(#bottom-left) description
        template(#top-right)
          MBadge(variant="success") Import Successful
      MListItem Event Name
        template(#bottom-left) description
        template(#top-right)
          MBadge(variant="success") Import Successful
      MListItem Event Name
        template(#bottom-left) description
        template(#top-right)
          MBadge(variant="success") Import Successful

</template>

<script lang="ts" setup>

import { ref, computed } from 'vue'

import { MActionModalButton, MInputSegmentedSwitch, MInputSingleFileContents, MList, MListItem, MBadge } from '@metaplay/meta-ui-next'

import { DateTime } from 'luxon'

const overwritePolicy = ref<'ignore' | 'overwrite' | 'createnew'>('ignore')

function updateOverwritePolicy (value: 'ignore' | 'overwrite' | 'createnew') {
  // overwritePolicy.value = value
  // validateArchive(entityArchiveFile.value ?? entityArchiveText.value).catch(error => {
  //   console.error(error)
  // })
}

const overwritePolicyOptions: Array<{ value: 'ignore' | 'overwrite' | 'createnew', label: string }> = [
  { value: 'ignore', label: 'Ignore' },
  { value: 'overwrite', label: 'Overwrite' },
  { value: 'createnew', label: 'Create New' }
]

async function importLiveOpsEvent () {
}

const importEventCount = 4

</script>
