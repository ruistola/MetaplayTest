<template lang="pug">
//- Container
li(
  :class="['tw-block tw-py-3 tw-pr-5', { 'hover:tw-bg-neutral-200 active:tw-bg-neutral-300 tw-cursor-pointer': clickable, 'even:tw-bg-neutral-100': striped, 'tw-pl-5': !noLeftPadding, '!tw-py-2.5': condensed }]"
  )
  //- List item container
  div(class="tw-flex tw-gap-x-1")
    //- Optional avatar
    img(
      v-if="avatarUrl"
      :src="avatarUrl"
      class="tw-h-10 tw-w-10 tw-rounded-full"
    )
    div(class="tw-w-full tw-space-y-0.5")
      //- Top row
      div(class="tw-flex tw-justify-between tw-items-baseline")
        //- Left
        span(class="tw-flex tw-flex-wrap tw-gap-1 tw-text-sm+ tw-overflow-hidden tw-text-ellipsis")
          span(
            role="heading"
            :class="['tw-max-w-lg', { 'tw-font-semibold': !condensed, 'tw-text-xs+': condensed } ]"
            )
            slot(name="default")

          span(class="tw-flex tw-gap-x-1")
            slot(name="badge")

        //- Right
        span(:class="['tw-flex-none tw-max-w-xs tw-break-all tw-gap-1 tw-text-right tw-text-sm tw-space-x-1', { 'tw-text-xs+': condensed }]")
          slot(name="top-right")

      //- Bottom row
      //- Custom max breakpoint below needs some fine tuning. Storybook not as working as expected.
      div(class="tw-flex tw-gap-1 tw-justify-between tw-text-neutral-500 tw-text-xs+")
        //- Left
        span(class="tw-flex-grow tw-flex-wrap tw-overflow-x-auto")
          slot(name="bottom-left")

        //- Right
        span(class="tw-flex-none tw-max-w-sm tw-break-all tw-text-right tw-flex-wrap")
          slot(name="bottom-right")
</template>

<script lang="ts" setup>
// - TODO: make prop for condensed for top left title to be more like generated ui dictionary.
const props = withDefaults(defineProps<{
  /**
   * Optional: If true, the list item will be clickable.
   */
  clickable?: boolean
  /**
   * Optional: The URL of the avatar image.
   */
  avatarUrl?: string
  /**
   * Optional: If true, the list item will be striped.
   */
  striped?: boolean
  /**
   * Optional: Remove the default left padding from the ListItem. Good for making collapsible lists.
   */
  noLeftPadding?: boolean
  /**
   * Optional: If true, the list item will be condensed.
   */
  condensed?: boolean
}>(), {
  avatarUrl: undefined,
  condensed: false
})
</script>
