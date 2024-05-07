<template lang="pug">
//- TODO: Transition and responsive design.
//- Teleporting dialog to body is necessary to avoid inheriting styles from the parent component.
Teleport(to="body")
  dialog(
    ref="dialogElement"
    class="tw-relative tw-transform tw-overflow-x-hidden tw-overflow-y-visible tw-rounded-lg tw-bg-white tw-shadow-xl sm:tw-my-8 sm:tw-w-full sm:tw-max-w-lg tw-p-4 sm:tw-px-6 sm:tw-py-5"
    v-bind="$attrs"
    )
    //- Loading and error states.
    div(v-if="okPromisePending")
      //- TODO: A nicer loading state.
      div(class="tw-flex tw-justify-center tw-my-14 tw-italic tw-text-neutral-500") Doing the thing...
      div(v-if="okPromiseError")
        //- TODO: A nicer error state.
        div(class="tw-flex tw-justify-center tw-text-red-500") Error: {{ okPromiseError.message }}
        MButton(
          @click="close"
          variant="danger"
          class="tw-mt-4 tw-w-full"
          ) Close

    //- Content.
    div(v-else)
      //- Title
      div(class="tw-flex tw-justify-between tw-mb-2")
        span(role="heading" class="tw-font-bold tw-text-neutral-900 tw-overflow-ellipsis tw-overflow-hidden") {{ title }}
        //- TODO: Nicer X button.
        button(
          @click="close"
          class="tw-shrink-0 tw-inline-flex tw-justify-center tw-items-center tw-w-7 tw-h-7 tw-rounded hover:tw-bg-neutral-100 active:tw-bg-neutral-200 tw-relative -tw-top-0.5 tw-font-semibold"
        ) X

      //- Body
      div(class="sm:tw-flex sm:tw-space-x-4")
        //- Left panel
        div(class="sm:tw-flex-1 tw-overflow-x-auto")
          slot Default modal content goes here...

        //- Right panel (optional)
        div(v-if="$slots['right-panel']" class="sm:tw-flex-1 tw-overflow-x-auto")
          slot(name="right-panel")

      div(class="tw-flex tw-flex-col sm:tw-flex-row tw-justify-end tw-mt-5 tw-space-y-2 sm:tw-space-y-0 sm:tw-space-x-2")
        MButton(
          variant="neutral"
          @click="cancel"
          ) Cancel
        MButton(
          :variant="variant"
          :disabled="okButtonDisabled"
          @click="ok"
          ) {{ okButtonLabel }}
</template>

<script setup lang="ts">
import { ref } from 'vue'
import MButton from '../unstable/MButton.vue'
import type { Variant } from '../utils/types'

// TODO: Modal size? Or could we get away with one size?
const props = withDefaults(defineProps<{
  title: string
  action: () => Promise<void>
  variant?: Variant
  okButtonLabel?: string
  okButtonDisabled?: boolean
}>(), {
  variant: 'primary',
  okButtonLabel: 'Ok',
})

const dialogElement = ref<HTMLDialogElement>()

// Visibility controls ------------------------------------------------------------------------------------------------

// TODO: Invent a way to invoke these from a global scope. Maybe a composable that has a registry of available modals?

const emit = defineEmits(['ok', 'cancel', 'show', 'hide'])

const isOpen = ref(false)
function open () {
  isOpen.value = true
  okPromisePending.value = false
  dialogElement.value?.showModal()
  emit('show')
}
function close () {
  isOpen.value = false
  okPromisePending.value = false
  dialogElement.value?.close()
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
    await props.action()
    emit('ok')
    close()
  } catch (error) {
    okPromiseError.value = error as Error
    console.error(error)
  }
}

function cancel () {
  emit('cancel')
  close()
}
</script>

<style>
/*   Open state of the dialog  */
dialog[open] {
  opacity: 1;
  transform: translateY(0);
}

/*   Closed state of the dialog   */
dialog {
  opacity: 0;
  transform: translateY(-1rem);
  transition:
    opacity 0.2s ease-out,
    transform 0.2s ease-out,
    overlay 0.2s ease-out allow-discrete,
    display 0.2s ease-out allow-discrete;
}

/*   Before-open state  */
/* Needs to be after the previous dialog[open] rule to take effect, as the specificity is the same */
@starting-style {
  dialog[open] {
    opacity: 0;
    transform: translateY(-1rem);
  }
}

/* Transition the :backdrop when the dialog modal is promoted to the top layer */
dialog::backdrop {
  background-color: rgb(0 0 0 / 0);
  transition:
    display 0.2s allow-discrete,
    overlay 0.2s allow-discrete,
    background-color 0.2s;
}

dialog[open]::backdrop {
  background: rgba(107, 114, 128, 0.45);
}

/* This starting-style rule cannot be nested inside the above selector because the nesting selector cannot represent pseudo-elements. */
@starting-style {
  dialog[open]::backdrop {
    background-color: rgb(0 0 0 / 0);
  }
}
</style>
