<!-- This file is part of Metaplay SDK which is released under the Metaplay SDK License. -->

<template lang="pug">
div.mb-1
  div.mb-1
    h6 {{displayName}}
      MTooltip.ml-2(v-if="displayHint" :content="displayHint" noUnderline): MBadge(shape="pill") ?
    b-row.mb-1(v-for="(val, idx) in value", :key="idx")
      b-col.pl-3.pr-3.pt-3.pb-2.bg-light.rounded.border
        generated-ui-form-dynamic-component(
          v-if="arrayType"
          v-bind="props"
          :fieldInfo="arrayFieldInfo"
          :value="val"
          @input="(newVal: any) => update(idx, newVal)"
          :fieldPath="fieldPath + '/' + idx"
          )
        div.mt-2.mb-2
          MButton(
            @click="remove(idx)"
            variant="danger"
            ) Remove
  MButton(
    @click="add"
    ) Add
</template>

<script lang="ts" setup>
import { EGeneratedUiTypeKind, type IGeneratedUiFieldInfo } from '../../generatedUiTypes'
import { generatedUiFieldFormEmits, generatedUiFieldFormProps, useGeneratedUiFieldForm } from '../../generatedFieldBase'
import { ref, onMounted, type PropType, computed } from 'vue'
import { MBadge, MButton, MTooltip } from '@metaplay/meta-ui-next'
import GeneratedUiFormDynamicComponent from './GeneratedUiFormDynamicComponent.vue'

// Use form props but override default value
const props = defineProps({
  ...generatedUiFieldFormProps,
  value: {
    type: Array as PropType<any[]>,
    default: () => ([])
  },
})

const emit = defineEmits(generatedUiFieldFormEmits)

const { displayName, displayHint, update: emitUpdate } = useGeneratedUiFieldForm(props, emit)

const arrayType = ref<string>()

const arrayFieldInfo = computed<IGeneratedUiFieldInfo>(() => {
  const arrayFieldInfo = { ...props.fieldInfo }
  arrayFieldInfo.fieldName = ''
  arrayFieldInfo.fieldType = arrayType.value ?? ''
  arrayFieldInfo.typeKind = EGeneratedUiTypeKind.ValueCollection
  return arrayFieldInfo
})

onMounted(() => {
  if (!props.fieldInfo.typeParams) throw new Error('Array field must have type params')
  arrayType.value = props.fieldInfo.typeParams[0]
})

function update (idx: any, newValue: any) {
  emitUpdate(props.value.map((val: any, i: any) => {
    if (i === idx) {
      return newValue
    }
    return val
  }))
}

function add () {
  emitUpdate(props.value.concat(undefined))
}

function remove (idx: any) {
  emitUpdate(props.value.filter((_val, i) => idx !== i))
}

</script>
