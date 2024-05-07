<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
//- Container.
div(v-bind="api.rootProps")
  //- Label.
  label(
    v-if="label"
    :for="id"
    :class="['tw-block tw-text-sm tw-font-bold tw-leading-6 tw-mb-1', { 'tw-text-neutral-400': internalDisabled, 'tw-text-neutral-900': !internalDisabled }]"
    ) {{ label }}

  //- Input.
  div(
    v-bind="{ ...$attrs, ...api.dropzoneProps }"
    :class="['tw-w-full tw-rounded-md tw-shadow-sm tw-border-0 tw-py-1.5 tw-text-neutral-900 tw-ring-1 tw-ring-inset placeholder:tw-text-neutral-400 focus:tw-ring-2 focus:tw-ring-inset focus:tw-ring-blue-600 sm:tw-text-sm sm:tw-leading-6 disabled:tw-cursor-not-allowed disabled:tw-bg-neutral-50 disabled:tw-text-neutral-500 disabled:ring-neutral-200 tw-select-none tw-flex tw-justify-between', containerVariantClasses]"
    )
    input(v-bind="api.hiddenInputProps")
    div(class="tw-px-3")
      span(v-if="!api.files.length" :class="placeholderLabelVariantClasses") {{ placeholder ?? 'Drag your file here' }}
      span(v-else v-bind="api.getItemNameProps({ file: api.files[0] })" :class="{ 'tw-text-red-500': variant === 'danger' }") {{ api.files[0].name }}
    button(
      v-if="!api.files.length"
      v-bind="api.triggerProps"
      :disabled="disabled"
      class="tw-bg-neutral-200 hover:tw-bg-neutral-300 active:tw-bg-neutral-400 disabled:tw-bg-neutral-100 disabled:tw-text-neutral-500 tw-px-2 tw-rounded tw-mr-2"
      ) Browse
    button(
      v-else
      @click.stop="api.deleteFile(api.files[0])"
      class="tw-bg-neutral-200 hover:tw-bg-neutral-300 active:tw-bg-neutral-400 tw-px-2 tw-rounded tw-mr-2"
      ) Remove

  //- Hint message.
  div(
    :id="hintId"
    v-if="hintMessage"
    :class="['tw-text-xs tw-text-neutral-400 tw-mt-1', { 'tw-text-red-400': variant === 'danger' }]"
    ) {{ hintMessage }}
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { makeIntoUniqueKey } from '../utils/generalUtils'
import * as fileUpload from '@zag-js/file-upload'
import { normalizeProps, useMachine } from '@zag-js/vue'
import { useEnableAfterSsr } from '../composables/useEnableAfterSsr'

export type FileError = fileUpload.FileError

defineOptions({
  inheritAttrs: false
})

const props = withDefaults(defineProps<{
  /**
   * The value of the input. Can be undefined.
   */
  modelValue?: File
  /**
   * Optional: A function that validates the selected files. Should return an array of errors.
   * @param file The file to validate.
   */
  validationFunction?: (file: File) => fileUpload.FileError[] | null
  /**
   * Optional: Set a lower boundary for the file size in bytes. Defaults to undefined.
   */
  minFileSize?: number
  /**
   * Optional: Set an upper boundary for the file size in bytes. Defaults to undefined.
   */
  maxFileSize?: number
  /**
   * Optional: Show a label for the input.
   */
  label?: string
  /**
   * Optional: Disable the input. Defaults to false.
   */
  disabled?: boolean
  /**
   * Optional: Visual variant of the input. Defaults to 'default'.
   */
  variant?: 'default' | 'danger' | 'success' | 'loading'
  /**
   * Optional: Hint message to show below the input.
   */
  hintMessage?: string
  /**
   * Optional: Placeholder text to show in the input.
   */
  placeholder?: string
  /**
   * Optional: Limit the file types that can be selected. Defaults to all file types.
   * @example 'image/*' or '.png'
   */
  acceptedFileTypes?: string | Record<string, string[]>
}>(), {
  modelValue: undefined,
  label: undefined,
  variant: 'default',
  hintMessage: undefined,
  placeholder: undefined,
  acceptedFileTypes: undefined,
  validationFunction: undefined,
  minFileSize: undefined,
  maxFileSize: undefined,
})

const { internalDisabled } = useEnableAfterSsr(computed(() => props.disabled))

const emit = defineEmits<{
  'update:modelValue': [value?: File]
}>()

const id = makeIntoUniqueKey('file-input')
const hintId = makeIntoUniqueKey('file-input-hint')

const containerVariantClasses = computed(() => {
  if (internalDisabled.value) return 'tw-bg-neutral-50 tw-ring-neutral-200 tw-cursor-not-allowed'

  switch (props.variant) {
    case 'danger':
      return 'tw-cursor-pointer tw-ring-red-400'
    case 'success':
      return 'tw-cursor-pointer tw-ring-green-500'
    default:
      return 'tw-cursor-pointer tw-ring-neutral-300'
  }
})

const placeholderLabelVariantClasses = computed(() => {
  if (internalDisabled.value) return 'tw-text-neutral-400'

  switch (props.variant) {
    case 'danger':
      return 'tw-text-red-400'
    case 'success':
      return 'tw-text-green-500'
    default:
      return 'tw-text-neutral-400'
  }
})

// Zag ----------------------------------------------------------------------------------------------------------------

const transientContext = computed(() => ({
  disabled: internalDisabled.value,
  value: props.modelValue,
  validate: props.validationFunction,
  accept: props.acceptedFileTypes,
  minFileSize: props.minFileSize,
  maxFileSize: props.maxFileSize,
}))

const [state, send] = useMachine(fileUpload.machine({
  id,
  maxFiles: 1,
  onFileAccept: (file) => {
    emit('update:modelValue', file.files[0] ?? undefined)
  },
}), {
  context: transientContext,
})

const api = computed(() => fileUpload.connect(state.value, send, normalizeProps))
</script>
