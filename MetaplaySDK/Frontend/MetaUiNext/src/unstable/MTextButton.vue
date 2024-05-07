<template lang="pug">
MTooltip(
  :content="tooltipContent"
  no-underline
  )
  //- Anchor for external links.
  a(
    v-if="linkTargetType === 'external'"
    v-bind="$attrs"
    :href="linkUrl"
    :class="textVariantClasses"
    )
    //- Icon slot
    span(class="tw-inline-flex tw-items-center tw-space-x-2 ")
      slot(v-if="$slots.icon" name="icon")
      slot Link Text TBD
  //- Router link for internal links.
  router-link(
    v-else-if="linkTargetType === 'internal'"
    v-bind="$attrs"
    :to="linkUrl || ''"
    :class="textVariantClasses"
    )
    //- Icon slot
    span(class="tw-inline-flex tw-items-center tw-space-x-2 ")
      slot(v-if="$slots.icon" name="icon")
      slot Link Text TBD
  //- Button for everything else.
  button(
    v-else
    v-bind="$attrs"
    @click="emit('click')"
    :class="textVariantClasses"
    :disabled="isDisabled"
    )
    //- Icon slot
    span(class="tw-inline-flex tw-items-center tw-space-x-2 ")
      slot(v-if="$slots.icon" name="icon")
      slot Link Text TBD
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'

import { useEnableAfterSsr } from '../composables/useEnableAfterSsr'
import { doesHavePermission } from '../composables/usePermissions'

import MTooltip from '../primitives/MTooltip.vue'

import type { Variant } from '../utils/types'

defineOptions({
  inheritAttrs: false
})

const props = withDefaults(defineProps<{
  /**
   * Optional: The route to navigate to when the button is clicked.
   */
  to?: string
  /**
   * Optional: Disable the button. Defaults to `false`.
   */
  disabled?: boolean
  /**
   * Optional: Disable the button and show a tooltip with the given text.
   */
  disabledTooltip?: string
  /**
   * Optional: Set the visual variant of the text button. Defaults to 'primary'.
   */
  variant?: Variant
  /**
   * Optional: The permission required to use this button. If the user does not have this permission the button will be disabled with a tooltip.
   */
  permission?: string
}>(), {
  to: undefined,
  disabledTooltip: undefined,
  variant: 'primary',
  permission: undefined,
})

/**
 * Prevents disabled buttons from flashing as enabled on first render.
 */
const { internalDisabled } = useEnableAfterSsr(computed(() => props.disabled))

const emit = defineEmits(['click'])

const router = useRouter()

/**
 * Figure out what type of link, if any, the button's target is.
 */
const linkTargetType = computed(() => {
  if (isDisabled.value) return undefined
  if (props.to?.startsWith('http')) return 'external'
  if (props.to && router) return 'internal'
  return undefined
})

/**
 * The URL to navigate to when the button is clicked. This is also previewed when hovering over the button.
 * If the button is disabled, the link will be `undefined`.
 */
const linkUrl = computed(() => {
  if (isDisabled.value) return undefined
  // Return the external site url.
  if (props.to?.startsWith('http')) return props.to
  // Resolve an internal route path into a valid URL.
  if (props.to && router) return router.resolve(props.to).href
  return undefined
})

/**
 * Tooltip content to be shown when the button is disabled.
 */
const tooltipContent = computed(() => {
  if (props.disabledTooltip) return props.disabledTooltip
  if (props.permission && !hasGotPermission.value) return `You need the '${props.permission}' permission to use this feature.`
  return undefined
})

/**
 * Whether the button is disabled.
 */
const isDisabled = computed(() => {
  if (internalDisabled.value || props.disabledTooltip) return true
  if (props.permission && !hasGotPermission.value) return true
  return false
})

/**
 * Whether the user has the required permission to use this button.
 */
const hasGotPermission = computed(() => {
  return doesHavePermission(props.permission)
})

/**
 * The classes to apply to the button based on its variant and disabled state.
 */
const textVariantClasses = computed(() => {
  const variants: { [key: string]: string } = {
    neutral: 'tw-text-neutral-500 hover:tw-text-neutral-600 active:tw-text-neutral-800 focus:tw-ring-neutral-400 tw-cursor-pointer hover:tw-underline',
    success: 'tw-text-green-500 hover:tw-text-green-600 active:tw-text-green-800 focus:tw-ring-green-400 tw-cursor-pointer hover:tw-underline',
    warning: 'tw-text-orange-400 hover:tw-text-orange-500 active:tw-text-orange-600 focus:tw-ring-orange-400 tw-cursor-pointer hover:tw-underline',
    danger: 'tw-text-red-400 hover:tw-text-red-500 active:tw-text-red-600 focus:tw-ring-red-400 tw-cursor-pointer hover:tw-underline',
    primary: 'tw-text-blue-500 hover:tw-text-blue-600 active:tw-text-blue-800 focus:tw-ring-blue-400 tw-cursor-pointer hover:tw-underline',
  }

  return isDisabled.value ? 'tw-text-neutral-400 tw-cursor-not-allowed' : variants[props.variant]
})

</script>
