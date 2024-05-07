<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
//- TODO: Check for tab selection behaviour.

//- Container.
div(v-bind="api.rootProps")
  //- Label.
  div(
    v-if="label"
    role="heading"
    :class="['tw-block tw-text-sm tw-leading-6', { 'tw-text-neutral-400': internalDisabled, 'tw-font-bold tw-mb-0.5' : props.size === 'default', 'tw-font-semibold' : props.size === 'small' }]"
    v-bind="api.labelProps"
    ) {{ label }}

  //- Input.
  div(
    :class="['tw-flex tw-gap-0.5', { 'tw-flex-col': props.vertical, 'tw-gap-x-2 tw-flex-wrap': !props.vertical }]"
    )
    label(
      v-for="option in options"
      :key="option.label"
      v-bind="api.getItemProps({ value: option.label })"
      :class="['tw-flex tw-items-center tw-gap-x-1 tw-mb-0', { 'tw-cursor-not-allowed': internalDisabled, 'tw-cursor-pointer': !internalDisabled }]"
      :data-testid="`radio-button-${option.label}`"
      )
        div(
          :class="getRadioButtonClasses(option)"
          v-bind="api.getItemControlProps({ value: option.label })"
          )
          div(
            :class="['tw-bg-white tw-rounded-full', { 'tw-h-1.5 tw-w-1.5': props.size === 'default', 'tw-h-[0.3125rem] tw-w-[0.3125rem]': props.size === 'small' }]"
          )
        span(
          v-bind="api.getItemTextProps({ value: option.label })"
          :class="[labelVariantClasses, 'tw-overflow-hidden tw-overflow-ellipsis']"
          ) {{ option.label }}
        input(v-bind="api.getItemHiddenInputProps({ value: option.label })")

  //- Hint message.
  div(
    v-if="hintMessage"
    :class="['tw-text-xs tw-text-neutral-400 tw-mt-1', { 'tw-text-red-400': variant === 'danger' }]"
    ) {{ hintMessage }}
</template>

<script setup lang="ts" generic="T extends string | number">
import { computed, watch } from 'vue'
import { makeIntoUniqueKey } from '../utils/generalUtils'
import * as radio from '@zag-js/radio-group'
import { normalizeProps, useMachine } from '@zag-js/vue'
import { useEnableAfterSsr } from '../composables/useEnableAfterSsr'

defineOptions({
  inheritAttrs: false,
})

const props = withDefaults(defineProps<{
  /**
   * The value of the input. Can be undefined.
   */
  modelValue?: T
  /**
   * The collection of items to show in the select.
   */
  options: Array<{ label: string, value: T }>
  /**
   * Optional: Show a label for the input. Defaults to undefined.
   */
  label?: string
  /**
   * Optional: Disable the input. Defaults to false.
   */
  disabled?: boolean
  /**
   * Optional: Visual variant of the input. Defaults to 'default'.
   */
  variant?: 'default' | 'danger' | 'success'
  /**
   * Optional: Hint message to show below the input.
   */
  hintMessage?: string
  /**
   * Optional: Stack the radio buttons vertically instead of horizontally. Defaults to false.
   */
  vertical?: boolean
  /**
   * Optional: Set the size of the radio buttons to fit with the surrounding content. Defaults to 'default'.
   */
  size?: 'small' | 'default'
}>(), {
  modelValue: undefined,
  label: undefined,
  variant: 'default',
  hintMessage: undefined,
  description: undefined,
  size: 'default',
})

const { internalDisabled } = useEnableAfterSsr(computed(() => props.disabled))

const emit = defineEmits<{
  'update:modelValue': [value: T]
}>()

/**
 * Helper to get variant specific classes.
 */
function getRadioButtonClasses (option: { label: string, value: T }): string {
  const baseClasses = 'tw-rounded-full tw-border tw-flex tw-items-center tw-justify-center tw-flex-none'

  const sizeRadiusClasses: { [index: string]: string } = {
    default: 'tw-h-4 tw-w-4',
    small: 'tw-h-3.5 tw-w-3.5'
  }
  const radiusSize = sizeRadiusClasses[props.size]

  const variantClasses: { [index: string]: string } = {
    danger: 'tw-border-red-400 tw-bg-red-500',
    success: 'tw-border-green-500 tw-bg-green-500',
    neutral: 'tw-border-neutral-300 tw-bg-neutral-50',
    default: 'tw-border-blue-300 tw-bg-blue-500'
  }

  if (props.modelValue === option.value) {
    return `${baseClasses} ${radiusSize} ${variantClasses[props.variant]}`
  } else {
    return `${baseClasses} ${radiusSize} ${variantClasses.neutral}`
  }
}

/**
 * Helper to get variant specific classes.
 */
const labelVariantClasses = computed(() => {
  const baseClasses = props.size === 'small' ? 'tw-text-sm' : 'tw-text-base'

  if (internalDisabled.value) return `${baseClasses} tw-text-neutral-400`
  if (props.variant === 'danger') return `${baseClasses} tw-text-red-400`
  else return `${baseClasses} tw-text-inherit`
})

// Zag ----------------------------------------------------------------------------------------------------------------

const transientContext = computed(() => ({
  value: String(props.modelValue),
  disabled: internalDisabled.value,
}))

const [state, send] = useMachine(radio.machine({
  id: makeIntoUniqueKey('radio'),
  onValueChange: (details) => emit('update:modelValue', props.options.find((option) => option.label === details.value)?.value ?? props.options[0].value),
}), {
  context: transientContext,
})

const api = computed(() =>
  radio.connect(state.value, send, normalizeProps),
)

// Watch for prop updates.
watch(() => props.options, (newValue) => {
  // If there are two options with the same label, throw an error.
  const labels = newValue.map((option) => option.label)
  if (labels.length !== new Set(labels).size) {
    console.warn('Duplicate labels found in the options array of a ragio group. This is confusing for users. Options: ', labels)
  }
})
</script>
