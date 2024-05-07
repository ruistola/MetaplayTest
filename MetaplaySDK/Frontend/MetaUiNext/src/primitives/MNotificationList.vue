<template lang="pug">
//- Transition container and absolute postioning for the notifications.
TransitionGroup(
  name="notification-list"
  tag="ul"
  class="tw-absolute tw-top-0 tw-right-0 tw-p-1 sm:tw-p-4 tw-z-50 tw-w-full tw-flex sm:tw-block sm:tw-w-auto tw-justify-end tw-overflow-hidden tw-pointer-events-none"
  )
  //- Notifications.
  li(
    role="alert"
    tabindex="0"
    v-for="notification in notificationsToShow"
    :key="notification.id"
    @mouseenter="updateHoverStatus(notification.id, true)"
    @mouseleave="updateHoverStatus(notification.id, false)"
    @touchstart="updateHoverStatus(notification.id, true)"
    @touchend="updateHoverStatus(notification.id, false)"
    :class="['tw-pointer-events-auto tw-rounded-md tw-py-2 tw-px-3 tw-mb-3 tw-shadow tw-flex tw-items-start tw-justify-between tw-space-x-2 tw-border tw-border-opacity-30 tw-w-full sm:tw-w-80 hover:tw-shadow-md hover:-tw-translate-y-0.5 tw-transition-all', getVariantClasses(notification)]"
    )
      //- Notification content.
      div(class="tw-text-sm")
        div(
          role="heading"
          class="tw-font-bold"
          ) {{ notification.title }}
        div {{ notification.message }}

      //- Notification close button.
      MIconButton(
        @click="removeNotification(notification.id)"
        :variant="notification.variant"
        class="tw-shrink-0"
        aria-label="Close notification."
        )
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="tw-w-4 tw-h-4">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
          </svg>

</template>

<script lang="ts" setup>
import { computed, onMounted, onUnmounted } from 'vue'
import { useNotifications, type Notification } from '../composables/useNotifications'
import MIconButton from '../unstable/MIconButton.vue'
const { notificationsToShow, tickNotificationLifetimes, removeNotification, updateHoverStatus } = useNotifications()

/**
 * Utility to track the current page visibility state.
 */
const pageVisible = computed(() => document.visibilityState === 'visible')

// Start the timer to tick the notification lifetimes. 500ms is enough resolution.
let intervalId: ReturnType<typeof setInterval> | undefined
function startTimer () {
  if (intervalId !== undefined) return
  intervalId = setInterval(() => {
    if (pageVisible.value) {
      tickNotificationLifetimes(500)
    }
  }, 500)
}

function stopTimer () {
  if (intervalId === undefined) return
  clearInterval(intervalId)
  intervalId = undefined
}

// Set a timer to tick the notification lifetimes automatically if the browser is in focus.
onMounted(() => {
  if (document.visibilityState === 'visible') {
    startTimer()
  }
})

// Remember to clean up.
onUnmounted(() => {
  stopTimer()
})

// Stop the timer when the browser is not in focus. Resume when it is.
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') {
    startTimer()
  } else {
    stopTimer()
  }
})

function getVariantClasses (notification: Notification) {
  const variantClasses = {
    success: 'tw-bg-green-400 tw-border-green-600 hover:tw-bg-green-300 hover:tw-border-green-400 tw-text-white',
    warning: 'tw-bg-orange-400 tw-border-orange-600 hover:tw-bg-orange-300 hover:tw-border-orange-400 tw-text-white',
    danger: 'tw-bg-red-400 tw-border-red-600 hover:tw-bg-red-300 hover:tw-border-red-400 tw-text-white'
  }

  return variantClasses[notification.variant]
}
</script>

<style>
.notification-list-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}
.notification-list-leave-to {
  opacity: 0;
  transform: translateX(40px);
}
/** Switch leave animation to go up instead of right on small screens. */
@media (max-width: 640px) {
  .notification-list-leave-to {
    transform: translateY(-10px);
  }
}
</style>
