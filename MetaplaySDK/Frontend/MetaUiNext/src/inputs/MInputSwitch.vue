<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
//- Container.
label(v-bind="api.rootProps")
  //- Hidden input for forms.
  input(v-bind="api.hiddenInputProps")

  //- Switch body.
  span(
    v-bind="api.controlProps"
    :class="['tw-relative tw-inline-flex tw-flex-shrink-0 tw-rounded-full tw-border-2 tw-border-transparent tw-transition-colors tw-duration-200 tw-ease-in-out focus:tw-outline-none focus:tw-ring-2 focus:tw-ring-indigo-500 focus:tw-ring-offset-2', variantClasses, bodySizeClasses, { 'tw-cursor-pointer': !internalDisabled, 'tw-cursor-not-allowed': internalDisabled }]"
    )
    //- Switch thumb.
    span(
      v-bind="api.thumbProps"
      :class="['tw-pointer-events-none tw-inline-block tw-transform tw-rounded-full tw-bg-white tw-shadow tw-ring-0 tw-transition tw-duration-200 tw-ease-in-out', thumbSizeClasses, { 'tw-bg-neutral-300': internalDisabled }]"
      )

  //- Screen reader label.
  span(
    v-bind="api.labelProps"
    class="tw-sr-only"
    )
    span(v-if="api.isChecked") On
    span(v-else) Off
</template>

<script setup lang="ts">
import * as zagSwitch from '@zag-js/switch'
import { normalizeProps, useMachine } from '@zag-js/vue'

import { computed, ref, watch } from 'vue'
import type { Variant } from '../utils/types'
import { makeIntoUniqueKey } from '../utils/generalUtils'
import { useEnableAfterSsr } from '../composables/useEnableAfterSsr'

const props = withDefaults(defineProps<{
  /**
   * The current value of the switch.
   */
  modelValue: boolean
  /**
   * Optional: Disable the switch. Defaults to false.
   */
  disabled?: boolean
  /**
   * Optional: The visual variant of the switch. Defaults to 'primary'.
   */
  variant?: Variant
  /**
   * Optional: The size of the switch. Defaults to 'md'.
   */
  size?: 'xs' | 'sm' | 'md'
  /**
   * Optional: The name of the switch for HTML form submission. Defaults to `undefined`.
   */
  name?: string
}>(), {
  disabled: false,
  variant: 'primary',
  size: 'md',
  name: undefined
})

const { internalDisabled } = useEnableAfterSsr(computed(() => props.disabled))

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

// Zag Switch ---------------------------------------------------------------------------------------------------------

const transientContext = computed(() => ({
  disabled: internalDisabled.value,
  name: props.name,
  checked: props.modelValue,
}))

const [state, send] = useMachine(zagSwitch.machine({
  id: makeIntoUniqueKey('switch'),
  onCheckedChange: ({ checked }) => emit('update:modelValue', checked),
}), {
  context: transientContext
})

const api = computed(() =>
  zagSwitch.connect(state.value, send, normalizeProps),
)

// Pass prop updates to Zag.
watch(() => props.modelValue, (newValue) => {
  api.value.setChecked(newValue)
})

// UI visuals ---------------------------------------------------------------------------------------------------------

const variantClasses = computed(() => {
  if (props.variant === 'success') {
    if (internalDisabled.value) return api.value.isChecked ? 'tw-bg-green-200' : 'tw-bg-neutral-200'
    else return api.value.isChecked ? 'tw-bg-green-500' : 'tw-bg-neutral-300'
  }

  if (props.variant === 'warning') {
    if (internalDisabled.value) return api.value.isChecked ? 'tw-bg-orange-200' : 'tw-bg-neutral-300'
    else return api.value.isChecked ? 'tw-bg-orange-500' : 'tw-bg-neutral-300'
  }

  if (props.variant === 'danger') {
    if (internalDisabled.value) return api.value.isChecked ? 'tw-bg-red-200' : 'tw-bg-neutral-300'
    else return api.value.isChecked ? 'tw-bg-red-500' : 'tw-bg-neutral-300'
  }

  if (internalDisabled.value) return api.value.isChecked ? 'tw-bg-blue-200' : 'tw-bg-neutral-200'
  else return api.value.isChecked ? 'tw-bg-blue-500' : 'tw-bg-neutral-300'
})

const bodySizeClasses = computed(() => {
  if (props.size === 'xs') return 'tw-w-6 tw-h-3'
  else if (props.size === 'sm') return 'tw-w-8 tw-h-4.5'
  else return 'tw-w-11 tw-h-6'
})

const thumbSizeClasses = computed(() => {
  if (props.size === 'xs') {
    const size = 'tw-w-2 tw-h-2'
    if (api.value.isChecked) return `${size} tw-translate-x-3`
    else return `${size} tw-translate-x-0`
  }

  if (props.size === 'sm') {
    const size = 'tw-w-3.5 tw-h-3.5'
    if (api.value.isChecked) return `${size} tw-translate-x-3.5`
    else return `${size} tw-translate-x-0`
  }

  const size = 'tw-w-5 tw-h-5'
  if (api.value.isChecked) return `${size} tw-translate-x-5`
  else return `${size} tw-translate-x-0`
})
</script>
