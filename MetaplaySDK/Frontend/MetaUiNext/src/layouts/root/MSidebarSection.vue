<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div
  div(
    role="heading"
    class="tw-px-4 tw-font-bold tw-mb-1"
    :class="{'tw-text-neutral-400': disabled, 'tw-text-neural-700': !disabled}"
    v-bind="$attrs"
    ) {{ title }}
  ul(class="tw-divide-y tw-divide-neutral-200")
    slot
</template>

<script lang="ts" setup>
defineProps<{
  title: string
  disabled?: boolean
}>()

defineOptions({
  inheritAttrs: false
})
</script>
