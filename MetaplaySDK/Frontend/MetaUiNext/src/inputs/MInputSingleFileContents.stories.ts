import MInputSingleFileContents from './MInputSingleFileContents.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MInputSingleFileContents> = {
  component: MInputSingleFileContents,
  tags: ['autodocs'],
  render: (args) => ({
    components: { MInputSingleFileContents },
    setup: () => ({ args }),
    data: () => ({ content: args.modelValue }),
    template: `<div>
      <MInputSingleFileContents v-bind="args" v-model="content"/>
      <pre class="tw-mt-2">Output: {{ content?.toString() }}</pre>
    </div>`,
  }),
}

export default meta
type Story = StoryObj<typeof MInputSingleFileContents>

export const Default: Story = {
  args: {
    label: 'File',
    hintMessage: 'You can select one file at a time.',
  },
}

export const Placeholder: Story = {
  args: {
    label: 'File',
    placeholder: 'Select a file',
  },
}

export const Disabled: Story = {
  args: {
    label: 'File',
    disabled: true,
  },
}

export const Loading: Story = {
  args: {
    label: 'File',
    variant: 'loading',
  },
}

export const Danger: Story = {
  args: {
    label: 'File',
    variant: 'danger',
    hintMessage: 'Hints turn red when the variant is danger.',
  },
}

export const Success: Story = {
  args: {
    label: 'File',
    variant: 'success',
    hintMessage: 'Hints are still neutral when the variant is success.',
  },
}

export const NoLabel: Story = {
  args: {},
}
