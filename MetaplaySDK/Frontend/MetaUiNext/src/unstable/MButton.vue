<template lang="pug">
span(
  :data-testid="`${dataTestid}-button-root`"
  :safety-lock-active="isSafetyLockEnabled ? 'yes' : 'no'"
  )
  MTooltip(
    :content="tooltipContent"
    no-underline
    )
    span(
      :proof="makeIntoUniqueKey('proof')"
      :class="['tw-relative tw-rounded-full tw-items-center tw-space-x-1', {'tw-flex': safetyLock || fullWidth }]")
      //- -tw-end-3.5 is the width of the safety lock background. tw-absolute causing the hidden safetylock to also take space.
      //- TODO: needs to adjust dynamically depending on width of button with tw-w-16.
      //- Investigate margins when safety lock is not enabled.
      span(
        v-if="safetyLock"
        @click="isSafetyLockEnabled = !isSafetyLockEnabled"
        :class="['tw-absolute -tw-end-2 tw-rounded-e-full tw-pr-10 tw-w-2/3 tw-h-full tw-z-0 tw-cursor-pointer tw-transition-colors', safetyLockClasses]"
        data-testid="safety-lock-button"
        )
      component(
        :is="linkUrl ? 'a' : 'button'"
        v-bind="$attrs"
        type="button"
        :href="linkUrl"
        :class="['tw-inline-flex tw-place-content-center tw-rounded-full tw-cursor-pointer tw-no-underline tw-transition-colors tw-z-10', { 'tw-flex-grow': fullWidth }, buttonVariantClasses]"
        style="text-decoration-line: none;"
        :disabled="isDisabled"
        :data-testid="dataTestid"
        @click.stop.prevent="onClick"
        )
          //- ~20px icons (tw-w-5 tw-h-5) seem to look good on the default size buttons.
          div(class="tw-inline-flex tw-items-center tw-justify-center tw-rounded-full tw-transition-transform" :class="buttonSizeClasses")
            div(
              v-if="$slots.icon"
              :class="buttonIconSizeClasses"
              )
              slot(name="icon")
            div(
              v-if="$slots.default"
              :class="[buttonLabelSizeClasses, { 'tw-ml-0.5': $slots.icon }]"
              )
              slot
      span(
        v-if="safetyLock"
        class=" tw-text-white tw-z-0 tw-pointer-events-none"
        )
        <svg v-show="isSafetyLockEnabled" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="tw-size-3.5">
          <path fill-rule="evenodd" d="M12 1.5a5.25 5.25 0 0 0-5.25 5.25v3a3 3 0 0 0-3 3v6.75a3 3 0 0 0 3 3h10.5a3 3 0 0 0 3-3v-6.75a3 3 0 0 0-3-3v-3c0-2.9-2.35-5.25-5.25-5.25Zm3.75 8.25v-3a3.75 3.75 0 1 0-7.5 0v3h7.5Z" clip-rule="evenodd" />
        </svg>

        <svg v-show="!isSafetyLockEnabled" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="tw-size-3.5 tw-relative tw-left-[1px]">
          <path d="M18 1.5c2.9 0 5.25 2.35 5.25 5.25v3.75a.75.75 0 0 1-1.5 0V6.75a3.75 3.75 0 1 0-7.5 0v3a3 3 0 0 1 3 3v6.75a3 3 0 0 1-3 3H3.75a3 3 0 0 1-3-3v-6.75a3 3 0 0 1 3-3h9v-3c0-2.9 2.35-5.25 5.25-5.25Z" />
        </svg>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

import { doesHavePermission } from '../composables/usePermissions'
import { getSafetyLockEnabledByDefault } from '../composables/useSafetyLock'

import type { Variant } from '../utils/types'
import { useRouter } from 'vue-router'

import MTooltip from '../primitives/MTooltip.vue'
import { useEnableAfterSsr } from '../composables/useEnableAfterSsr'
import { makeIntoUniqueKey } from '../utils/generalUtils'

defineOptions({
  inheritAttrs: false
})

export type ButtonSize = 'small' | 'default'

