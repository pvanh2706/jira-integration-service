<template>
  <section class="page-stack">
    <div class="toolbar">
      <div>
        <h2>Products</h2>
        <p>Quan ly product, Jira base URL va project key.</p>
      </div>
      <el-button type="primary" :icon="Plus" @click="openCreateDialog">New product</el-button>
    </div>

    <div class="surface">
      <div class="table-toolbar">
        <el-input
          v-model="searchTerm"
          clearable
          placeholder="Search code, name, project key..."
          class="table-toolbar__search"
        />
        <el-select v-model="activeFilter" class="table-toolbar__filter">
          <el-option label="All statuses" value="all" />
          <el-option label="Active only" value="active" />
          <el-option label="Inactive only" value="inactive" />
        </el-select>
      </div>

      <el-alert
        v-if="productsError"
        :title="productsError"
        type="error"
        show-icon
        :closable="false"
        class="mb-16"
      />

      <el-table
        v-loading="productsQuery.isLoading.value"
        :data="filteredProducts"
        empty-text="No products found"
      >
        <el-table-column prop="code" label="Code" width="120" />
        <el-table-column prop="name" label="Name" min-width="180" />
        <el-table-column prop="jiraProjectKey" label="Jira project" width="130" />
        <el-table-column label="Jira endpoint" min-width="260">
          <template #default="{ row }">
            <div class="stack-compact">
              <span>{{ row.jiraBaseUrl }}</span>
              <small>{{ row.jiraApiBasePath }} · {{ row.jiraVersion }}</small>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="Status" width="120">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'" effect="plain">
              {{ row.isActive ? 'Active' : 'Inactive' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Updated" width="160">
          <template #default="{ row }">
            {{ formatDate(row.updatedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="Action" width="220" align="right" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="$router.push(`/products/${row.code}`)">
              Open
            </el-button>
            <el-button link @click="openEditDialog(row)">Edit</el-button>
            <el-button link type="danger" @click="confirmDelete(row)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog
      v-model="productDialogVisible"
      :title="dialogMode === 'create' ? 'Create product' : `Edit ${editingProductCode}`"
      width="720px"
      destroy-on-close
      @closed="productFormRef?.clearValidate()"
    >
      <ProductForm
        ref="productFormRef"
        v-model="productForm"
        :mode="dialogMode"
        :loading="isSavingProduct"
      />
      <template #footer>
        <el-button :disabled="isSavingProduct" @click="productDialogVisible = false">
          Cancel
        </el-button>
        <el-button type="primary" :loading="isSavingProduct" @click="submitProduct">
          {{ dialogMode === 'create' ? 'Create' : 'Save changes' }}
        </el-button>
      </template>
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { Plus } from '@element-plus/icons-vue'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import dayjs from 'dayjs'
import { ElMessage, ElMessageBox } from 'element-plus'
import { computed, ref } from 'vue'

import ProductForm from '../components/ProductForm.vue'
import { adminApi } from '../services/adminApi'
import { describeApiError } from '../services/http'
import type {
  CreateProductAdminRequest,
  ProductAdminResponse,
  UpdateProductAdminRequest,
} from '../types/admin'
import { DEFAULT_PRODUCT_FORM, type ProductFormModel } from '../types/forms'

type ProductFormExpose = {
  validate: () => Promise<void>
  clearValidate: () => void
}

const queryClient = useQueryClient()
const searchTerm = ref('')
const activeFilter = ref<'all' | 'active' | 'inactive'>('all')
const productDialogVisible = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const editingProductCode = ref('')
const productFormRef = ref<ProductFormExpose>()
const productForm = ref<ProductFormModel>(createDefaultProductForm())

const productsQuery = useQuery({
  queryKey: ['admin', 'products'],
  queryFn: adminApi.getProducts,
})

const createProductMutation = useMutation({
  mutationFn: (payload: CreateProductAdminRequest) => adminApi.createProduct(payload),
  onSuccess: async (product) => {
    ElMessage.success(`Product ${product.code} created.`)
    productDialogVisible.value = false
    await queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
  },
})

const updateProductMutation = useMutation({
  mutationFn: ({ code, payload }: { code: string; payload: UpdateProductAdminRequest }) =>
    adminApi.updateProduct(code, payload),
  onSuccess: async (product) => {
    ElMessage.success(`Product ${product.code} updated.`)
    productDialogVisible.value = false
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] }),
      queryClient.invalidateQueries({ queryKey: ['admin', 'product', product.code] }),
    ])
  },
})

