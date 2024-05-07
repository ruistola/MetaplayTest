import { ref } from 'vue'
import MActionModal from './MActionModal.vue'
import MButton from '../unstable/MButton.vue'
import MIconButton from '../unstable/MIconButton.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MActionModal> = {
  component: MActionModal,
  tags: ['autodocs'],
  parameters: {
    docs: {
      description: {
        component: 'MActionModal is a modal that is tied to a actuation button. It contains a title, a body, and a footer. It is intended to be used for actions that require user confirmation, such as deleting an item.',
      },
    },
  },
}

export default meta
type Story = StoryObj<typeof MActionModal>

/**
 * The `MActionModal` is an effective way to capture user's attention for actions that require
 * additional confirmation or input from the user before they run. For example deleting an
 * item or submitting a form. To create a `MActionModal`, you need to provide `title`,
 * `action`, and the content to be shown on the modal.
 */
export const SimpleModal: Story = {
  render: (args) => ({
    components: { MActionModal, MButton },
    setup: () => {
      const modalRef = ref<typeof MActionModal>()
      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { modalRef, debugStatus, action, args }
    },
    template: `
    <MButton v-if="modalRef" @click="modalRef.open()">Show Modal</MButton>
    <p>{{ debugStatus }}</p>
    <MActionModal v-bind="args" :action="action" ref="modalRef">
      <p>Are you sure you want to do this?</p>
    </MActionModal>
    `,
  }),
  args: {
    title: 'This action has a custom success view',
  },
}

/**
 * The `action` property takes in a function that returns a promise. Once executed the
 * modal will automatically display the appropriate state based on the promise's state ie:
 * loading, success, or error.
 */
export const ActionStateLoading: Story = {
  render: (args) => ({
    components: { MActionModal, MButton },
    setup: () => {
      const modalRef = ref<typeof MActionModal>()
      const modalRef2 = ref<typeof MActionModal>()
      const modalRef3 = ref<typeof MActionModal>()

      const debugStatus = ref('')
      async function action () {
        await new Promise(() => {
          // No resolution or rejection here
        })
      }
      async function actionSuccess () {
        await new Promise(resolve => setTimeout(resolve, 2000))
      }
      async function actionError () {
        throw new Error('Something bad happened!')
      }
      return { modalRef, modalRef2, modalRef3, debugStatus, action, actionSuccess, actionError, args }
    },
    template: `
    <div class="tw-flex tw-gap-x-2">
      <MButton v-if="modalRef" @click="modalRef.open()">Loading State Modal</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef" title="This action will never resolve">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>

      <MButton v-if="modalRef2" @click="modalRef2.open()">Success State Modal</MButton>
        <p>{{ debugStatus }}</p>
        <MActionModal v-bind="args" :action="actionSuccess" ref="modalRef2" title="This action will succeed">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>

      <MButton v-if="modalRef3" @click="modalRef3.open()">Error State Modal</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="actionError" ref="modalRef3" title="This action will fail">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>
    </div>
    `,
  }),
  args: {
  },
}

/**
 * Add content to the `result-panel` slot to display a custom message or display the
 * results of an action after a promise has been resolved. This will override the default
 * behaviour of the MActionModal component preventing it from automatically close.
 */
export const ActionSuccessCustomContent: Story = {
  render: (args) => ({
    components: { MActionModal, MButton },
    setup: () => {
      const modalRef = ref<typeof MActionModal>()
      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { modalRef, debugStatus, action, args }
    },
    template: `
    <MButton v-if="modalRef" @click="modalRef.open()">Modal with custom resutls</MButton>
    <p>{{ debugStatus }}</p>
    <MActionModal v-bind="args" :action="action" ref="modalRef">
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      <template #result-panel>
        <p>Custom success state content.</p>
      </template>
    </MActionModal>
    `,
  }),
  args: {
    title: 'This action has a custom success view',
  },
}

/**
 * The `MButton` and/or `MIconButton` components can be used as triggers for the `MActionModal`.
 *
 */
