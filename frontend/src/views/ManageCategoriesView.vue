<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { message } from 'ant-design-vue'
import { useBlogPosts } from '@/composables/useBlogPosts'
import { EditOutlined, DeleteOutlined, PlusOutlined } from '@ant-design/icons-vue'
import type { Category } from '@/types'

const { categories, getCategories, createCategory, updateCategory, deleteCategory } = useBlogPosts()

const loading = ref(false)
const modalVisible = ref(false)
const isEditing = ref(false)
const currentCategoryId = ref<number | null>(null)
const categoryNameInput = ref('')

const columns = [
  {
    title: 'ID',
    dataIndex: 'id',
    key: 'id',
    width: 80,
  },
  {
    title: 'Name',
    dataIndex: 'name',
    key: 'name',
  },
  {
    title: 'Slug',
    dataIndex: 'slug',
    key: 'slug',
  },
  {
    title: 'Action',
    key: 'action',
    width: 150,
  },
]

onMounted(async () => {
  loading.value = true
  await getCategories()
  loading.value = false
})

const openCreateModal = () => {
  isEditing.value = false
  currentCategoryId.value = null
  categoryNameInput.value = ''
  modalVisible.value = true
}

const openEditModal = (record: Category) => {
  isEditing.value = true
  currentCategoryId.value = record.id
  categoryNameInput.value = record.name
  modalVisible.value = true
}

const handleOk = async () => {
  if (!categoryNameInput.value.trim()) {
    message.warning('Category name cannot be empty')
    return
  }

  loading.value = true
  try {
    if (isEditing.value && currentCategoryId.value) {
      const result = await updateCategory(currentCategoryId.value, categoryNameInput.value)
      if (result) {
        message.success('Category updated successfully')
        modalVisible.value = false
      }
    } else {
      const result = await createCategory(categoryNameInput.value)
      if (result) {
        message.success('Category created successfully')
        modalVisible.value = false
      }
    }
  } finally {
    loading.value = false
  }
}

const handleDelete = async (id: number) => {
  const result = await deleteCategory(id)
  if (result) {
    message.success('Category deleted successfully')
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto py-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-3xl font-bold">Manage Categories</h1>
      <a-button type="primary" @click="openCreateModal">
        <template #icon><PlusOutlined /></template>
        New Category
      </a-button>
    </div>

    <a-table
      :dataSource="categories"
      :columns="columns"
      rowKey="id"
      :loading="loading"
      :pagination="false"
      bordered
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'action'">
          <a-space>
            <a-button type="text" @click="openEditModal(record)">
              <template #icon><EditOutlined /></template>
            </a-button>
            <a-popconfirm
              title="Are you sure you want to delete this category?"
              ok-text="Yes"
              cancel-text="No"
              @confirm="handleDelete(record.id)"
            >
              <a-button type="text" danger>
                <template #icon><DeleteOutlined /></template>
              </a-button>
            </a-popconfirm>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-modal
      v-model:open="modalVisible"
      :title="isEditing ? 'Edit Category' : 'New Category'"
      @ok="handleOk"
      :confirmLoading="loading"
    >
      <div class="my-4">
        <a-typography-text class="block mb-2">Category Name:</a-typography-text>
        <a-input
          v-model:value="categoryNameInput"
          placeholder="Enter category name"
          @pressEnter="handleOk"
        />
      </div>
    </a-modal>
  </div>
</template>