const deleteProductMutation = useMutation({
  mutationFn: (code: string) => adminApi.deleteProduct(code),
  onSuccess: async (_result, code) => {
    ElMessage.success(`Product ${code} deleted.`)
    await queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
  },
})

const products = computed(() => productsQuery.data.value ?? [])
const productsError = computed(() =>
  productsQuery.error.value
    ? describeApiError(productsQuery.error.value, 'Khong tai duoc danh sach product.')
    : '',
)
const isSavingProduct = computed(
  () => createProductMutation.isPending.value || updateProductMutation.isPending.value,
)

const filteredProducts = computed(() => {
  const keyword = searchTerm.value.trim().toLowerCase()

  return products.value.filter((product) => {
    const matchesKeyword =
      !keyword ||
      [product.code, product.name, product.jiraProjectKey, product.jiraBaseUrl]
        .join(' ')
        .toLowerCase()
        .includes(keyword)
    const matchesStatus =
      activeFilter.value === 'all' ||
      (activeFilter.value === 'active' && product.isActive) ||
      (activeFilter.value === 'inactive' && !product.isActive)

    return matchesKeyword && matchesStatus
  })
})

function openCreateDialog() {
  dialogMode.value = 'create'
  editingProductCode.value = ''
  productForm.value = createDefaultProductForm()
  productDialogVisible.value = true
}

function openEditDialog(product: ProductAdminResponse) {
  dialogMode.value = 'edit'
  editingProductCode.value = product.code
  productForm.value = toProductForm(product)
  productDialogVisible.value = true
}

async function submitProduct() {
  await productFormRef.value?.validate()

  if (dialogMode.value === 'create') {
    await createProductMutation.mutateAsync(toCreateProductPayload(productForm.value))
    return
  }

  await updateProductMutation.mutateAsync({
    code: editingProductCode.value,
    payload: toUpdateProductPayload(productForm.value),
  })
}

async function confirmDelete(product: ProductAdminResponse) {
  try {
    await ElMessageBox.confirm(
      `Delete product ${product.code}? Cau hinh issue type, field mapping va credential lien quan cung se bi xoa.`,
      'Delete product',
      {
        type: 'warning',
        confirmButtonText: 'Delete',
        cancelButtonText: 'Cancel',
        confirmButtonClass: 'el-button--danger',
      },
    )
  } catch {
    return
  }

  await deleteProductMutation.mutateAsync(product.code)
}

function createDefaultProductForm(): ProductFormModel {
  return { ...DEFAULT_PRODUCT_FORM }
}

function toProductForm(product: ProductAdminResponse): ProductFormModel {
  return {
    code: product.code,
    name: product.name,
    jiraProjectKey: product.jiraProjectKey,
    jiraBaseUrl: product.jiraBaseUrl,
    jiraApiBasePath: product.jiraApiBasePath,
    jiraVersion: product.jiraVersion,
    isActive: product.isActive,
  }
}

function toCreateProductPayload(form: ProductFormModel): CreateProductAdminRequest {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    jiraProjectKey: form.jiraProjectKey.trim(),
    jiraBaseUrl: form.jiraBaseUrl.trim(),
    jiraApiBasePath: form.jiraApiBasePath.trim(),
    jiraVersion: form.jiraVersion.trim(),
    isActive: form.isActive,
  }
}

function toUpdateProductPayload(form: ProductFormModel): UpdateProductAdminRequest {
  return {
    name: form.name.trim(),
    jiraProjectKey: form.jiraProjectKey.trim(),
    jiraBaseUrl: form.jiraBaseUrl.trim(),
    jiraApiBasePath: form.jiraApiBasePath.trim(),
    jiraVersion: form.jiraVersion.trim(),
    isActive: form.isActive,
  }
}

function formatDate(value: string) {
  return dayjs(value).format('YYYY-MM-DD HH:mm')
}
</script>
