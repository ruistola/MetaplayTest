<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div
  div.mb-1.font-weight-bold {{ displayName }}
    MTooltip.ml-2(v-if="displayHint" :content="displayHint" noUnderline): MBadge(shape="pill") ?
  //- TODO: Consider implementing a number range input component.
  MInputNumber(
    v-if="fieldInfo.fieldTypeHint && fieldInfo.fieldTypeHint.type === 'range'"
    :model-value="value"
    @update:model-value="update(String($event))"
    :min="fieldInfo.fieldTypeHint.props.min"
    :max="fieldInfo.fieldTypeHint.props.max"
    :placeholder="formInputPlaceholder"
    :variant="isValid !== undefined ? isValid ? 'success' : 'danger' : 'default'"
    :data-testid="dataTestid + '-input'"
    :hint-message="validationError ? validationError : undefined"
    )
  MInputNumber(
    v-else
    :model-value="value"
    @update:model-value="update(String($event))"
    :placeholder="formInputPlaceholder"
    :variant="isValid !== undefined ? isValid ? 'success' : 'danger' : 'default'"
    :data-testid="dataTestid + '-input'"
    :hint-message="validationError ? validationError : undefined"
    )
</template>

<script lang="ts" setup>
import { MTooltip, MBadge, MInputText, MInputNumber } from '@metaplay/meta-ui-next'
import { generatedUiFieldFormEmits, generatedUiFieldFormProps, useGeneratedUiFieldForm } from '../../generatedFieldBase'

const props = defineProps({
  ...generatedUiFieldFormProps,
  value: {
    type: Number,
    default: 0
  },
})

const emit = defineEmits(generatedUiFieldFormEmits)

const {
  displayName,
  displayHint,
  formInputPlaceholder,
  isValid,
  validationError,
  update: emitUpdate,
  dataTestid
} = useGeneratedUiFieldForm(props, emit)

const update = (newValue: string) => {
  emitUpdate(Number(newValue))
}
</script>
