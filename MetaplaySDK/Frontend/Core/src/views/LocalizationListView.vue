<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
meta-page-container(
  :is-loading="!allLocalizationData"
  :meta-api-error="allLocalizationError"
  )
  template(#overview)
    meta-page-header-card
      template(#title) View Localizations
      p Localizations are a way to deliver localized text content to your players.
      div.small.text-muted You can upload new localization builds and review them before publishing. Published localizations will be delivered over-the-air and do not require players to update their clients.

      template(#buttons)
        MActionModalButton(
          modal-title="Build New Localizations"
          :action="buildLocalizations"
          trigger-button-label="New Build"
          :trigger-button-disabled="!staticConfigData?.localizationsBuildInfo.buildSupported ? 'Localization builds have not been enabled.' : undefined"
          ok-button-label="Build Localizations"
          :ok-button-disabled="!formValidationState"
          permission="api.game_config.edit"
          data-testid="new-config-form"
          @show="resetBuildNewConfigModal"
          )
          template(#default)
            p You can configure and trigger a new localizations build to happen directly on the game server. It may take a few minutes for large projects.
            MInputText(
              label="Localizations Build Name"
              :model-value="localizationsName"
              @update:model-value="localizationsName = $event"
              :variant="nameValidationState !== null ? nameValidationState ? 'success' : 'danger' : 'default'"
              placeholder="For example: 1.3.2"
              class="tw-mb-1"
              )

            MInputTextArea(
              label="Localizations Build Description"
              :model-value="localizationsDescription"
              @update:model-value="localizationsDescription = $event"
              :variant="descriptionValidationState !== null ? descriptionValidationState ? 'success' : 'danger' : 'default'"
              placeholder="For example: Reduced the difficulty of levels between 5 and 10."
              :rows="3"
              class="tw-mb-1"
              )

          template(#right-panel)
            //- Use a generated form for the rest of the build params.
            div(class="tw-border tw-border-neutral-200 tw-p-4 tw-rounded-md tw-bg-neutral-100")
              div(class="tw-font-semibold tw-mb-2") Build Parameters
              div.small Optional configuration for how the localizations should be built. You can, for example, pull data from a different sources.
              meta-generated-form(
                typeName='Metaplay.Core.Config.LocalizationsBuildParameters'
                :value="buildParams"
                @input="buildParams = $event"
                :page="'LocalizationsBuildCard'"
                @status="buildParamsValidationState = $event"
                :abstract-type-filter="buildParamsTypeFilter"
                class="tw-mt-2"
                )

  template(#default)
    core-ui-placement(placementId="Localizations/List")

    meta-raw-data(:kv-pair="allLocalizationData" name="allLocalizationData")
</template>

<script lang="ts" setup>
import { computed, ref } from 'vue'

import { useGameServerApi } from '@metaplay/game-server-api'
import { showSuccessToast } from '@metaplay/meta-ui'
import { MActionModalButton, MInputText, MInputTextArea } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import CoreUiPlacement from '../components/system/CoreUiPlacement.vue'
import MetaGeneratedForm from '../components/generatedui/components/MetaGeneratedForm.vue'

import type { MinimalLocalizationInfo } from '../localizationServerTypes'
import { getStaticConfigSubscriptionOptions } from '../subscription_options/general'
import type { StaticConfig } from '../subscription_options/generalTypes'
import { getAllLocalizationsSubscriptionOptions } from '../subscription_options/localization'
import type { IGeneratedUiFieldSchemaDerivedTypeInfo } from '../components/generatedui/generatedUiTypes'

const gameServerApi = useGameServerApi()

const {
  data: staticConfigData,
} = useSubscription<StaticConfig>(getStaticConfigSubscriptionOptions())

const {
  data: allLocalizationData,
  error: allLocalizationError,
  refresh: allLocalizationsTriggerRefresh
} = useSubscription<MinimalLocalizationInfo[]>(getAllLocalizationsSubscriptionOptions())

// Form data ----------------------------------------------------------------------------------------------------------

/**
 * Optional name for the new localizations.
 */
const localizationsName = ref<string>()

/**
 * Optional description for the new localization.
 */
const localizationsDescription = ref<string>()

const buildParams = ref<any>()

/**
 * Reset state of the new build modal.
 */
function resetBuildNewConfigModal () {
  buildParams.value = null
  localizationsName.value = ''
  localizationsDescription.value = ''
}

// Form validation ----------------------------------------------------------------------------------------------------

/**
 *  Validation check for the name input field.
 */
const nameValidationState = computed((): true | false | null => {
  if (localizationsName.value && localizationsName.value.length > 0) {
    return true
  }
  // Optional validation here (eg: check for length, invalid chars, etc.) could return false if invalid.
  return false
})

/**
 *  Validation check for the description input field.
 */
const descriptionValidationState = computed((): true | false | null => {
  if (localizationsDescription.value && localizationsDescription.value.length > 0) {
    return true
  }
  // Optional validation here (eg: check for length, invalid chars, etc.) could return false if invalid.
  return null
})

/**
 * Validation state of the generated form.
 */
const buildParamsValidationState = ref<boolean>()

/**
 * Overall validation state of the entire modal.
 */
const formValidationState = computed(() => {
  return nameValidationState.value === true &&
    descriptionValidationState.value !== false &&
    buildParamsValidationState.value
})

function buildParamsTypeFilter (abstractType: string) {
  if (abstractType === 'Metaplay.Core.Config.LocalizationsBuildParameters') {
    const hasCustomBuildParams = staticConfigData.value != null &&
    staticConfigData.value.localizationsBuildInfo.buildParametersNamespaceQualifiedName !== 'Metaplay.Core.Config.DefaultLocalizationsBuildParameters'
    if (hasCustomBuildParams) {
      return (concreteType: IGeneratedUiFieldSchemaDerivedTypeInfo) => concreteType.typeName !== 'Metaplay.Core.Config.DefaultLocalizationsBuildParameters'
    }
  }
  return () => true
}

// Sending build command to the game server ---------------------------------------------------------------------------

/**
 * Build localization from source data.
 */
async function buildLocalizations () {
  const params = {
    Properties: {
      Name: localizationsName.value,
      Description: localizationsDescription.value
    },
    BuildParams: buildParams.value
  }

  await gameServerApi.post('/localization/build', params)

  showSuccessToast('Localizations build started.')
  allLocalizationsTriggerRefresh()
}
</script>