const props = withDefaults(defineProps<{
  /**
   * Optional: Set the size of the button.
   */
  size?: ButtonSize
  /**
   * Optional: Set the visual variant of the button. Defaults to 'primary'.
   */
  variant?: Variant
  /**
   * Optional: The icon to show in the button.
   */
  icon?: string
  /**
   * Optional: Disable the button. Defaults to `false`.
   */
  disabled?: boolean
  /**
   * Optional: Disable the button and show a tooltip with the given text.
   */
  disabledTooltip?: string
  /**
   * Optional: The permission required to use this button. If the user does not have this permission the button will be disabled with a tooltip.
   */
  permission?: string
  /**
   * Optional: The route to navigate to or the external site url when the button is clicked. If this is set the button will be rendered as a link.
   */
  to?: string
  /**
   * Optional: Disables the button if the safety lock is on.
   */
  safetyLock?: boolean
  /**
   * Optional: Set the button to full width.
   */
  fullWidth?: boolean
  /**
   * Optional: Add a `data-testid` element to the button.
   */
  dataTestid?: string
}>(), {
  size: 'default',
  variant: 'primary',
  icon: undefined,
  permission: undefined,
  to: undefined,
  disabledTooltip: undefined,
  safetyLock: false,
  fullWidth: undefined,
  dataTestid: undefined,
})

const { internalDisabled } = useEnableAfterSsr(computed(() => props.disabled))

const emit = defineEmits(['click'])

const router = useRouter()

/**
 * Tooltip content to be shown when the button is disabled.
 */
const tooltipContent = computed(() => {
  if (props.disabledTooltip) return props.disabledTooltip
  if (props.permission && !hasGotPermission.value) return `You need the '${props.permission}' permission to use this feature.`
  if (isSafetyLockEnabled.value) return 'Disable the safety lock first.'
  return undefined
})

/**
 * Whether the button is disabled.
 */
const isDisabled = computed(() => {
  if (internalDisabled.value || props.disabledTooltip) return true
  if (props.permission && !hasGotPermission.value) return true
  if (isSafetyLockEnabled.value) return true
  return false
})

/**
 * The URL to navigate to when the button is clicked and can be previewed when hovering over the button.
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
 * Initial status of the safety lock. Not a computed variable on purpose.
 */
const isSafetyLockEnabled = ref(props.safetyLock ? getSafetyLockEnabledByDefault() : false)

/**
 * The click handler for the button that handles both internal and external links.
 */
function onClick () {
  if (isDisabled.value) return

  // Check if is valid url, that opens to external site.
  if (props.to?.startsWith('http')) {
    window.open(props.to, '_blank')
  } else if (props.to && router) {
    // If not external, then it's internal, so use router.
    void router.push(props.to)
  }

  emit('click')
}

const hasGotPermission = computed(() => {
  if (props.permission) return doesHavePermission(props.permission)
  return true
})

const buttonVariantClasses = computed(() => {
  const activeClass = 'active:tw-text-neutral-200 active:tw-drop-shadow-sm'

  const baseClass = props.safetyLock ? 'm-button-with-lock' : 'm-button'

  if (isDisabled.value) {
    const disabledVariants = {
      neutral: `${baseClass}-neutral tw-bg-neutral-300 tw-text-white !tw-cursor-not-allowed`,
      success: `${baseClass}-success tw-bg-green-300 tw-text-white !tw-cursor-not-allowed`,
      warning: `${baseClass}-warning tw-bg-orange-300 tw-text-white !tw-cursor-not-allowed`,
      danger: `${baseClass}-danger tw-bg-red-300 tw-text-white !tw-cursor-not-allowed`,
      primary: `${baseClass}-primary tw-bg-blue-300 tw-text-white !tw-cursor-not-allowed`,
    }
    return disabledVariants[props.variant]
  }

  const variants = {
    neutral: `${baseClass}-neutral tw-text-white hover:tw-text-white tw-bg-neutral-500 hover:tw-bg-neutral-600 active:tw-bg-neutral-700 focus:tw-ring-neutral-400 tw-drop-shadow-sm`,
    success: `${baseClass}-success tw-text-white hover:tw-text-white tw-bg-green-500 hover:tw-bg-green-600 active:tw-bg-green-700 focus:tw-ring-green-400 tw-drop-shadow-sm`,
    warning: `${baseClass}-warning tw-text-white hover:tw-text-white tw-bg-orange-400 hover:tw-bg-orange-500 active:tw-bg-orange-600 focus:tw-ring-orange-400 tw-drop-shadow-sm`,
    danger: `${baseClass}-danger tw-text-white hover:tw-text-white tw-bg-red-400 hover:tw-bg-red-500 active:tw-bg-red-600 focus:tw-ring-red-400 tw-drop-shadow-sm`,
    primary: `${baseClass}-primary tw-text-white hover:tw-text-white tw-bg-blue-500 hover:tw-bg-blue-600 active:tw-bg-blue-700 focus:tw-ring-blue-400 tw-drop-shadow-sm`,
  }

  return [variants[props.variant], activeClass].join('')
})

