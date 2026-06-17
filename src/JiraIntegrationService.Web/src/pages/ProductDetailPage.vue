<template>
  <section class="page-stack">
    <div class="toolbar">
      <div>
        <h2>{{ productCode }}</h2>
        <p>Quan ly cau hinh tich hop theo tung product.</p>
      </div>
      <div class="button-row">
        <el-button @click="$router.push('/products')">Back to products</el-button>
        <el-button
          v-if="product"
          type="primary"
          :icon="Edit"
          @click="openEditProductDialog"
        >
          Edit product
        </el-button>
      </div>
    </div>

    <el-alert
      v-if="productError"
      :title="productError"
      type="error"
      show-icon
      :closable="false"
    />

    <div class="surface">
      <el-skeleton v-if="productQuery.isLoading.value" :rows="5" animated />

      <el-tabs v-else v-model="activeTab">
        <el-tab-pane label="Overview" name="overview">
          <el-descriptions v-if="product" :column="2" border>
            <el-descriptions-item label="Product code">
              {{ product.code }}
            </el-descriptions-item>
            <el-descriptions-item label="Status">
              <el-tag :type="product.isActive ? 'success' : 'info'" effect="plain">
                {{ product.isActive ? 'Active' : 'Inactive' }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="Name">
              {{ product.name }}
            </el-descriptions-item>
            <el-descriptions-item label="Jira project key">
              {{ product.jiraProjectKey }}
            </el-descriptions-item>
            <el-descriptions-item label="Jira base URL">
              {{ product.jiraBaseUrl }}
            </el-descriptions-item>
            <el-descriptions-item label="Jira API path">
              {{ product.jiraApiBasePath }}
            </el-descriptions-item>
            <el-descriptions-item label="Jira version">
              {{ product.jiraVersion }}
            </el-descriptions-item>
            <el-descriptions-item label="Updated">
              {{ formatDate(product.updatedAt) }}
            </el-descriptions-item>
          </el-descriptions>
        </el-tab-pane>

        <el-tab-pane label="Credential" name="credential">
          <div class="tab-stack">
            <el-alert
              v-if="credentialMissing"
              title="Product nay chua co active Jira credential. Hay nhap credential va save de bat dau goi Jira."
              type="warning"
              show-icon
              :closable="false"
            />
            <el-alert
              v-else-if="credentialError"
              :title="credentialError"
              type="error"
              show-icon
              :closable="false"
            />

            <div v-if="credential" class="credential-summary">
              <el-tag :type="credential.isActive ? 'success' : 'info'" effect="plain">
                {{ credential.isActive ? 'Active credential' : 'Inactive credential' }}
              </el-tag>
              <span>{{ credential.username }}</span>
              <span>{{ credential.authType }}</span>
              <span>{{ credential.hasPasswordOrToken ? 'Token configured' : 'Missing token' }}</span>
              <span>Updated {{ formatDate(credential.updatedAt) }}</span>
            </div>

            <CredentialForm
              ref="credentialFormRef"
              v-model="credentialForm"
              :loading="credentialQuery.isFetching.value || upsertCredentialMutation.isPending.value"
            />

            <div class="form-actions">
              <el-button
                type="primary"
                :loading="upsertCredentialMutation.isPending.value"
                @click="submitCredential"
              >
                Save credential
              </el-button>
            </div>
          </div>
        </el-tab-pane>

        <el-tab-pane label="Issue Types" name="issueTypes">
          <IssueTypeTable :product-code="productCode" />
        </el-tab-pane>
        <el-tab-pane label="Field Mappings" name="fieldMappings">
          <FieldMappingTable :product-code="productCode" />
        </el-tab-pane>
        <el-tab-pane label="Status Mappings" name="statusMappings">
          <StatusMappingTable :product-code="productCode" />
        </el-tab-pane>
        <el-tab-pane label="Validate" name="validate">
          <ValidateConfigPanel :product-code="productCode" />
        </el-tab-pane>
      </el-tabs>
    </div>

    <el-dialog
      v-model="productDialogVisible"
      :title="`Edit ${productCode}`"
      width="720px"
      destroy-on-close
      @closed="productFormRef?.clearValidate()"
    >
      <ProductForm
        ref="productFormRef"
        v-model="productForm"
        mode="edit"
        :loading="updateProductMutation.isPending.value"
      />
      <template #footer>
        <el-button :disabled="updateProductMutation.isPending.value" @click="productDialogVisible = false">
          Cancel
        </el-button>
        <el-button
          type="primary"
          :loading="updateProductMutation.isPending.value"
          @click="submitProduct"
        >
          Save changes
        </el-button>
      </template>
    </el-dialog>
  </section>
</template>

<script setup lang="ts">
import { Edit } from '@element-plus/icons-vue'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import dayjs from 'dayjs'
import { ElMessage } from 'element-plus'
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'

import CredentialForm from '../components/CredentialForm.vue'
import FieldMappingTable from '../components/FieldMappingTable.vue'
import IssueTypeTable from '../components/IssueTypeTable.vue'
import ProductForm from '../components/ProductForm.vue'
import StatusMappingTable from '../components/StatusMappingTable.vue'
import ValidateConfigPanel from '../components/ValidateConfigPanel.vue'
import { adminApi } from '../services/adminApi'
import { describeApiError, toApiClientError } from '../services/http'
import type {
  JiraCredentialAdminResponse,
  ProductAdminResponse,
  UpdateProductAdminRequest,
  UpsertJiraCredentialAdminRequest,
} from '../types/admin'
import {
  DEFAULT_CREDENTIAL_FORM,
  DEFAULT_PRODUCT_FORM,
  type CredentialFormModel,
  type ProductFormModel,
} from '../types/forms'

type FormExpose = {
  validate: () => Promise<void>
  clearValidate: () => void
}

const route = useRoute()
const queryClient = useQueryClient()
const activeTab = ref('overview')
const productDialogVisible = ref(false)
const productFormRef = ref<FormExpose>()
const credentialFormRef = ref<FormExpose>()
const productForm = ref<ProductFormModel>({ ...DEFAULT_PRODUCT_FORM })
const credentialForm = ref<CredentialFormModel>({ ...DEFAULT_CREDENTIAL_FORM })

const productCode = computed(() => String(route.params.code ?? '').trim())

const productQuery = useQuery({
  queryKey: ['admin', 'product', productCode],
  queryFn: () => adminApi.getProduct(productCode.value),
  enabled: computed(() => Boolean(productCode.value)),
})

const credentialQuery = useQuery({
  queryKey: ['admin', 'credential', productCode],
  queryFn: () => adminApi.getCredential(productCode.value),
  enabled: computed(() => Boolean(productCode.value) && activeTab.value === 'credential'),
  meta: {
    suppressGlobalError: true,
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

const upsertCredentialMutation = useMutation({
  mutationFn: (payload: UpsertJiraCredentialAdminRequest) =>
    adminApi.upsertCredential(productCode.value, payload),
  onSuccess: async (credential) => {
    ElMessage.success('Credential saved.')
    credentialForm.value = toCredentialForm(credential)
    await queryClient.invalidateQueries({ queryKey: ['admin', 'credential', productCode.value] })
  },
})

const product = computed(() => productQuery.data.value)
const credential = computed(() => credentialQuery.data.value)
const productError = computed(() =>
  productQuery.error.value
    ? describeApiError(productQuery.error.value, 'Khong tai duoc product.')
    : '',
)
const credentialMissing = computed(() => {
  const error = credentialQuery.error.value
  return Boolean(error && toApiClientError(error).errorCode === 'CONFIG_NOT_FOUND')
})
const credentialError = computed(() => {
  const error = credentialQuery.error.value
  if (!error || credentialMissing.value) {
    return ''
  }

  return describeApiError(error, 'Khong tai duoc credential.')
})

watch(
  () => productQuery.data.value,
  (nextProduct) => {
    if (nextProduct && !productDialogVisible.value) {
      productForm.value = toProductForm(nextProduct)
    }
  },
  { immediate: true },
)

watch(
  () => credentialQuery.data.value,
  (nextCredential) => {
    if (nextCredential) {
      credentialForm.value = toCredentialForm(nextCredential)
    }
  },
  { immediate: true },
)

function openEditProductDialog() {
  if (!product.value) {
    return
  }

  productForm.value = toProductForm(product.value)
  productDialogVisible.value = true
}

async function submitProduct() {
  await productFormRef.value?.validate()

  await updateProductMutation.mutateAsync({
    code: productCode.value,
    payload: toUpdateProductPayload(productForm.value),
  })
}

async function submitCredential() {
  await credentialFormRef.value?.validate()

  await upsertCredentialMutation.mutateAsync(toCredentialPayload(credentialForm.value))
}

function toProductForm(nextProduct: ProductAdminResponse): ProductFormModel {
  return {
    code: nextProduct.code,
    name: nextProduct.name,
    jiraProjectKey: nextProduct.jiraProjectKey,
    jiraBaseUrl: nextProduct.jiraBaseUrl,
    jiraApiBasePath: nextProduct.jiraApiBasePath,
    jiraVersion: nextProduct.jiraVersion,
    isActive: nextProduct.isActive,
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

function toCredentialForm(nextCredential: JiraCredentialAdminResponse): CredentialFormModel {
  return {
    authType: nextCredential.authType,
    username: nextCredential.username,
    passwordOrToken: '',
    isActive: nextCredential.isActive,
  }
}

function toCredentialPayload(form: CredentialFormModel): UpsertJiraCredentialAdminRequest {
  return {
    authType: form.authType.trim(),
    username: form.username.trim(),
    passwordOrToken: form.passwordOrToken.trim(),
    isActive: form.isActive,
  }
}

function formatDate(value: string) {
  return dayjs(value).format('YYYY-MM-DD HH:mm')
}
</script>
