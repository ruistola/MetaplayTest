import { ref } from 'vue'
import MActionModalButton from './MActionModalButton.vue'
import MActionModal from './MActionModal.vue'
import { usePermissions } from '../composables/usePermissions'

import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MActionModalButton> = {
  component: MActionModalButton,
  tags: ['autodocs'],
  parameters: {
    docs: {
      description: {
        component: 'MActionModalButton is a button that opens a modal that contains a title, a body, and a footer with an action button. It is intended to be used for actions that require user confirmation, such as deleting an item. TODO: write better description here!',
      },
    },
  },
}

export default meta
type Story = StoryObj<typeof MActionModalButton>

export const OneColumn: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { debugStatus, action, args }
    },
    template: `
    <MActionModalButton v-bind="args" :action="action">
    <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalButton>
    <p>{{ debugStatus }}</p>
    `,
  }),
  args: {
    triggerButtonLabel: 'Action button text',
    modalTitle: 'Fairly Normal Length Title',
  },
}

/**
 * Two column story.
 */
export const TwoColumn: Story = {
  render: (args) => ({
    components: { MActionModalButton, MActionModal },
    setup: () => {
      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { debugStatus, action, args }
    },
    template: `
    <MActionModalButton v-bind="args" :action="action">
      <template #right-panel>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </template>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalButton>
    <p>{{ debugStatus }}</p>
    `,
  }),
  args: {
    triggerButtonLabel: 'Two column modal',
    modalTitle: 'Two column example modal',
  },
}

/**
 * Three column story
 */
export const ThreeColumn: Story = {
  render: (args) => ({
    components: { MActionModalButton, MActionModal },
    setup: () => {
      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { debugStatus, action, args }
    },
    template: `
    <MActionModalButton v-bind="args" :action="action">
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      <template #right-panel>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </template>
       <template #bottom-panel>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
      </template>
    </MActionModalButton>
    <p>{{ debugStatus }}</p>
    `,
  }),
  args: {
    triggerButtonLabel: 'Three column modal',
    modalTitle: 'Three column example modal',
  },
}

/**
 * Action button with permissions.
 */
export const Permissions: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      const debugStatus = ref('')
      usePermissions().setPermissions(['example-permission'])
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { debugStatus, action, args }
    },
    template: `
    <MActionModalButton v-bind="args" :action="action" ref="modalRef">
    <p>{{ debugStatus }}</p>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'Permissions enabled modal',
    triggerButtonDisabled: 'Sorry you do not have the required permissions to perform this action.',
    modalTitle: 'Permissions enabled example modal',
    permission: 'example-permission',
  },
}

/**
 * Disabled action button.
 */
export const DisabledActionButton: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      return { args }
    },
    template: `
    <MActionModalButton v-bind="args" :action="action" ref="modalRef">
    <p>{{ debugStatus }}</p>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'Disabled action button modal',
    triggerButtonDisabled: true,
    modalTitle: 'Action button disabled example modal',
  },
}

/**
 * Disabled action button with a tooltip.
 */
export const DisabledActionButtonWithTooltip: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      return { args }
    },
    template: `
    <MActionModalButton v-bind="args" :action="action" ref="modalRef">
    <p>{{ debugStatus }}</p>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'Disabled action button modal',
    triggerButtonDisabled: 'You cannot perform this action at this time.',
    modalTitle: 'Action button disabled example modal',
  },
}
/**
 * Disabled ok button.
 */
export const DisabledOkButton: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      return { args }
    },
    template: `
     <MActionModalButton v-bind="args" :action="action" ref="modalRef">
    <p>{{ debugStatus }}</p>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'Disabled ok button modal',
    okButtonDisabled: true,
    modalTitle: 'Ok button disabled example modal',
  },
}

/**
 * Disabled ok button with a tooltip.
 */
export const DisabledOkButtonWithTooltip: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      return { args }
    },
    template: `
     <MActionModalButton v-bind="args" :action="action" ref="modalRef">
    <p>{{ debugStatus }}</p>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'Disabled ok button modal',
    okButtonDisabled: 'You cannot perform this action.',
    modalTitle: 'Ok button disabled example modal',
  },
}

/**
 * Ok Action returns an error.
 */
export const ActionError: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      const modalRef = ref<typeof MActionModalButton>()
      const debugStatus = ref('')
      async function action () {
        throw new Error('Something bad happened!')
      }
      return { modalRef, debugStatus, action, args }
    },
    template: `
    <MActionModalButton v-bind="args" :action="action" ref="modalRef">
    <p>{{ debugStatus }}</p>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'Action error modal',
    modalTitle: 'Action error example modal',
  },
}

/**
 * Button labels.
 */
export const ActionAndOKButtonLabel: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      return { args }
    },
    template: `
     <MActionModalButton v-bind="args" :action="action" ref="modalRef">
    <p>{{ debugStatus }}</p>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'New action button label',
    okButtonLabel: 'New ok button label',
    modalTitle: 'Modified button labels example modal',
  },
}

/**
 * View-only modal used for modals where users can see information or content but cannot interact with or modify it.
 */
export const ViewOnlyModal: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      return { args }
    },
    template: `
     <MActionModalButton v-bind="args" :action="action" ref="modalRef">
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'View only modal',
    modalTitle: 'View only example modal',
    onlyClose: true,
  },
}

/**
 * Action button and OK button overflows.
 */
export const ButtonLabelOverflows: Story = {
  render: (args) => ({
    components: { MActionModalButton },
    setup: () => {
      return { args }
    },
    template: `
     <MActionModalButton v-bind="args" :action="action" ref="modalRef">
    <p>{{ debugStatus }}</p>
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalModalButton>
    `,
  }),
  args: {
    triggerButtonLabel: 'This is a really long label for the Action button text',
    okButtonLabel: 'This is a really long label for the Ok button text',
    modalTitle: 'Modal Button label overflows',
  },
}

// TODO: Come up with all the stories. Variants, error states, permissions, etc.
