<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div
  div.mb-1.font-weight-bold {{ displayName }}
    MTooltip.ml-2(v-if="displayHint" :content="displayHint" noUnderline): MBadge(shape="pill") ?
  meta-input-select(
    :value="value"
    @input="update"
    :options="options"
    :state="isValid"
    :data-testid="dataTestid + '-input'"
    no-clear
    )
  div(class="tw-text-red-500" v-if="!isValid") {{ validationError }}
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import { MTooltip, MBadge } from '@metaplay/meta-ui-next'
import { generatedUiFieldFormEmits, generatedUiFieldFormProps, useGeneratedUiFieldForm } from '../../generatedFieldBase'
import type { MetaInputSelectOption } from '@metaplay/meta-ui'

const props = defineProps({
  ...generatedUiFieldFormProps,
  value: {
    type: String,
    default: ''
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
  useDefault
} = useGeneratedUiFieldForm(props, emit)

// TODO: Improve the prop typings so we don't need to use non-null assertions.
// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
useDefault('', props.fieldSchema.possibleValues![0])

const options = computed((): Array<MetaInputSelectOption<string>> => {
  if (!props.fieldSchema.possibleValues) {
    return []
  }

  return props.fieldSchema.possibleValues.map((value) => {
    return {
      value,
      id: value
    }
  })
})
</script>
