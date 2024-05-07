<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div
  div.mb-1.font-weight-bold {{ displayName }}
    MTooltip.ml-2(v-if="displayHint" :content="displayHint" noUnderline): MBadge(shape="pill") ?
  div
    meta-input-select(
      v-if="fieldSchema.configLibrary && fieldSchema.configLibrary.length > 0"
      :value="stringValue"
      @input="updateValue"
      :options="possibleValues"
      :data-testid="dataTestid + '-input'"
      :class="isValid ? 'border-success' : ''"
      no-clear
      no-deselect
      )
    MInputText(
      v-else
      :model-value="stringValue"
      @input="update"
      :placeholder="formInputPlaceholder"
      :variant="isValid !== undefined ? isValid ? 'success' : 'danger' : 'default'"
      :data-testid="dataTestid + '-input'"
      )
  div(class="tw-text-red-500" v-if="!isValid") {{ validationError }}
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import { getGameDataSubscriptionOptions } from '../../../../subscription_options/general'
import { MTooltip, MBadge, MInputText } from '@metaplay/meta-ui-next'
import { useSubscription } from '@metaplay/subscriptions'

import { generatedUiFieldFormEmits, generatedUiFieldFormProps, useGeneratedUiFieldForm } from '../../generatedFieldBase'
import { useCoreStore } from '../../../../coreStore'

const props = defineProps({
  ...generatedUiFieldFormProps,
  value: {
    type: null,
    required: true,
    default: undefined
  },
})

const emit = defineEmits(generatedUiFieldFormEmits)

const {
  displayName,
  displayHint,
  isValid,
  validationError,
  update,
  dataTestid,
  formInputPlaceholder,
  useDefault
} = useGeneratedUiFieldForm(props, emit)

const stringValue = computed<string>(() => String(props.value ?? possibleValues.value.find(() => true)?.value))

const {
  data: gameData,
} = useSubscription(getGameDataSubscriptionOptions())
const coreStore = useCoreStore()

function updateValue (value: any) {
  update(value)
}

const possibleValues = computed(() => {
  // TODO: Improve the prop typings so we don't need to use non-null assertions.
  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
  const libraryKey = props.fieldSchema.configLibrary!
  if (gameData.value.gameConfig[libraryKey]) {
    return Object.keys(gameData.value.gameConfig[libraryKey]).map((key) => {
      // Look up if there is a prettier display name for this string id.
      const id = coreStore.stringIdDecorators[props.fieldInfo.fieldType] ? coreStore.stringIdDecorators[props.fieldInfo.fieldType](key) : key
      return {
        id,
        value: key,
      }
    })
  } else {
    return []
  }
})

useDefault(undefined, stringValue) // Use first value if available, or undefined
</script>