const buttonSizeClasses = computed(() => {
  const activeClass = 'active:tw-translate-y-px'

  const classes = {
    small: 'tw-px-2.5 tw-h-full',
    default: 'tw-px-3 tw-h-full',
  }

  if (isDisabled.value) {
    return classes[props.size]
  } else {
    return [classes[props.size], activeClass].join(' ')
  }
})

const buttonIconSizeClasses = computed(() => {
  const classes = {
    small: 'tw-my-0.5',
    default: undefined,
  }

  return classes[props.size]
})

const buttonLabelSizeClasses = computed(() => {
  const classes = {
    small: 'tw-text-sm tw-my-1.5 tw-font-semibold',
    default: 'tw-text-sm tw-my-2 tw-font-bold',
  }

  return classes[props.size]
})

const safetyLockClasses = computed(() => {
  if (isSafetyLockEnabled.value) {
    return 'lock-locked tw-bg-red-500 hover:tw-bg-red-600 active:tw-bg-red-700'
  } else {
    return 'lock-unlocked tw-bg-green-400 hover:tw-bg-green-500 active:tw-bg-green-600'
  }
})
</script>

<style>
/* Button shadows. There's a million permutations to make all of this work without JS. */

.m-button-primary {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #2473b0, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-primary {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #2473b0, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-primary:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #1b5684, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-primary:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #1b5684, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-primary:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -8px 0px -6px #123a58, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-primary:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -8px 0px -6px #123a58, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-primary:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #57a6e3, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-primary:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #57a6e3, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-success {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #325226, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-success {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #325226, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-success:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #263e1d, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-success:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #263e1d, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-success:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -8px 0px -6px #192913, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-success:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -8px 0px -6px #192913, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-success:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #658559, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-success:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #658559, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-warning {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #ff7a00, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-warning {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #ff7a00, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-warning:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #cc6200, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-warning:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #cc6200, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-warning:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -8px 0px -6px #994900, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-warning:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -8px 0px -6px #994900, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-warning:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #e6750b, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-warning:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #e6750b, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-danger {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #ef4444, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-danger {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #ef4444, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-danger:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #dc2626, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-danger:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px #dc2626, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-danger:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -8px 0px -6px #b91c1c, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-danger:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -8px 0px -6px #b91c1c, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-danger:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px rgb(248 113 113), inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-danger:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.2), inset 0 -9px 0px -6px rgb(248 113 113), inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-neutral {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.1), inset 0 -9px 0px -6px #525252, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-neutral {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.1), inset 0 -9px 0px -6px #525252, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-neutral:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.1), inset 0 -9px 0px -6px #404040, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-neutral:hover {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.1), inset 0 -9px 0px -6px #404040, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-neutral:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.1), inset 0 -8px 0px -6px #262626, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-neutral:active {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.1), inset 0 -8px 0px -6px #262626, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

.m-button-neutral:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.1), inset 0 -9px 0px -6px #a3a3a3, inset 0 -10px 1px -4px rgba(255,255,255, 0.05);
}

.m-button-with-lock-neutral:disabled {
  box-shadow: inset 0px 5px 0px -4px rgba(255, 255, 255, 0.1), inset 0 -9px 0px -6px #a3a3a3, inset 0 -10px 1px -4px rgba(255,255,255, 0.05), 4px 0px 0px -2px rgba(0, 0, 0, 0.09);
}

/* Safety lock's clickable background element */

.lock-locked {
  box-shadow: inset 0px 3px 0px -1px #dc2626, inset 0px -3px 0px -1px rgba(255, 255, 255, 0.4);
}

.lock-locked:hover {
  box-shadow: inset 0px 3px 1px -1px #b91c1c, inset 0px -3px 0px -1px rgba(255, 255, 255, 0.4);
}

.lock-locked:active {
  box-shadow: inset 0px 3px 1px -1px #991b1b, inset 0px -3px 0px -1px rgba(255, 255, 255, 0.4);
}

.lock-unlocked {
  box-shadow: inset 0px 3px 1px -1px #3f6730, inset 0px -3px 0px -1px rgba(255, 255, 255, 0.4);
}

.lock-unlocked:hover {
  box-shadow: inset 0px 3px 1px -1px #325226, inset 0px -3px 0px -1px rgba(255, 255, 255, 0.4);
}

.lock-unlocked:active {
  box-shadow: inset 0px 3px 1px -1px #263e1d, inset 0px -3px 0px -1px rgba(255, 255, 255, 0.4);
}

</style>
