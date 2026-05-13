<template>

<div class="p-6">

    <DynamicTable
        :entity="entity"
        @data-changed="$emit('data-changed', entity)" />

</div>

</template>

<script setup lang="ts">
defineProps<{
    entity:string
}>()

defineEmits(['data-changed'])

import DynamicTable from '../components/DynamicTable.vue'
</script>
