<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div.w-100.text-center.pt-5(v-if="!schema || !gameData || !staticConfigData")
  b-spinner.mt-5(label="Loading...")/
div(v-else)
  div(v-if="schema.isLocalized && !forcedLocalization")
    div.mb-1.font-weight-bold Selected Locales
    MInputMultiSelectCheckbox.mb-2(
      :model-value="selectedLocales"
      @update:model-value="updateLocalizations"
      :options="localeOptions"
      data-testid="localizations-selection-input"
      :variant="selectedLocales.length === 0 ? 'danger' : 'default'"
      )

    div.mb-1.font-weight-bold Current Locale
    meta-input-select(
      :value="currentLocale"
      @input="currentLocale = $event"
      :options="selectedLocaleOptions"
      data-testid="currentlocale-input"
      seachable
      no-clear
      ).mb-2

  generated-ui-form-dynamic-component(
    :fieldInfo="rootField"
    :value="value"
    @input="update"
    :editLocales="selectedLocales"
    :previewLocale="currentLocale === 'all' ? undefined : currentLocale"
    :fieldPath="''"
    :serverValidationResults="validationResults"
    :page="page"
    :abstractTypeFilter="abstractTypeFilter"
  )
  div(class="tw-text-red-500" v-if="!isServerValid") {{ serverValidationError }}
</template>

<script lang="ts" setup>
import { computed, onMounted, ref, watch, type PropType } from 'vue'

import { getGameDataSubscriptionOptions, getStaticConfigSubscriptionOptions } from '../../../subscription_options/general'
import { useGameServerApi } from '@metaplay/game-server-api'
import { useSubscription } from '@metaplay/subscriptions'

import GeneratedUiFormDynamicComponent from '../fields/forms/GeneratedUiFormDynamicComponent.vue'
import type { IGeneratedUiFieldInfo, IGeneratedUiFieldTypeSchema, IGeneratedUiFormAbtractTypeFilter } from '../generatedUiTypes'
import { EGeneratedUiTypeKind } from '../generatedUiTypes'
import { PreloadAllSchemasForTypeName } from '../getGeneratedUiTypeSchema'
import { findLanguages, stripNonMetaFields } from '../generatedUiUtils'
import { MInputMultiSelectCheckbox, MInputCheckbox } from '@metaplay/meta-ui-next'
import type { MetaInputSelectOption } from '@metaplay/meta-ui'

const props = defineProps({
  /**
   * The namespace qualified type name of the C# type we are about to visualize.
   * @example 'Metaplay.Core.InGameMail.MetaInGameMail'
   */
  typeName: {
    type: String,
    required: true
  },
  /**
   * The value of the current form data of the object.
   */
  value: {
    type: null,
    required: false,
    default: undefined
  },
  /**
   * This can be used to tell the form to add a $type specifier to the output object.
   * @example 'BroadcastForm'
   */
  page: {
    type: String,
    default: undefined
  },
  /**
   * This can be used to tell the form to add a $type specifier to the output object.
   */
  addTypeSpecifier: {
    type: Boolean,
    default: false
  },
  /**
   * Can be used to force a form to only show a single localization option, eg. the player's locale.
   * @example 'en'
   */
  forcedLocalization: {
    type: String,
    default: undefined
  },
  /**
   * A custom type filter for abtract types. Can be used to filter derived types inside a type dropdown.
   * @example see the Metaplay documentation for an example
   */
  abstractTypeFilter: {
    type: Function as PropType<IGeneratedUiFormAbtractTypeFilter>,
    default: undefined
  }
})

const schema = ref<IGeneratedUiFieldTypeSchema>()
const validationResults = ref<any>()
const validationTimeout = ref<any>(null)

const showAllLocales = ref<boolean>()

const {
  data: gameData,
} = useSubscription(getGameDataSubscriptionOptions())
const {
  data: staticConfigData,
} = useSubscription(getStaticConfigSubscriptionOptions())
const gameServerApi = useGameServerApi()

const emit = defineEmits<{(e: 'input', value: any): void, (e: 'status', value: boolean): void }>()

async function update (newValue: any) {
  if (schema.value) {
    if (props.addTypeSpecifier) {
      newValue.$type = schema.value.jsonType
    }
    const stripped = await stripNonMetaFields(newValue, schema.value)
    emit('input', stripped)
  }
}

