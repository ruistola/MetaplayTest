<template lang="pug">
div(
  :class="['tw-rounded-lg tw-shadow tw-border', containerVariantClasses]"
  v-bind="$attrs"
  )
  //- Header container.
  div(class="tw-px-4 tw-pt-3 tw-pb-2")
    //- Loading placeholder.
    div(v-if="isLoading")
      div(
        class="tw-h-6 tw-w-6/12 tw-my-3 tw-bg-neutral-200 tw-rounded tw-animate-pulse"
        data-testid="overviewcard-loading-indicator"
        )

    div(v-else)
      //- Header.
      div(:class="['sm:tw-flex tw-items-center tw-justify-between']")
        //- Left side of header.
        div(:class="['tw-flex tw-items-center tw-space-x-2 tw-my-1 tw-text-2xl', headerVariantClasses]")
          //- TODO: Avatar.
          div(
            v-if="avatarImageUrl"
            class="tw-rounded-full tw-bg-neutral-100 tw-border tw-border-neutral-200 tw-border-opacity-25 hover:tw-brightness-90 active:tw-brightness-75 tw-cursor-pointer tw-w-9 tw-h-9 tw-flex tw-items-center tw-justify-center"
            )
            img(
              :src="avatarImageUrl"
              class="tw-rounded-full"
              )

          //- Title.
          span(
            role="heading"
            :class="['tw-font-bold tw-overflow-ellipsis']"
            data-testid="overviewcard-title"
            )
            slot(name="title") {{ title }}

        //- ID.
        div(
          v-if="id"
          class="tw-shrink-0 tw-text-right tw-text-neutral-500 tw-max-w-xs tw-flex tw-justify-end tw-items-center tw-space-x-1"
          )
            span ID: {{ id }}
            MClipboardCopy(:contents="id")

      //- Subtitle.
      p(
        v-if="$slots.subtitle || subtitle"
        :class="['tw-text-sm tw-mb-2', subtitleVariantClasses]"
        data-testid="overviewcard-subtitle"
        )
        slot(name="subtitle") {{ subtitle }}

  //- Body container.
  div(
    :class="['tw-mt-1 tw-mb-5 tw-px-4', bodyVariantClasses]"
    )
    //- Error state.
    MErrorCallout(
      v-if="error"
      :error="error"
      class="tw-shadow"
      )

    //- Loading state.
    div(
      v-else-if="isLoading"
      class="tw-animate-pulse"
      )
      div(class="tw-h-4 tw-w-9/12 tw-mb-2 tw-bg-neutral-200 tw-rounded")
      div(class="tw-h-4 tw-w-7/12 tw-mb-2 tw-bg-neutral-200 tw-rounded")
      div(class="tw-h-4 tw-w-8/12 tw-mb-2 tw-bg-neutral-200 tw-rounded")

    //- Content.
    div(v-else)
      slot

      //- Buttons.
      div(
        v-if="$slots.buttons"
        class="tw-flex tw-flex-col sm:tw-flex-row tw-justify-end tw-flex-wrap tw-mt-4 sm:tw-space-x-2 *:tw-mt-2 *:*:tw-w-full sm:*:*:tw-w-auto"
        )
        slot(name="buttons")

div(
  v-if="$slots.caption"
  class="tw-w-full tw-text-right tw-text-xs tw-text-neutral-500 tw-mt-2 tw-mb-4"
  )
    slot(name="caption")
</template>

<script lang="ts" setup>
import { computed } from 'vue'

import MErrorCallout from '../composits/MErrorCallout.vue'
import MClipboardCopy from '../unstable/MClipboardCopy.vue'
import { DisplayError } from '../utils/DisplayErrorHandler'
import type { Variant } from '../utils/types'

const props = withDefaults(defineProps<{
  /**
   * The title of the card.
   */
  title: string
  /**
   * Optional: Show a loading state.
   */
  isLoading?: boolean
  /**
   * Optional: Show an error. You can directly pass in the `error` property from subscriptions.
   */
  error?: Error | DisplayError
  /**
   * Optional: A subtitle to show below the title.
   */
  subtitle?: string
  /**
   * Optional: The visual variant of the badge. Defaults to `primary`.
   */
  variant?: Variant
  /**
   * Optional content for an avatar icon.
   * @example: http://placekitten.com/256/256
   */
  avatarImageUrl?: string
  /**
   * Optional: An ID string to be show on the card with a copy-to-clipboard button.
   * @example 'Player:ZArvpuPqNL'
   */
  id?: string
}>(), {
  variant: 'primary',
  badgeVariant: 'neutral',
  error: undefined,
  badge: undefined,
  subtitle: undefined,
  avatarImageUrl: undefined,
  id: undefined
})

const emit = defineEmits(['headerClick'])

function onHeaderClick () {
  emit('headerClick')
}

const internalVariant = computed(() => {
  if (props.error) return 'danger'
  else return props.variant
})

const containerVariantClasses = computed(() => {
  const loadingClasses = 'tw-border-neutral-200 tw-bg-neutral-50'

  const classes = {
    primary: 'tw-border-neutral-200 tw-bg-white',
    neutral: 'tw-border-neutral-200 tw-bg-neutral-50',
    success: 'tw-border-green-200 tw-bg-green-100',
    warning: 'tw-border-orange-200 tw-bg-orange-100',
    danger: 'tw-border-red-200 tw-bg-red-200'
  }

  return props.isLoading ? loadingClasses : classes[internalVariant.value]
})

const headerVariantClasses = computed(() => {
  const loadingClasses = 'tw-text-neutral-300'

  const classes = {
    primary: 'tw-text-neutral-800',
    neutral: 'tw-text-neutral-500',
    success: 'tw-text-green-800',
    warning: 'tw-text-orange-800',
    danger: 'tw-text-red-800',
  }

  return props.isLoading ? loadingClasses : classes[internalVariant.value]
})

const subtitleVariantClasses = computed(() => {
  const classes = {
    primary: 'tw-text-neutral-500',
    neutral: 'tw-text-neutral-400',
    success: 'tw-text-green-500',
    warning: 'tw-text-orange-500',
    danger: 'tw-text-red-500',
  }

  return classes[internalVariant.value]
})

const bodyVariantClasses = computed(() => {
  const classes = {
    primary: 'tw-text-neutral-900',
    neutral: 'tw-text-neutral-500',
    success: 'tw-text-green-900',
    warning: 'tw-text-orange-900',
    danger: 'tw-text-red-900',
  }

  return classes[internalVariant.value]
})
</script>