export const CustomTriggerButton: Story = {
  render: (args) => ({
    components: { MActionModal, MButton, MIconButton },
    setup: () => {
      const modalRef = ref<typeof MActionModal>()
      const modalRef2 = ref<typeof MActionModal>()
      const modalRef3 = ref<typeof MActionModal>()

      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { modalRef, modalRef2, modalRef3, debugStatus, action, args }
    },
    template: `
    <div class="tw-flex tw-gap-x-2">
      <MButton v-if="modalRef" @click="modalRef.open()">Default trigger Button</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef">
        <p>Are you sure you want to do this?</p>
      </MActionModal>

      <MButton v-if="modalRef2" @click="modalRef2.open()">
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="tw-w-4 tw-h-4">
            <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.06 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75
            0 10-1.061 1.06l1.06 1.06z" />
          </svg>
        </template>
        Trigger button with an icon
      </MButton>
        <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef2">
        <p>Are you sure you want to do this?</p>
      </MActionModal>

      <MIconButton v-if="modalRef3" @click="modalRef3.open()">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="tw-w-4 tw-h-4">
          <path d="M10 2a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 2zM10 15a.75.75 0 01.75.75v1.5a.75.75 0 01-1.5 0v-1.5A.75.75 0 0110 15zM10 7a3 3 0 100 6 3 3 0 000-6zM15.657 5.404a.75.75 0 10-1.06-1.06l-1.061 1.06a.75.75 0 001.06 1.06l1.06-1.06zM6.464 14.596a.75.75 0 10-1.06-1.06l-1.06 1.06a.75.75 0 001.06 1.06l1.06-1.06zM18 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 0118 10zM5 10a.75.75 0 01-.75.75h-1.5a.75.75 0 010-1.5h1.5A.75.75 0 015 10zM14.596 15.657a.75.75 0 001.06-1.06l-1.06-1.061a.75.75 0 10-1.06 1.06l1.06 1.06zM5.404 6.464a.75.75 0 001.06-1.06l-1.06-1.06a.75.75
          0 10-1.061 1.06l1.06 1.06z" />
        </svg>
      </MIconButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef3">
        <p>Are you sure you want to do this?</p>
      </MActionModal>
    </div>
    `,
  }),
  args: {
    title: 'This action will succeed',
  },
}

/**
 * The MActionModal comes with three main slot areas where you can add content to the modal body.
 * The `default` slot is the primary content area of the modal. If no other slots are provided,
 * all content is placed in this slot in a single column layout.
 *
 * Use `right-panel` slot to create a two-column layout, positioning content to the right of the default slot.
 * Similarly, use the `bottom-panel` slot to create a three-column layout, adding content below the two slots.
 * Note that both additional slots require content in the `default` slot to function properly.
 */
export const ModalContentLayout: Story = {
  render: (args) => ({
    components: { MActionModal, MButton },
    setup: () => {
      const modalRef = ref<typeof MActionModal>()
      const modalRef2 = ref<typeof MActionModal>()
      const modalRef3 = ref<typeof MActionModal>()
      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { modalRef, modalRef2, modalRef3, debugStatus, action, args }
    },
    template: `
    <div class="tw-flex tw-gap-x-2">
      <MButton v-if="modalRef" @click="modalRef.open()">Single Column Modal Layout</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef" title="Single column modal layout">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>

      <MButton v-if="modalRef2" @click="modalRef2.open()">Two Column Modal Layout</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef2" title="Two column modal layout">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <template #right-panel>
          <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        </template>
      </MActionModal>

      <MButton v-if="modalRef3" @click="modalRef3.open()">Three Column Modal Layout</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef3" title="Three column modal layout">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <template #right-panel>
          <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        </template>
        <template #bottom-panel>
          <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        </template>
      </MActionModal>
    </div>
    `,
  }),
}

