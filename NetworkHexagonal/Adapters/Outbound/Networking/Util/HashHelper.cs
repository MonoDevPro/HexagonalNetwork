using System;

namespace NetworkHexagonal.Adapters.Outbound.Networking.Util;

/// <summary>
        /// Classe estática para caching de hash por tipo.
        /// Usa o algoritmo FNV-1 de 64 bits para calcular o hash.
        /// </summary>
        /// <typeparam name="T">Tipo para o qual calcular o hash</typeparam>
        internal static class HashHelper<T>
        {
            public static readonly ulong Id;

            //FNV-1 64 bit hash
            static HashHelper()
            {
                ulong hash = 14695981039346656037UL; //offset
                string typeName = typeof(T).ToString();
                for (var i = 0; i < typeName.Length; i++)
                {
                    hash ^= typeName[i];
                    hash *= 1099511628211UL; //prime
                }
                Id = hash;
            }
        }