<template lang="pug">
//- TODO: Transition and responsive design.
//- Teleporting dialog to body is necessary to avoid inheriting styles from the parent component.
Teleport(to="body")
  Transition(name="backdrop-fade")
    div(v-bind="api.backdropProps" class="tw-fixed tw-inset-0 h-dvh")
  Transition(name="modal-fade")
    div(v-if="api.isOpen" class="tw-fixed tw-inset-0 h-dvh tw-overflow-y-auto")
      div(v-bind="api.positionerProps" class="tw-relative")
        //- TODO: Why does the size not animate?
        div(
          v-bind="api.contentProps"
          :class="{ 'sm:tw-max-w-3xl': $slots['right-panel'], 'sm:tw-max-w-lg': !$slots['right-panel'] }"
          class="tw-mx-auto tw-overflow-x-hidden tw-overflow-y-visible tw-rounded-lg tw-bg-white tw-shadow-xl sm:tw-my-8 sm:tw-w-full tw-p-4 sm:tw-px-6 sm:tw-py-5 tw-transition-transform tw-duration-1000"
          )
          //- Header.
          div(v-bind="api.titleProps" class="tw-flex tw-justify-between tw-mb-2")
            span(role="heading" class="tw-font-bold tw-text-neutral-900 tw-overflow-ellipsis tw-overflow-hidden") {{ title }}

            button(
              @click="close"
              class="tw-shrink-0 tw-inline-flex tw-justify-center tw-items-center tw-w-7 tw-h-7 tw-rounded hover:tw-bg-neutral-100 active:tw-bg-neutral-200 tw-relative -tw-top-0.5 tw-font-semibold"
            )
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="tw-w-6 tw-h-6">
                <path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 011.06 0L12 10.94l5.47-5.47a.75.75 0 111.06 1.06L13.06 12l5.47 5.47a.75.75 0 11-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 01-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 010-1.06z" clip-rule="evenodd" />
              </svg>

          //- Loading and error states.
          div(
            v-if="visualState === 'loading'"
            class="tw-flex tw-justify-center tw-my-14 tw-text-neutral-500 tw-animate-bounce tw-italic"
            ) Loading, please hold...

          div(v-else-if="visualState === 'error'")
            p Oh snap, something went wrong while trying to perform the action. Here's what we know:
            MErrorCallout(
              v-if="okPromiseError"
              :error="okPromiseError"
              class="tw-mt-4"
              )
            div(class="tw-flex tw-justify-end")
              MButton(
                @click="close"
                variant="neutral"
                class="tw-mt-4"
                ) Close

          div(
            v-else-if="visualState === 'success'"
            class="tw-flex tw-justify-center tw-my-14 tw-text-neutral-500 tw-italic"
            ) Success!

          //- Default state.
          div(v-else)
            //- Body.
            div(class="sm:tw-flex sm:tw-space-x-2")
              //- Left panel.
              div(class="sm:tw-flex-1 tw-overflow-x-auto")
                slot Default modal content goes here...

              //- Right panel (optional).
              div(v-if="$slots['right-panel']" class="sm:tw-flex-1 tw-overflow-x-auto")
                slot(name="right-panel")

            //- Buttons.
            div(class="tw-flex tw-flex-col sm:tw-flex-row tw-justify-end tw-mt-5 tw-space-y-2 sm:tw-space-y-0 sm:tw-space-x-2")
              MButton(
                variant="neutral"
                @click="cancel"
                class="tw-w-full sm:tw-w-auto"
                ) Cancel
              MButton(
                :variant="variant"
                :disabled="okButtonDisabled"
                :disabled-tooltip="okButtonDisabledTooltip"
                @click="ok"
                class="tw-w-full sm:tw-w-auto"
                ) {{ okButtonLabel }}
</template>

<script setup lang="ts">
import * as dialog from '@zag-js/dialog'
import { normalizeProps, useMachine } from '@zag-js/vue'
import { computed, Teleport, ref } from 'vue'
import MButton from '../unstable/MButton.vue'
import type { Variant } from '../utils/types'
import { makeIntoUniqueKey } from '../utils/generalUtils'
import MErrorCallout from '../composits/MErrorCallout.vue'

const props = withDefaults(defineProps<{
  /**
       * The title of the modal.
       */
  title: string
  /**
       * The action to perform when the user clicks the OK button. A loading screen is shown while the async action is
       * pending. If the action throws an error, the error is shown instead.
       */
  action: () => Promise<void>
  /**
       * Optional: The visual variant of the modal. Currently only affects the color of the OK button. Defaults to "primary".
       */
  variant?: Variant
  /**
       * Optional: The label of the OK button. Defaults to "Ok".
       */
  okButtonLabel?: string
  /**
       * Optional: Whether the OK button should be disabled. Defaults to false.
       */
  okButtonDisabled?: boolean
  /**
       * Optional: The tooltip to show when the OK button is disabled. Also disables the button. Defaults to undefined.
       */
  okButtonDisabledTooltip?: string
}>(), {
  variant: 'primary',
  okButtonLabel: 'Ok',
  okButtonDisabledTooltip: undefined,
})

const okPromiseTriggered = ref(false)

const visualState = computed(() => {
  if (okPromisePending.value) {
    return 'loading'
  } else if (okPromiseError.value) {
    return 'error'
  } else if (okPromiseTriggered.value) {
    return 'success'
  } else {
    return 'default'
  }
})

// Visibility controls ------------------------------------------------------------------------------------------------

const emit = defineEmits(['ok', 'cancel', 'show', 'hide'])

function open () {
  okPromisePending.value = false
  okPromiseError.value = undefined
  okPromiseTriggered.value = false
  api.value.open()
  emit('show')
}
function close () {
  api.value.close()
  emit('hide')
}
defineExpose({
  open,
  close,
})

// Actions ------------------------------------------------------------------------------------------------------------

const okPromisePending = ref(false)
const okPromiseError = ref<Error>()
async function ok () {
  okPromisePending.value = true
  try {
    okPromiseTriggered.value = true
    await props.action()
    okPromisePending.value = false
    emit('ok')
    close()
  } catch (error) {
    okPromisePending.value = false
    okPromiseError.value = error as Error
    console.error(error)
  }
}

function cancel () {
  emit('cancel')
  close()
}

// Zag ----------------------------------------------------------------------------------------------------------------

const [state, send] = useMachine(dialog.machine(
  {
    id: makeIntoUniqueKey('modal'),
    closeOnInteractOutside: false,
  }
))

const api = computed(() => dialog.connect(state.value, send, normalizeProps))
</script>

    <style>
    [data-part="backdrop"][data-state="open"] {
      background: rgba(107, 114, 128, 0.45);
    }

    [data-part="backdrop"] {
      background-color: rgb(0 0 0 / 0);
    }

    .modal-fade-enter-active,
    .modal-fade-leave-active {
      transition: opacity 0.2s ease-out, transform 0.2s ease-out;
    }

    .modal-fade-enter-from,
    .modal-fade-leave-to {
      opacity: 0;
      transform: translateY(-1rem);
    }

    .backdrop-fade-enter-active,
    .backdrop-fade-leave-active {
      transition: opacity 0.2s;
    }

    .backdrop-fade-enter-from,
    .backdrop-fade-leave-to {
      opacity: 0;
    }
    </style>