/**
 * The `MActionModal` component is designed to gracefully manage lengthy and detailed content.
 * Both the title and body section content will seamlessly adjust their heights accordingly to
 * accommodate their respective content.
 *
 * For wide content, the body section will overflow automatically enabling horizontal scrolling
 * along the x-axis..
 */
export const ModalContentOverflows: Story = {
  render: (args) => ({
    components: { MActionModal, MButton },
    setup: () => {
      const modalRef = ref<typeof MActionModal>()
      const modalRef2 = ref<typeof MActionModal>()

      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { modalRef, modalRef2, debugStatus, action, args }
    },
    template: `
    <div class="tw-flex tw-gap-x-2">
      <MButton v-if="modalRef" @click="modalRef.open()">Long Content Modal</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef">
        <h3>H3 Header 1</h3>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <h3>H3 Header 2</h3>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <h3>H3 Header 3</h3>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <h3>H3 Header 4</h3>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <h3>H3 Header 5</h3>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <h3>H3 Header 6</h3>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <h3>H3 Header 7</h3>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
        <h3>H3 Header 8</h3>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>

      <MButton v-if="modalRef2" @click="modalRef2.open()">Wide Content Modal</MButton>
        <p>{{ debugStatus }}</p>
        <MActionModal v-bind="args" :action="action" ref="modalRef2">
          <h3>H3 Header</h3>
          <pre>
            export const TwoColumns: Story = {
              render: (args) => ({
                components: { MActionModal },
                setup: () => ({ args }),
                template: 'Very long template string that will cause overflow in the modal content area and will make the body scrollable.',
              }),
              args: {},
            }
        </pre>
      </MActionModal>
    </div>
    `,
  }),
  args: {
    title: 'Long Title That Will Cause Overflow In The Header Area And Will Not Be Truncated With Ellipsis',
  },
}

/**
 * Disable the OK button by setting the `okButtonDisabled` prop to `true`.
 * For example, you may want to disable the OK button until the user has completed a form.
 */
export const OkButtonDisabled: Story = {
  render: (args: any) => ({
    components: { MActionModal, MButton },
    setup: () => {
      const modalRef = ref<typeof MActionModal>()
      const modalRef2 = ref<typeof MActionModal>()
      const args = {}

      const debugStatus = ref<string>('')
      async function action (): Promise<void> {
        await new Promise<void>(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { modalRef, modalRef2, debugStatus, action, args }
    },
    template: `
    <div class="tw-flex tw-gap-x-2">
      <MButton v-if="modalRef" @click="modalRef.open()">Ok Button Disabled</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef" okButtonDisabled>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>

      <MButton v-if="modalRef2" @click="modalRef2.open()">OK Button Disabled with Tooltip</MButton>
      <p>{{ debugStatus }}</p>
      <MActionModal v-bind="args" :action="action" ref="modalRef2" okButtonDisabled="I cant do it Dave.">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>
    </div>
    `,
  }),
  args: {
    title: 'The OK button is disabled',
  },
}

/**
 * The MActionModal's Ok button has a safety lock that prevents accidental triggering of the action.
 * This feature adds an extra layer of security, requiring users to 'unlock' an action before they can trigger it.
 * To disable this feature, set the `disableSafetyLock` prop to `true`.
 */
export const OkButtonSafetyLock: Story = {
  render: (args) => ({
    components: { MActionModal, MButton },
    setup: () => {
      const modalRef = ref<typeof MActionModal>()
      const modalRef2 = ref<typeof MActionModal>()

      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { modalRef, modalRef2, debugStatus, action, args }
    },
    template: `
    <div class="tw-flex tw-gap-x-2">
      <MButton v-if="modalRef" @click="modalRef.open()">Ok Button with Safety Lock</MButton>
      <MActionModal v-bind="args" :action="action" ref="modalRef">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>

      <MButton v-if="modalRef2" @click="modalRef2.open()">Ok Button with Safety Lock Disabled</MButton>
      <MActionModal v-bind="args" :action="action" ref="modalRef2" disableSafetyLock>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </MActionModal>
    </div>
    `,
  }),
}
