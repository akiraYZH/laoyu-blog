import { ref } from 'vue'
import { message } from 'ant-design-vue'
import type { UploadImgEvent } from 'md-editor-v3'
import { apiFetch } from '@/api/apiFetch'
import { createApiRequestError } from '@/stores/functions/readError'

interface UploadImageResponse {
  url: string
}

export function useUploadImage() {
  const uploading = ref(false)

  const uploadImage: UploadImgEvent = async (files, callback) => {
    if (files.length !== 1) {
      message.warning('Only one image can be uploaded at a time')
      return
    }

    const file = files[0]

    if (!file) {
      message.warning('Please select an image')
      return
    }

    const formData = new FormData()

    formData.append('file', file)

    uploading.value = true

    try {
      const response = await apiFetch('/api/images', {
        method: 'POST',
        body: formData,
      })

      if (!response.ok) {
        throw await createApiRequestError(response)
      }

      const result: UploadImageResponse = await response.json()

      callback([
        {
          url: result.url,
          alt: file.name,
          title: file.name,
        },
      ])

      message.success('Successfully uploaded image')
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to upload image'

      message.error(errorMessage)
    } finally {
      uploading.value = false
    }
  }

  return {
    uploading,
    uploadImage,
  }
}
