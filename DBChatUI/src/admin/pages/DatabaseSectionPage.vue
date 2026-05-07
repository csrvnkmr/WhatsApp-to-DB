<template>

<div class="p-6">

    <DynamicTable
        :entity="entity"
        :database="database" />

</div>

</template>

<script setup lang="ts">
defineProps<{
    entity:string
    database:string
}>()

import DynamicTable from '../components/DynamicTable.vue'
</script>
