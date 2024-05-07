<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
MInputSingleFile(
  :label="label"
  :model-value="selectedFile"
  @update:model-value="onFileSelected"
  :validation-function="validationFunction"
  :disabled="disabled"
  :variant="fileError ? 'danger' : variant"
  :hint-message="fileError ?? hintMessage"
  :placeholder="placeholder"
  :accepted-file-types="acceptedFileTypes"
  )
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { type FileError } from '@zag-js/file-upload'
import MInputSingleFile from './MInputSingleFile.vue'

const props = withDefaults(defineProps<{
  /**
   * The value of the input. Can be undefined.
   */
  modelValue?: string
  /**
   * Optional: A function that validates the selected files. Should return an array of errors.
   * @param file The file to validate.
   */
  validationFunction?: (file: File) => FileError[] | null
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
   * @example 'image/*' or '.png' or '.png,.jpg,.jpeg'
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
})

const emit = defineEmits<{
  'update:modelValue': [value?: string]
}>()

const selectedFile = ref<File>()
const fileError = ref<string>()

async function onFileSelected (file?: File) {
  if (!file) {
    selectedFile.value = undefined
    emit('update:modelValue', undefined)
    return
  }

  try {
    const fileContents = await file.text()
    selectedFile.value = file
    emit('update:modelValue', fileContents)
  } catch (error) {
    fileError.value = 'Failed to parse file contents.'
  }
}
</script>
