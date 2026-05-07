<template>

<div class="p-6">

    <DynamicTable
        :entity="entity" />

</div>

</template>

<script setup lang="ts">
defineProps<{
    entity:string
}>()

import DynamicTable from '../components/DynamicTable.vue'
</script>
