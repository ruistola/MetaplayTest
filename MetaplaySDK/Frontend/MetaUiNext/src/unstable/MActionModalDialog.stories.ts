import { ref } from 'vue'
import MActionModalDialog from './MActionModalDialog.vue'
import MButton from '../unstable/MButton.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MActionModalDialog> = {
  component: MActionModalDialog,
  // tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof MActionModalDialog>

export const OneColumn: Story = {
  render: (args) => ({
    components: { MActionModalDialog, MButton },
    setup: () => {
      const modalRef = ref<typeof MActionModalDialog>()
      const debugStatus = ref('')
      async function action () {
        await new Promise(resolve => setTimeout(resolve, 2000))
        debugStatus.value = 'done'
      }
      return { modalRef, debugStatus, action, args }
    },
    template: `
    <MButton v-if="modalRef" @click="modalRef.open()"> Show Modal</MButton>
    <p>{{ debugStatus }}</p>
    <MActionModalDialog v-bind="args" :action="action" ref="modalRef">
      <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
    </MActionModalDialog>
    `,
  }),
  args: {
    title: 'Fairly Normal Length Title'
  },
}

// TODO: Come up with all the stories. Variants, error states, etc.

// export const TwoColumns: Story = {
//   render: (args) => ({
//     components: { MActionModalDialog },
//     setup: () => ({ args }),
//     template: `
//     <MActionModalDialog v-bind="args">
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       <template #right-panel>
//         <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       </template>
//     </MActionModalDialog>
//     `,
//   }),
//   args: {
//     title: 'Fairly Normal Length Title'
//   },
// }

// export const LongContent: Story = {
//   render: (args) => ({
//     components: { MActionModalDialog },
//     setup: () => ({ args }),
//     template: `
//     <MActionModalDialog v-bind="args">
//       <h3>H3 Header 1</h3>
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       <h3>H3 Header 2</h3>
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       <h3>H3 Header 3</h3>
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       <h3>H3 Header 4</h3>
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       <h3>H3 Header 5</h3>
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       <h3>H3 Header 6</h3>
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       <h3>H3 Header 7</h3>
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//       <h3>H3 Header 8</h3>
//       <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed euismod, nisl nec ultricies aliquam, nisl nisl aliquet nisl, eget aliquet nisl.</p>
//     </MActionModalDialog>
//     `,
//   }),
//   args: {
//     title: 'Long Title That Will Cause Overflow In The Header Area And Will Not Be Truncated With Ellipsis',
//   },
// }

// export const Overflow: Story = {
//   render: (args) => ({
//     components: { MActionModalDialog },
//     setup: () => ({ args }),
//     template: `
//     <MActionModalDialog v-bind="args">
//       <h3>H3 Header</h3>
//       <pre>
// export const TwoColumns: Story = {
//   render: (args) => ({
//     components: { MActionModalDialog },
//     setup: () => ({ args }),
//     template: 'Very long template string that will cause overflow in the modal content area and will make the body scrollable.',
//   }),
//   args: {},
// }
//       </pre>
//     </MActionModalDialog>
//     `,
//   }),
//   args: {
//     title: 'LongTitleThatWillCauseOverflowInTheHeaderAreaAndWillBeTruncatedWithEllipsis',
//   },
// }
