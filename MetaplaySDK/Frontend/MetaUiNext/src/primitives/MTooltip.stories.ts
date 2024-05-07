import MTooltip from './MTooltip.vue'
import MActionModalButton from '../unstable/MActionModalButton.vue'
import type { Meta, StoryObj } from '@storybook/vue3'

const meta: Meta<typeof MTooltip> = {
  component: MTooltip,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof MTooltip>

export const Default: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      Text with a <MTooltip v-bind="args">tooltip</MTooltip> in it.
    </span>
    `,
  }),
  args: {
    content: 'This is a tooltip',
  },
}

export const TriggerWhitespace: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      Text with <MTooltip v-bind="args">two words</MTooltip> in it to trigger whitespace in between.
    </span>
    `,
  }),
  args: {
    content: 'This is a tooltip',
  },
}

export const ContentLineBreak: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      Tooltip <MTooltip v-bind="args">content</MTooltip> can have pre-formatted line breaks.
    </span>
    `,
  }),
  args: {
    content: 'This is a tooltip with\na line break.',
  },
}

export const Slot: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      You can also use
      <MTooltip v-bind="args">
        slots<template #content>
          <span>Content <span class="tw-text-red-300">slot</span> overrides the content prop.</span>
        </template>
      </MTooltip> for complex tooltips.
    </span>
    `,
  }),
  args: {
    content: 'This is not show because of the slot.',
  },
}

export const Hidden: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      Text with a disabled <MTooltip v-bind="args">tooltip</MTooltip> in it.
    </span>
    `,
  }),
  args: {
    content: undefined,
  },
}

export const MediumDuration: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      This <MTooltip v-bind="args">duration</MTooltip> needs max-width of 13rem with xs-font and 14.5rem with custom tooltip font to be on 1 line and to look the best.
    </span>
    `,
  }),
  args: {
    content: '42 minutes and 20 seconds exactly',
  },
}

export const LongDuration: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      For this <MTooltip v-bind="args">duration</MTooltip> max-width of 10rem looks the best, but does defaulting make it more difficult to read?
    </span>
    `,
  }),
  args: {
    content: '1 day 9 hours 17 minutes and 39 seconds exactly',
  },
}

export const MediumContent: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      <MTooltip v-bind="args">Logout</MTooltip>
    </span>
    `,
  }),
  args: {
    // content: 'Cannot log out when authentication is disabled.',
    content: 'Adds advanced technical information onto most pages.'
  },
}

export const MediumPermission: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      <MTooltip v-bind="args">Environment</MTooltip>
    </span>
    `,
  }),
  args: {
    content: "You need the 'dashboard.environment.view' permission to view this page.",
  },
}

export const LongPermissions: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      <MTooltip v-bind="args">Broadcasts</MTooltip>
    </span>
    `,
  }),
  args: {
    content: "You need the 'api.broadcasts.view' permission to view this page.",
  },
}

export const LongContent: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      <MTooltip v-bind="args">Uncomfortably long tooltip</MTooltip> that should probably not be used at all.
    </span>
    `,
  }),
  args: {
    content: 'The phases on the page are evaluated according to the local time of your browser. Enabling custom evaluation allows you to set an exact time to evaluate against.',
  },
}

export const RidiculouslyLongContent: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      This use-case does not make sense but also should not crash the browser or anything: <MTooltip v-bind="args">Lorem ipsum</MTooltip>
    </span>
    `,
  }),
  args: {
    content: 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. In non purus eget tortor dapibus tristique et pellentesque nisi. In dictum a eros non posuere. Vivamus viverra libero sit amet elementum facilisis. Aenean non euismod velit. Ut ante augue, molestie non suscipit a, cursus in ante. Duis egestas libero vel congue varius. Duis vel metus non tellus auctor consectetur nec at justo. Nam semper lorem purus. Maecenas elementum tristique tempor. Ut condimentum posuere rutrum. In hac habitasse platea dictumst. Sed aliquet urna nisl, nec porta purus ullamcorper eget. Phasellus rutrum enim ac massa porttitor, ut egestas lectus consectetur. Lorem ipsum dolor sit amet, consectetur adipiscing elit. In non purus eget tortor dapibus tristique et pellentesque nisi. In dictum a eros non posuere. Vivamus viverra libero sit amet elementum facilisis. Aenean non euismod velit. Ut ante augue, molestie non suscipit a, cursus in ante. Duis egestas libero vel congue varius. Duis vel metus non tellus auctor consectetur nec at justo. Nam semper lorem purus. Maecenas elementum tristique tempor. Ut condimentum posuere rutrum. In hac habitasse platea dictumst. Sed aliquet urna nisl, nec porta purus ullamcorper eget. Phasellus rutrum enim ac massa porttitor, ut egestas lectus consectetur.',
  },
}

export const NoWhitespace: Story = {
  render: (args) => ({
    components: { MTooltip },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-tooltips"></div>
    </Teleport>
    <span>
      This use-case does not make sense but also should not crash the browser or anything: <MTooltip v-bind="args">NoWhitespaces</MTooltip>
    </span>
    `,
  }),
  args: {
    content: 'Loremipsumdolorsitamet,consecteturadipiscingelit.Innonpurusegettortordapibustristiqueetpellentesquenisi.Indictumaerosnonposuere.Vivamusviverraliberositametelementumfacilisis.Aeneannoneuismodvelit.Utanteaugue,molestienonsuscipita,cursusinante.Duisegestasliberovelconguevarius.Duisvelmetusnontellusauctorconsecteturnecatjusto.Namsemperlorempurus.Maecenaselementumtristiquetempor.Utcondimentumposuererutrum.Inhachabitasseplateadictumst.Sedaliqueturnanisl,necportapurusullamcorpereget.Phasellusrutrumenimacmassaporttitor,utegestaslectusconsectetur.',
  },
}

export const InsideModal: Story = {
  render: (args) => ({
    components: { MTooltip, MActionModalButton },
    setup: () => ({ args }),
    template: `
    <Teleport to="body">
      <div id="root-modals"></div>
      <div id="root-tooltips"></div>
    </Teleport>
    <MActionModalButton modalTitle="Example Modal" triggerButtonLabel="Open">
      <MTooltip v-bind="args">Hover me</MTooltip>
    </MActionModalButton>
    `,
  }),
  args: {
    content: 'This is a tooltip',
  },
}
