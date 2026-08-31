import { RouterProvider } from 'react-router-dom'
import { QueryProvider } from '@/app/providers/QueryProvider'
import { ThemeProvider } from '@/app/providers/ThemeProvider'
import { router } from '@/app/router/routes'
import { AuthProvider } from '@/shared/auth/AuthContext'

function App() {
  return (
    <ThemeProvider>
      <QueryProvider>
        <AuthProvider>
          <RouterProvider router={router} />
        </AuthProvider>
      </QueryProvider>
    </ThemeProvider>
  )
}

export default App