// TODO: This changed in Vue 3 migration -> check if it still works as expected
const rootField = computed<IGeneratedUiFieldInfo>(() => ({
  fieldName: undefined,
  fieldType: props.typeName,
  typeKind: schema.value?.typeKind ?? EGeneratedUiTypeKind.Class,
  isLocalized: schema.value?.isLocalized,
}))

// -- Localization ---

const currentLocale = ref('')
const selectedLocales = ref<string[]>([])

function updateLocalizations (newValues: string[]) {
  selectedLocales.value = newValues
  if (!newValues.includes(currentLocale.value)) {
    currentLocale.value = newValues[0]
  }
}

function initialSelectedLocalizations () {
  let selectedLocales: string[] = []

  if (!gameData.value || !staticConfigData.value) {
    return selectedLocales
  }

  const defaultLang = staticConfigData.value.defaultLanguage

  // always add default language
  if (defaultLang && !selectedLocales.includes(defaultLang)) {
    selectedLocales.push(defaultLang)
  }

  const oldLocales = findLanguages(props.value, gameData.value)
  for (const lang of oldLocales) {
    if (!selectedLocales.includes(lang)) {
      selectedLocales.push(lang)
    }
  }

  if (oldLocales.length === 0) {
    for (const lang in gameData.value.gameConfig.Languages) {
      if (!selectedLocales.includes(lang)) {
        selectedLocales.push(lang)
      }
    }
  }

  if (props.forcedLocalization) {
    selectedLocales = [props.forcedLocalization]
  }

  currentLocale.value = props.forcedLocalization ?? staticConfigData.value?.defaultLanguage

  return selectedLocales
}

const allLanguages = computed(() => {
  return gameData.value?.gameConfig.Languages ?? {}
})

const localeOptions = computed(() => {
  const a = Object.values(allLanguages.value).sort((a: any, b: any) => {
    if (a.languageId === staticConfigData.value?.defaultLanguage) {
      return -1
    } else if (b.languageId === staticConfigData.value?.defaultLanguage) {
      return 1
    } else {
      return 0
    }
  }).filter((lang: any) => !props.forcedLocalization || lang.languageId === props.forcedLocalization)
    .map((lang: any) => {
      return {
        label: lang.displayName,
        value: lang.languageId,
        disabled: lang.languageId === staticConfigData.value?.defaultLanguage || !!props.forcedLocalization
      }
    })
  return a
})

const selectedLocaleOptions = computed((): Array<MetaInputSelectOption<string>> => {
  const options = selectedLocales.value.map(lang => {
    return {
      id: gameData.value.gameConfig.Languages[lang].displayName,
      value: lang
    }
  })

  options.unshift({
    id: 'Show all',
    value: 'all'
  })

  return options
})

// When either gameData or config changes, find localizations again.
watch([gameData, staticConfigData], (newValue) => {
  selectedLocales.value = initialSelectedLocalizations()
}, {
  deep: false
})

// --- Validation shenanigans ---

const isServerValid = computed(() => {
  return validationResults.value ? validationResults.value.length === 0 : undefined
})

const serverValidationError = computed(() => {
  if (validationResults.value?.length > 0) {
    return validationResults.value[0].path + ': ' + validationResults.value[0].reason
  } else {
    return ''
  }
})

async function validate (value: any) {
  // \todo #mail-refactoring: listen to fields status event to support client side validation
  // server side validate.
  const response = await gameServerApi.post(`forms/schema/${props.typeName}/validate`, value)
  if (response.status === 200) {
    validationResults.value = response.data
    emit('status', validationResults.value.length === 0)
  } else {
    // eslint-disable-next-line
    console.error('Server validation failed: ', response)
    emit('status', false)
  }
}

watch(() => props.value, (newValue, oldValue) => {
  if (validationTimeout.value !== null) {
    clearTimeout(validationTimeout.value)
  }
  validationTimeout.value = setTimeout(() => {
    validate(newValue).then(() => {
      validationTimeout.value = null
    }).catch((err: any) => {
      console.error(err)
    })
  }, 200)
})

// --- Lifecycle hooks ---

onMounted(() => {
  if (props.typeName !== '') {
    PreloadAllSchemasForTypeName(props.typeName)
      .then((loadedSchema: IGeneratedUiFieldTypeSchema) => {
        schema.value = loadedSchema
      })
      .catch((err: any) => {
        console.error(err)
      })
    selectedLocales.value = initialSelectedLocalizations()
  }
})

</script>
