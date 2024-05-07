<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div
  div.mb-1.font-weight-bold {{ displayName }}
    MTooltip.ml-2(v-if="displayHint" :content="displayHint" noUnderline): MBadge(shape="pill") ?
  MInputTextArea(
    v-if="fieldInfo.fieldTypeHint && fieldInfo.fieldTypeHint.type === 'textArea'"
    :model-value="value"
    @update:model-value="update"
    :placeholder="formInputPlaceholder"
    :variant="isValid !== undefined ? isValid ? 'success' : 'danger' : 'default'"
    :data-testid="dataTestid + '-input'"
    :hint-message="!isValid ? validationError : undefined"
    )
  MInputText(
    v-else
    :model-value="value"
    @update:model-value="update"
    :placeholder="formInputPlaceholder"
    :variant="isValid !== undefined ? isValid ? 'success' : 'danger' : 'default'"
    :data-testid="dataTestid + '-input'"
    :hint-message="!isValid ? validationError : undefined"
    )
</template>

<script lang="ts" setup>
import { generatedUiFieldFormEmits, generatedUiFieldFormProps, useGeneratedUiFieldForm } from '../../generatedFieldBase'
import { computed } from 'vue'
import { MTooltip, MBadge, MInputText, MInputTextArea } from '@metaplay/meta-ui-next'

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
  formInputPlaceholder,
  isValid: serverIsValid,
  getServerValidationError,
  update,
  dataTestid
} = useGeneratedUiFieldForm(props, emit)

const notEmptyRule = computed(() => {
  return props.fieldInfo.validationRules ? props.fieldInfo.validationRules.find((rule: any) => rule.type === 'notEmpty') : null
})

const isValid = computed(() => {
  if (notEmptyRule.value && props.value.length === 0) {
    return false
  } else {
    return serverIsValid.value
  }
})

const validationError = computed((): string | undefined => {
  if (notEmptyRule.value && props.value.length === 0) {
    return notEmptyRule.value.props.message
  } else {
    return (getServerValidationError as any)()
  }
})
</script>
