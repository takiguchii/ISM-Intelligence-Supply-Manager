<template>
  <div class="min-h-screen bg-amarelo-ISM flex items-center justify-center p-4">
    
    <div class="bg-white rounded-3xl shadow-2xl flex flex-col md:flex-row w-full max-w-5xl overflow-hidden min-h-[600px]">
      
      <div class="md:w-2/5 bg-vermelho-ISM text-white flex flex-col items-center justify-center p-10 text-center transition-all duration-500" :class="{'order-2': !isLogin}">
        <h2 class="text-4xl font-bold mb-4">{{ isLogin ? 'Pronto para transformar o seu negócio?' : 'Bem-vindo de volta!' }}</h2>
        <p class="mb-8">
          {{ isLogin 
            ? 'Insira seus dados pessoais e comece sua jornada!' 
            : 'Para se manter conectado conosco, faça login com suas informações pessoais.' 
          }}
        </p>
      </div>
      <div class="md:w-3/5 p-10 flex flex-col justify-center transition-all duration-500" :class="{'order-1': !isLogin}">
        
        <div v-if="isLogin" class="flex flex-col items-center w-full max-w-md mx-auto">
          <h2 class="text-4xl font-bold text-gray-800 mb-6">Entrar</h2>

          <a href="#" class="text-sm text-gray-500 hover:underline mb-6">Entre com suas credenciais ISM</a>
          
          <input 
            type="email" 
            placeholder="Email" 
            class="w-full bg-gray-100 text-gray-900 border-none rounded-md px-4 py-3 mb-4 focus:ring-2 focus:ring-vermelho-ISM outline-none" />
          <input 
            type="password" 
            placeholder="Senha" 
            class="w-full bg-gray-100 text-gray-900 border-none rounded-md px-4 py-3 mb-4 focus:ring-2 focus:ring-vermelho-ISM outline-none" />
            <input 
            type="number" 
            placeholder="Código da Loja" 
            class="w-full bg-gray-100 text-gray-900 border-none rounded-md px-4 py-3 mb-4 focus:ring-2 focus:ring-vermelho-ISM outline-none" />
          
          <a href="#" class="text-sm text-gray-500 hover:underline mb-6">Esqueceu sua senha?</a>
          <button class="bg-vermelho-ISM text-white rounded-full px-12 py-3 font-semibold hover:bg-red-600 transition-colors">ENTRAR</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const isLogin = ref(true)

const toggleForm = () => {
  isLogin.value = !isLogin.value
}
</script>