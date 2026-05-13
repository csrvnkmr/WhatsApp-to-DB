<template>

<div class="p-6">

    <DynamicTable
        :entity="props.entity"
        :database="props.database" />

</div>

</template>

<script setup lang="ts">
const props = defineProps<{
    entity:string
    database:string
}>()

import DynamicTable from '../components/DynamicTable.vue'
</script>
