import MInputSingleFile, { type FileError } from './MInputSingleFile.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MInputSingleFile> = {
  component: MInputSingleFile,
  tags: ['autodocs'],
  render: (args) => ({
    components: { MInputSingleFile },
    setup: () => ({ args }),
    data: () => ({ content: args.modelValue }),
    template: `<div>
      <MInputSingleFile v-bind="args" v-model="content"/>
      <pre class="tw-mt-2">Output: {{ content?.toString() }}</pre>
    </div>`,
  }),
}

export default meta
type Story = StoryObj<typeof MInputSingleFile>

export const Default: Story = {
  args: {
    label: 'File',
    hintMessage: 'You can select one file at a time.',
  },
}

export const LimitedFileType: Story = {
  args: {
    label: 'Only JSON Files',
    acceptedFileTypes: '.json',
  },
}

export const FileValidation: Story = {
  args: {
    label: 'JSON File Smaller Than 2KB',
    validationFunction: (file: File) => {
      const errors: FileError[] = []
      if (file.size > 2048) errors.push('FILE_TOO_LARGE')
      if (file.type !== 'application/json') errors.push('FILE_INVALID_TYPE')
      return errors
    },
  },
}

export const SmallFilesOnly: Story = {
  args: {
    label: 'File Smaller Than 2KB',
    maxFileSize: 2048,
  },
}

export const LargeFilesOnly: Story = {
  args: {
    label: 'File Larger Than 2KB',
    minFileSize: 2048,
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
