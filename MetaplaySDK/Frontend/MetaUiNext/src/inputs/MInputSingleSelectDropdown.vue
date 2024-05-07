<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
//- Container.
div
  //- Label.
  label(
    v-bind="api.labelProps"
    v-if="label"
    :for="id"
    :class="['tw-block tw-text-sm tw-font-bold tw-leading-6 tw-mb-1', { 'tw-text-neutral-400': internalDisabled, 'tw-text-neutral-900': !internalDisabled }]"
    ) {{ label }}

  //- Input.
  div
    button(
      ref="trigger"
      v-bind="api.triggerProps"
      :class="['tw-w-full tw-flex tw-justify-between tw-px-3 tw-rounded-md tw-shadow-sm tw-border-0 tw-bg-white tw-py-1.5 tw-overflow-x-hidden tw-text-neutral-900 tw-ring-1 tw-ring-inset placeholder:tw-text-neutral-400 focus:tw-ring-2 focus:tw-ring-inset focus:tw-ring-blue-600 sm:tw-text-sm sm:tw-leading-6 disabled:tw-cursor-not-allowed disabled:tw-bg-neutral-50 disabled:tw-text-neutral-500 disabled:ring-neutral-200', variantClasses]"
      )
        slot(name="selection" :value="api.value[0]")
          span(:class="{ 'tw-text-neutral-400': !api.valueAsString }") {{ api.valueAsString || placeholder }}
          span(class="tw-text-neutral-400") ▼

  //- Options popover.
  component(
    :is="props.teleportToBody ? 'teleport' : 'div'"
    :to="props.teleportToBody ? 'body' : undefined"
    )
    div(
      v-bind="api.positionerProps"
      style="z-index: 1;"
      )
      ul(
        ref="listbox"
        v-bind="api.contentProps"
        class="tw-rounded-md tw-bg-white tw-shadow-lg tw-border tw-border-neutral-300 tw-text-sm tw-max-h-80 tw-overflow-y-auto tw-overflow-ellipsis tw-overflow-x-hidden"
        )
        li(
          v-bind="api.getItemProps({ item: option })"
          v-for="option in options"
          :key="option.value"
          :class="['tw-px-3 tw-py-1.5 first:tw-rounded-t-md last:tw-rounded-b-md tw-cursor-pointer', { '!tw-bg-blue-500 hover:!tw-bg-blue-600 !tw-text-white': api.selectedItems.some((selectedOption) => selectedOption.value === option.value), 'tw-text-neutral-400 tw-bg-neutral-50 tw-cursor-not-allowe tw-italic': !!option.disabled }]"
          :data-testid="`select-option-${option.label}`"
          )
            slot(name="option" :option="getOptionInfo(option)")
              div(class="tw-flex tw-justify-between")
                span {{ option.label }}
                span(v-bind="api.getItemIndicatorProps({ item: option })" class="tw-ml-2") ✓

  //- Hint message.
  div(
    v-if="hintMessage"
    :class="['tw-text-xs tw-text-neutral-400 tw-mt-1', { 'tw-text-red-400': variant === 'danger' }]"
    ) {{ hintMessage }}
</template>

<script setup lang="ts" generic="T extends string">
import { computed, watch, ref, onMounted } from 'vue'
import { makeIntoUniqueKey } from '../utils/generalUtils'
import * as select from '@zag-js/select'
import { normalizeProps, useMachine } from '@zag-js/vue'
import { useEnableAfterSsr } from '../composables/useEnableAfterSsr'

const props = withDefaults(defineProps<{
  /**
   * The value of the input. Can be undefined.
   */
  modelValue?: T
  /**
   * The collection of items to show in the select.
   */
  options: Array<{ label: string, value: T, disabled?: boolean }>
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
  variant?: 'default' | 'danger' | 'success'
  /**
   * Optional: Hint message to show below the input.
   */
  hintMessage?: string
  /**
   * Optional: Placeholder text to show in the input.
   */
  placeholder?: string
  /**
   * Optional: Whether to teleport the options popover to the HTML body. Defaults to false.
   * This is an advanced option that can help with z-index issues.
   */
  teleportToBody?: boolean
}>(), {
  modelValue: undefined,
  label: undefined,
  variant: 'default',
  hintMessage: undefined,
  placeholder: 'Select option',
})

const { internalDisabled } = useEnableAfterSsr(computed(() => props.disabled))

const emit = defineEmits<{
  'update:modelValue': [value: T]
}>()

const trigger = ref<HTMLElement | null>(null)
const listbox = ref<HTMLElement | null>(null)

onMounted(() => {
  // Get the rendered width of the trigger element and set it as the width of the listbox.
  if (trigger.value) {
    const width = trigger.value.getBoundingClientRect().width
    if (listbox.value) {
      listbox.value.style.width = `${width}px`
    }
  }
})

/**
 * Helper to get variant specific classes.
 */
const variantClasses = computed(() => {
  switch (props.variant) {
    case 'danger':
      return 'tw-ring-red-400 tw-text-red-400'
    case 'success':
      return 'tw-ring-green-500'
    default:
      return 'tw-ring-neutral-300'
  }
})

// Zag Select ---------------------------------------------------------------------------------------------------------

const id = makeIntoUniqueKey('select')

const transientContext = computed(() => ({
  disabled: internalDisabled.value,
  value: props.modelValue ? [props.modelValue] : undefined,
}))

const [state, send] = useMachine(select.machine({
  id,
  collection: select.collection({
    items: props.options,
    isItemDisabled: (item) => !!item.disabled,
  }),
  loop: true,
  // @ts-expect-error Zag Select types are not generic but we know that the value is of type T.
  onValueChange: ({ value }) => emit('update:modelValue', value[0]),
}), {
  context: transientContext,
})

const api = computed(() => select.connect(state.value, send, normalizeProps))

// Watch for prop updates.
watch(() => props.modelValue, (newValue) => {
  // Zag Select doesn't support undefined values.
  if (newValue === undefined) return
  api.value.setValue([newValue])
})

// Also watch for changes to the options.
watch(() => props.options, (newValue) => {
  api.value.setCollection(select.collection({ items: newValue }))
})

function getOptionInfo (option: { label: string, value: T }) {
  return {
    ...option,
    highlighted: api.value.highlightedItem?.value === option.value,
    selected: api.value.selectedItems.some((selectedOption) => selectedOption.value === option.value),
  }
}
</script>

<style scoped>
[data-part="item"][data-highlighted] {
  @apply tw-bg-neutral-200;
}
</style>
