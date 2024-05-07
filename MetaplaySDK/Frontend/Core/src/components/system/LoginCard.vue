<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div#system-login-card
  b-card.text-center
    b-card-title
      span 🔑 Authentication Needed
    p You have accessed the LiveOps Dashboard without logging in first. In cloud deployments we recommend forcing authentication during cluster ingress.
    MButton(@click="login") Login
</template>

<script lang="ts" setup>
import { MButton } from '@metaplay/meta-ui-next'
import { login } from '@metaplay/game-server-api'
</script>
