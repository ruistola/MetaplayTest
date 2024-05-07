<template lang="pug">
span(
  v-bind="{ ...triggerProps, ...$attrs}"
  class="tw-inline-flex tw-text-sm tw-pr-2 tw-rounded-full tw-ring-1 tw-ring-opacity-20"
  :class="[VariantClasses, VariantTextColors, TriggerSizeClasses]"
  :variant="variant"
  @click="open"
  )
  //- Icon from https://heroicons.com/
  svg(
    xmlns="http://www.w3.org/2000/svg"
    viewBox="0 0 20 20"
    fill="currentColor"
    :class="['tw-w-5 tw-h-5 tw-transition-transform', { 'tw-rotate-90': api.isOpen }]"
    )
    path(
      fill-rule="evenodd"
      d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
      clip-rule="evenodd"
      )
  | {{ triggerLabel }}

Teleport(to="body")
  div(v-bind="api.positionerProps")
    div(
      v-bind="api.arrowProps"
      v-show="api.isOpen"
      )
      div(
        v-bind="api.arrowTipProps"
        class="tw-border-t tw-border-l tw-border-neutral-200 tw-z-0"
        )
    div(
      v-bind="api.contentProps"
      class="tw-bg-white tw-border tw-border-neutral-200 tw-rounded-lg tw-shadow-md tw-pt-4 tw-max-w-sm tw-max-h-[calc(100vh_-_100px)] tw-overflow-auto"
      )
      div(
        role="heading"
        class="tw-font-bold tw-text-sm tw-px-4"
        ) {{ title }}
      div(
        v-if="subtitle"
        class="tw-text-neutral-500 tw-text-sm tw-my-2 tw-px-4"
        ) {{ subtitle }}
      div(
        v-else
        :class="['tw-mt-1 tw-text-sm', VariantTextColors, { 'tw-px-4 tw-pb-4': !noBodyPadding }]"
        )
        slot Content TBD
</template>

<script setup lang="ts">
import * as popover from '@zag-js/popover'
import { normalizeProps, useMachine } from '@zag-js/vue'
import { computed, Teleport } from 'vue'

import { makeIntoUniqueKey } from '../utils/generalUtils'
import type { Variant } from '../utils/types'

const props = withDefaults(defineProps<{
  /**
   * The label of the popover trigger button.
   */
  triggerLabel: string
  /**
   * The title of the popover.
   */
  title: string
  /**
   * Optional: The subtitle of the popover. If not provided, the content slot will be used as the body.
   */
  subtitle?: string
  /**
   * Optional: Whether to remove the padding from the popover.
   */
  noBodyPadding?: boolean
  /**
   * Optional: The variant of the trigger button and body content.
   */
  variant?: Variant
  /**
   * Optional: The size of the trigger button. Defaults to 'default'.
   */
  size?: 'small' | 'default'
}>(), {
  subtitle: undefined,
  variant: 'neutral',
  size: 'default',
})

const [state, send] = useMachine(popover.machine({
  id: makeIntoUniqueKey('popover'),
  modal: true,
}))

defineExpose({
  open,
})

function open () {
  api.value.open()
}

const api = computed(() => popover.connect(state.value, send, normalizeProps))

// Broken types.
const triggerProps = computed(() => api.value.triggerProps as any)

const VariantClasses = computed(() => {
  const classes: { [index: string]: string } = {
    primary: 'tw-bg-blue-200 hover:tw-bg-blue-300 active:tw-bg-blue-400 tw-ring-blue-600',
    neutral: 'tw-bg-neutral-200 hover:tw-bg-neutral-300 active:tw-bg-neutral-400 tw-ring-neutral-600',
    success: 'tw-bg-green-200 hover:tw-bg-green-300 active:tw-bg-green-400 tw-ring-green-600',
    danger: 'tw-bg-red-200 hover:tw-bg-red-300 active:tw-bg-red-400 tw-ring-red-600',
    warning: 'tw-bg-orange-200 hover:tw-bg-orange-300 active:tw-bg-orange-400 tw-ring-orange-600',
  }
  return classes[props.variant]
})

const VariantTextColors = computed(() => {
  const classes: { [index: string]: string } = {
    primary: 'tw-text-blue-800',
    neutral: 'tw-text-neutral-800',
    success: 'tw-text-green-900',
    danger: 'tw-text-red-900',
    warning: 'tw-text-orange-900',
  }
  return classes[props.variant]
})

const TriggerSizeClasses = computed(() => {
  const classes: { [index: string]: string } = {
    default: 'tw-py-1',
    small: 'tw-py-0',
  }
  return classes[props.size]
})

</script>

<style scoped>
[data-part="arrow"] {
  --arrow-background: white;
  --arrow-size: 16px;
}

@media (max-width: 390px) {
  [data-part="content"][data-state="open"] {
  max-width: calc(100vw - 16px);
  }
}
</style>
